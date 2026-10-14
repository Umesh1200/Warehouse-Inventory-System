-- ==========================================
-- 08_SupplierReturns.sql
-- Return to Vendor (RTV) and Supplier Returns
-- ==========================================

-- 1. Alter Suppliers table if columns do not exist
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Suppliers' AND COLUMN_NAME = 'ReturnPolicy')
BEGIN
    ALTER TABLE Suppliers ADD ReturnPolicy NVARCHAR(MAX) NULL;
END

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Suppliers' AND COLUMN_NAME = 'ReturnEmail')
BEGIN
    ALTER TABLE Suppliers ADD ReturnEmail NVARCHAR(100) NULL;
END

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Suppliers' AND COLUMN_NAME = 'PreferredCarrier')
BEGIN
    ALTER TABLE Suppliers ADD PreferredCarrier NVARCHAR(100) NULL;
END
GO

-- 2. Create SupplierReturns Table
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SupplierReturns')
BEGIN
    CREATE TABLE SupplierReturns (
        ReturnID INT IDENTITY(1,1) PRIMARY KEY,
        ReturnNo NVARCHAR(50) NOT NULL UNIQUE,
        SupplierID INT NOT NULL FOREIGN KEY REFERENCES Suppliers(SupplierID),
        RMANumber NVARCHAR(100) NULL,
        ReturnDate DATETIME NOT NULL DEFAULT GETDATE(),
        Carrier NVARCHAR(100) NULL,
        TrackingNumber NVARCHAR(100) NULL,
        CreditStatus NVARCHAR(50) NOT NULL DEFAULT 'Pending', -- Pending, Issued, Denied
        Status NVARCHAR(50) NOT NULL DEFAULT 'Draft', -- Draft, Shipped, Completed, Cancelled
        Remarks NVARCHAR(500) NULL,
        CreatedBy INT NULL FOREIGN KEY REFERENCES Users(UserID),
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
        UpdatedAt DATETIME NULL
    );
END
GO

-- 3. Create SupplierReturnItems Table
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SupplierReturnItems')
BEGIN
    CREATE TABLE SupplierReturnItems (
        ReturnItemID INT IDENTITY(1,1) PRIMARY KEY,
        ReturnID INT NOT NULL FOREIGN KEY REFERENCES SupplierReturns(ReturnID) ON DELETE CASCADE,
        ProductID INT NOT NULL FOREIGN KEY REFERENCES Products(ProductID),
        Quantity DECIMAL(18,4) NOT NULL,
        UnitPrice DECIMAL(18,4) NOT NULL,
        Reason NVARCHAR(250) NULL,
        SourceShelfID INT NULL FOREIGN KEY REFERENCES Shelves(ShelfID)
    );
END
GO

-- 4. Seed a Quarantine shelf if it doesn't exist
IF NOT EXISTS (SELECT 1 FROM Shelves WHERE ShelfCode = 'KTM-F1-R01-A01-QR1')
BEGIN
    INSERT INTO Shelves (RackID, ShelfCode, ShelfName, ShelfType, MaxQty, MaxWeightKg, IsActive)
    VALUES (1, 'KTM-F1-R01-A01-QR1', 'Quarantine Shelf 1', 'Quarantine', 500, 1000, 1);
END
GO

-- 5. Stored Procedure for Confirming Supplier Return
CREATE OR ALTER PROCEDURE dbo.sp_ConfirmSupplierReturn (
    @ReturnID INT,
    @PerformedBy INT
)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;

    BEGIN TRY
        DECLARE @ReturnNo NVARCHAR(50), @Status NVARCHAR(50);
        SELECT @ReturnNo = ReturnNo, @Status = Status FROM SupplierReturns WHERE ReturnID = @ReturnID;

        IF @ReturnNo IS NULL OR @Status <> 'Draft'
            THROW 50020, 'Invalid Return or already processed/shipped.', 1;

        -- Cursor to process items
        DECLARE @ProductID INT, @SourceShelfID INT, @Quantity DECIMAL(18,2);
        
        DECLARE curItems CURSOR FOR 
        SELECT ProductID, SourceShelfID, Quantity FROM SupplierReturnItems WHERE ReturnID = @ReturnID;

        OPEN curItems;
        FETCH NEXT FROM curItems INTO @ProductID, @SourceShelfID, @Quantity;

        WHILE @@FETCH_STATUS = 0
        BEGIN
            -- Check Source Stock
            DECLARE @CurrentStock DECIMAL(18,2) = 0;
            SELECT @CurrentStock = CurrentQty FROM InventoryStocks WHERE ProductID = @ProductID AND ShelfID = @SourceShelfID;
            
            IF @CurrentStock < @Quantity
                THROW 50021, 'Insufficient stock at source shelf for return.', 1;

            -- Log Movement as StockOut
            EXEC dbo.sp_RecordStockMovement 
                @ProductID = @ProductID, 
                @ShelfID = @SourceShelfID, 
                @MovementType = 'StockOut', 
                @Quantity = @Quantity, 
                @ReferenceType = 'SupplierReturn', 
                @ReferenceNo = @ReturnNo, 
                @PerformedBy = @PerformedBy, 
                @Remarks = 'Returned to vendor';

            -- Deduct from Source
            UPDATE InventoryStocks 
            SET CurrentQty = CurrentQty - @Quantity, 
                LastUpdated = GETDATE()
            WHERE ProductID = @ProductID AND ShelfID = @SourceShelfID;

            FETCH NEXT FROM curItems INTO @ProductID, @SourceShelfID, @Quantity;
        END

        CLOSE curItems;
        DEALLOCATE curItems;

        -- Update Return Status to Shipped
        UPDATE SupplierReturns 
        SET Status = 'Shipped', 
            ReturnDate = GETDATE(),
            UpdatedAt = GETDATE()
        WHERE ReturnID = @ReturnID;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO
