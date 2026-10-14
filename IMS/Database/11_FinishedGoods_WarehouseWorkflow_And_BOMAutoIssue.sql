-- ================================================================================
-- 11_FinishedGoods_WarehouseWorkflow_And_BOMAutoIssue.sql
-- Production Finished Goods Warehouse Workflow & BOM Auto-Issue
-- ================================================================================

USE FactoryIMS;
GO

-- 1. Ensure QCChecks supports both PO ReceivingItems and Production FinishedGoodsReceipts
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('QCChecks') AND name = 'FGReceiptID')
BEGIN
    ALTER TABLE QCChecks ADD FGReceiptID INT NULL FOREIGN KEY REFERENCES FinishedGoodsReceipts(FGReceiptID);
    PRINT 'Added FGReceiptID column to QCChecks.';
END
GO

-- Allow ReceiptID to be NULL in QCChecks when QC is performed on Finished Goods
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('QCChecks') AND name = 'ReceiptID' AND is_nullable = 0)
BEGIN
    ALTER TABLE QCChecks ALTER COLUMN ReceiptID INT NULL;
    PRINT 'Updated QCChecks.ReceiptID to allow NULL.';
END
GO

-- 2. Stored Procedure: Auto-Generate Material Issue Requests from BOM
CREATE OR ALTER PROCEDURE dbo.sp_AutoGenerateMaterialIssues (
    @PlanID INT,
    @PerformedBy INT
)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;

    BEGIN TRY
        DECLARE @BOMID INT, @PlannedQty DECIMAL(18,2);

        SELECT @BOMID = BOMID, @PlannedQty = PlannedQty
        FROM ProductionPlans
        WHERE PlanID = @PlanID;

        IF @BOMID IS NULL
            THROW 50020, 'Production Plan or linked BOM not found.', 1;

        -- Only generate if no material issues currently exist for this plan
        IF NOT EXISTS (SELECT 1 FROM ProductionMaterialIssues WHERE PlanID = @PlanID)
        BEGIN
            INSERT INTO ProductionMaterialIssues (PlanID, ProductID, ShelfID, RequestedQty, IssuedQty, ReturnedQty, Status, IssuedBy, IssueDate)
            SELECT 
                @PlanID,
                bi.RawMaterialID,
                COALESCE(
                    -- Prefer shelf that has current available stock for this raw material
                    (SELECT TOP 1 s.ShelfID 
                     FROM InventoryStocks inv
                     INNER JOIN Shelves s ON inv.ShelfID = s.ShelfID
                     WHERE inv.ProductID = bi.RawMaterialID AND inv.CurrentQty > 0 AND s.IsActive = 1
                     ORDER BY inv.CurrentQty DESC),
                    -- Or fallback to any active raw material / general shelf
                    (SELECT TOP 1 ShelfID FROM Shelves WHERE IsActive = 1 AND ShelfType IN ('Raw Material', 'Standard', 'General') ORDER BY ShelfID),
                    (SELECT TOP 1 ShelfID FROM Shelves WHERE IsActive = 1 ORDER BY ShelfID)
                ) AS ShelfID,
                ROUND(bi.RequiredQty * @PlannedQty * (1.0 + (ISNULL(bi.WastagePercent, 0) / 100.0)), 2) AS RequestedQty,
                0 AS IssuedQty,
                0 AS ReturnedQty,
                'Pending' AS Status,
                @PerformedBy AS IssuedBy,
                GETDATE() AS IssueDate
            FROM BOMItems bi
            WHERE bi.BOMID = @BOMID;
        END

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

PRINT '11_FinishedGoods_WarehouseWorkflow_And_BOMAutoIssue.sql applied successfully.';
GO
