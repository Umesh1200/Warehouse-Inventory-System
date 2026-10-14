-- ==========================================
-- 03_CreateStoredProcedures.sql
-- Inventory Management System (IMS)
-- ==========================================

-- 1. sp_RecordStockMovement
-- Base procedure to log movements. Used internally by other SPs.
CREATE OR ALTER PROCEDURE dbo.sp_RecordStockMovement (
    @ProductID INT,
    @ShelfID INT,
    @MovementType NVARCHAR(50),
    @Quantity DECIMAL(18,2),
    @ReferenceType NVARCHAR(50),
    @ReferenceNo NVARCHAR(100),
    @BatchID INT = NULL,
    @PerformedBy INT,
    @Remarks NVARCHAR(255) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @WarehouseID INT;
    DECLARE @BeforeQty DECIMAL(18,2) = 0;
    DECLARE @AfterQty DECIMAL(18,2) = 0;

    -- Get WarehouseID from ShelfID
    SELECT @WarehouseID = f.WarehouseID
    FROM Shelves s
    INNER JOIN Racks r ON s.RackID = r.RackID
    INNER JOIN Rooms rm ON r.RoomID = rm.RoomID
    INNER JOIN Floors f ON rm.FloorID = f.FloorID
    WHERE s.ShelfID = @ShelfID;

    -- Get BeforeQty
    SELECT @BeforeQty = CurrentQty
    FROM InventoryStocks
    WHERE ProductID = @ProductID AND ShelfID = @ShelfID;

    SET @BeforeQty = ISNULL(@BeforeQty, 0);

    -- Calculate AfterQty based on movement type logic
    -- (The actual update to InventoryStocks happens in the calling SPs, 
    -- but we calculate the expected AfterQty for the log here)
    IF @MovementType IN ('StockIn', 'TransferIn', 'AdjustmentIn', 'ReturnIn', 'ProductionReturn')
        SET @AfterQty = @BeforeQty + @Quantity;
    ELSE IF @MovementType IN ('StockOut', 'TransferOut', 'AdjustmentOut', 'DamageOut', 'ProductionIssue', 'Dispatch')
        SET @AfterQty = @BeforeQty - @Quantity;
    ELSE -- Reserve, Unreserve don't change CurrentQty, they change ReservedQty
        SET @AfterQty = @BeforeQty;

    -- Insert Log
    INSERT INTO StockMovements (
        ProductID, WarehouseID, ShelfID, MovementType, Quantity, BeforeQty, AfterQty, ReferenceType, ReferenceNo, BatchID, PerformedBy, Remarks
    )
    VALUES (
        @ProductID, @WarehouseID, @ShelfID, @MovementType, @Quantity, @BeforeQty, @AfterQty, @ReferenceType, @ReferenceNo, @BatchID, @PerformedBy, @Remarks
    );
END;
GO

-- 2. sp_ConfirmPutaway
-- Increases usable stock after QC acceptance
CREATE OR ALTER PROCEDURE dbo.sp_ConfirmPutaway (
    @TaskID INT,
    @ActualShelfID INT,
    @PerformedBy INT
)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;

    BEGIN TRY
        DECLARE @ProductID INT;
        DECLARE @Qty DECIMAL(18,2);
        DECLARE @TaskNo NVARCHAR(50);
        DECLARE @AvailableCapacity DECIMAL(18,2);

        SELECT @ProductID = ProductID, @Qty = QtyToPutaway, @TaskNo = TaskNo
        FROM PutawayTasks
        WHERE TaskID = @TaskID AND Status = 'Pending';

        IF @ProductID IS NULL
            THROW 50001, 'Invalid Putaway Task or already completed.', 1;

        -- Check Shelf Capacity
        SET @AvailableCapacity = dbo.fn_GetShelfAvailableCapacity(@ActualShelfID);
        IF @AvailableCapacity < @Qty
            THROW 50002, 'Insufficient shelf capacity for putaway.', 1;

        -- Ensure Stock Record Exists
        IF NOT EXISTS (SELECT 1 FROM InventoryStocks WHERE ProductID = @ProductID AND ShelfID = @ActualShelfID)
        BEGIN
            INSERT INTO InventoryStocks (ProductID, ShelfID, CurrentQty)
            VALUES (@ProductID, @ActualShelfID, 0);
        END

        -- Log Movement
        EXEC dbo.sp_RecordStockMovement 
            @ProductID = @ProductID, @ShelfID = @ActualShelfID, @MovementType = 'StockIn', 
            @Quantity = @Qty, @ReferenceType = 'Putaway', @ReferenceNo = @TaskNo, 
            @PerformedBy = @PerformedBy, @Remarks = 'Putaway confirmed';

        -- Update Stock
        UPDATE InventoryStocks
        SET CurrentQty = CurrentQty + @Qty,
            LastUpdated = GETDATE()
        WHERE ProductID = @ProductID AND ShelfID = @ActualShelfID;

        -- Update Task Status
        UPDATE PutawayTasks
        SET Status = 'Completed', ActualShelfID = @ActualShelfID, CompletedDate = GETDATE()
        WHERE TaskID = @TaskID;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

-- 3. sp_ReserveStock
-- Reserves stock for Sales Orders
CREATE OR ALTER PROCEDURE dbo.sp_ReserveStock (
    @SOID INT,
    @ProductID INT,
    @ShelfID INT,
    @QtyToReserve DECIMAL(18,2),
    @PerformedBy INT
)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;

    BEGIN TRY
        DECLARE @AvailableQty DECIMAL(18,2);
        DECLARE @SONumber NVARCHAR(50);

        SELECT @SONumber = SONumber FROM SalesOrders WHERE SOID = @SOID;

        -- Check Available Stock
        SET @AvailableQty = dbo.fn_GetAvailableStock(@ProductID, @ShelfID);
        IF @AvailableQty < @QtyToReserve
            THROW 50003, 'Insufficient available stock for reservation.', 1;

        -- Update Reserved Qty
        UPDATE InventoryStocks
        SET ReservedQty = ReservedQty + @QtyToReserve,
            LastUpdated = GETDATE()
        WHERE ProductID = @ProductID AND ShelfID = @ShelfID;

        -- Create Reservation Record
        INSERT INTO StockReservations (ProductID, ShelfID, ReferenceType, ReferenceNo, ReservedQty, CreatedBy)
        VALUES (@ProductID, @ShelfID, 'SalesOrder', @SONumber, @QtyToReserve, @PerformedBy);

        -- Log Movement (Reserve)
        EXEC dbo.sp_RecordStockMovement 
            @ProductID = @ProductID, @ShelfID = @ShelfID, @MovementType = 'Reserve', 
            @Quantity = @QtyToReserve, @ReferenceType = 'SalesOrder', @ReferenceNo = @SONumber, 
            @PerformedBy = @PerformedBy, @Remarks = 'Stock reserved for SO';

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

-- 4. sp_TransferStock
CREATE OR ALTER PROCEDURE dbo.sp_TransferStock (
    @TransferID INT,
    @PerformedBy INT
)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;

    BEGIN TRY
        DECLARE @TransferNo NVARCHAR(50), @Status NVARCHAR(50);
        SELECT @TransferNo = TransferNo, @Status = Status FROM Transfers WHERE TransferID = @TransferID;

        IF @TransferNo IS NULL OR @Status NOT IN ('Approved', 'Pending')
            THROW 50004, 'Invalid Transfer or already processed.', 1;

        -- Cursor to process items
        DECLARE @ProductID INT, @FromShelfID INT, @ToShelfID INT, @Quantity DECIMAL(18,2), @ProductWeight DECIMAL(18,2);
        
        DECLARE curItems CURSOR FOR 
        SELECT ProductID, FromShelfID, ToShelfID, Quantity FROM TransferItems WHERE TransferID = @TransferID;

        OPEN curItems;
        FETCH NEXT FROM curItems INTO @ProductID, @FromShelfID, @ToShelfID, @Quantity;

        WHILE @@FETCH_STATUS = 0
        BEGIN
            -- Check Source Stock
            IF dbo.fn_GetAvailableStock(@ProductID, @FromShelfID) < @Quantity
                THROW 50005, 'Insufficient available stock at source shelf.', 1;

            -- Check Dest Quantity Capacity
            IF dbo.fn_GetShelfAvailableCapacity(@ToShelfID) < @Quantity
                THROW 50006, 'Insufficient quantity capacity at destination shelf.', 1;

            -- Check Dest Weight Capacity
            SELECT @ProductWeight = ISNULL(WeightKg, 0) FROM Products WHERE ProductID = @ProductID;
            IF @ProductWeight > 0 AND dbo.fn_GetShelfAvailableWeightCapacity(@ToShelfID) < (@Quantity * @ProductWeight)
                THROW 50014, 'Insufficient weight capacity at destination shelf.', 1;

            -- Add to Dest (Create record if not exists)
            IF NOT EXISTS (SELECT 1 FROM InventoryStocks WHERE ProductID = @ProductID AND ShelfID = @ToShelfID)
                INSERT INTO InventoryStocks (ProductID, ShelfID, CurrentQty) VALUES (@ProductID, @ToShelfID, 0);

            -- Log Movements
            EXEC dbo.sp_RecordStockMovement @ProductID, @FromShelfID, 'TransferOut', @Quantity, 'Transfer', @TransferNo, NULL, @PerformedBy, 'Transfer Out';
            EXEC dbo.sp_RecordStockMovement @ProductID, @ToShelfID, 'TransferIn', @Quantity, 'Transfer', @TransferNo, NULL, @PerformedBy, 'Transfer In';

            -- Deduct from Source
            UPDATE InventoryStocks SET CurrentQty = CurrentQty - @Quantity, LastUpdated = GETDATE()
            WHERE ProductID = @ProductID AND ShelfID = @FromShelfID;
            
            UPDATE InventoryStocks SET CurrentQty = CurrentQty + @Quantity, LastUpdated = GETDATE()
            WHERE ProductID = @ProductID AND ShelfID = @ToShelfID;

            FETCH NEXT FROM curItems INTO @ProductID, @FromShelfID, @ToShelfID, @Quantity;
        END

        CLOSE curItems;
        DEALLOCATE curItems;

        -- Update Transfer Status
        UPDATE Transfers SET Status = 'Completed', CompletionDate = GETDATE() WHERE TransferID = @TransferID;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

-- 5. sp_PostAdjustment
CREATE OR ALTER PROCEDURE dbo.sp_PostAdjustment (
    @AdjustmentID INT,
    @PerformedBy INT
)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;

    BEGIN TRY
        DECLARE @AdjustmentNo NVARCHAR(50), @AdjType NVARCHAR(50);
        SELECT @AdjustmentNo = AdjustmentNo, @AdjType = AdjustmentType FROM Adjustments WHERE AdjustmentID = @AdjustmentID AND Status = 'Approved';

        IF @AdjustmentNo IS NULL
            THROW 50007, 'Adjustment must be approved before posting.', 1;

        DECLARE @ProductID INT, @ShelfID INT, @DifferenceQty DECIMAL(18,2), @AbsDiff DECIMAL(18,2);
        
        DECLARE curAdj CURSOR FOR 
        SELECT ProductID, ShelfID, (CountedQty - SystemQty) FROM AdjustmentItems WHERE AdjustmentID = @AdjustmentID;

        OPEN curAdj;
        FETCH NEXT FROM curAdj INTO @ProductID, @ShelfID, @DifferenceQty;

        WHILE @@FETCH_STATUS = 0
        BEGIN
            SET @AbsDiff = ABS(@DifferenceQty);

            IF @DifferenceQty > 0 -- AdjustmentIn
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM InventoryStocks WHERE ProductID = @ProductID AND ShelfID = @ShelfID)
                    INSERT INTO InventoryStocks (ProductID, ShelfID, CurrentQty) VALUES (@ProductID, @ShelfID, 0);
                
                EXEC dbo.sp_RecordStockMovement @ProductID, @ShelfID, 'AdjustmentIn', @AbsDiff, 'Adjustment', @AdjustmentNo, NULL, @PerformedBy, 'Positive Variance';

                UPDATE InventoryStocks SET CurrentQty = CurrentQty + @AbsDiff, LastUpdated = GETDATE()
                WHERE ProductID = @ProductID AND ShelfID = @ShelfID;
            END
            ELSE IF @DifferenceQty < 0 -- AdjustmentOut
            BEGIN
                -- Ensure it doesn't go negative
                DECLARE @CurrentQty DECIMAL(18,2);
                SELECT @CurrentQty = CurrentQty FROM InventoryStocks WHERE ProductID = @ProductID AND ShelfID = @ShelfID;
                
                IF @CurrentQty < @AbsDiff
                    THROW 50008, 'Adjustment out would result in negative stock.', 1;

                EXEC dbo.sp_RecordStockMovement @ProductID, @ShelfID, 'AdjustmentOut', @AbsDiff, 'Adjustment', @AdjustmentNo, NULL, @PerformedBy, 'Negative Variance';

                UPDATE InventoryStocks SET CurrentQty = CurrentQty - @AbsDiff, LastUpdated = GETDATE()
                WHERE ProductID = @ProductID AND ShelfID = @ShelfID;
            END

            FETCH NEXT FROM curAdj INTO @ProductID, @ShelfID, @DifferenceQty;
        END

        CLOSE curAdj;
        DEALLOCATE curAdj;

        UPDATE Adjustments SET Status = 'Posted' WHERE AdjustmentID = @AdjustmentID;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

-- 6. sp_DispatchStock
-- Reduces stock and consumes reservation
CREATE OR ALTER PROCEDURE dbo.sp_DispatchStock (
    @DispatchID INT,
    @PerformedBy INT
)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;

    BEGIN TRY
        DECLARE @SOID INT, @SONumber NVARCHAR(50);
        SELECT @SOID = SOID FROM Dispatches WHERE DispatchID = @DispatchID AND Status = 'Ready To Dispatch';
        SELECT @SONumber = SONumber FROM SalesOrders WHERE SOID = @SOID;

        IF @SOID IS NULL
            THROW 50009, 'Invalid dispatch or not in ready state.', 1;

        -- To keep this simple for the schema, we assume we look up reservations for this SO
        DECLARE @ProductID INT, @ShelfID INT, @ReservedQty DECIMAL(18,2), @ResID INT;
        DECLARE @Dispatched TABLE (ProductID INT, Quantity DECIMAL(18,2));
        
        DECLARE curRes CURSOR FOR 
        SELECT ReservationID, ProductID, ShelfID, ReservedQty FROM StockReservations 
        WHERE ReferenceType = 'SalesOrder' AND ReferenceNo = @SONumber AND Status = 'Active';

        OPEN curRes;
        FETCH NEXT FROM curRes INTO @ResID, @ProductID, @ShelfID, @ReservedQty;

        WHILE @@FETCH_STATUS = 0
        BEGIN
            -- Ensure enough current qty
            DECLARE @CurrentQty DECIMAL(18,2);
            SELECT @CurrentQty = CurrentQty FROM InventoryStocks WHERE ProductID = @ProductID AND ShelfID = @ShelfID;
            
            IF @CurrentQty < @ReservedQty
                THROW 50010, 'Cannot dispatch: Insufficient physical stock.', 1;

            -- Log Movement
            EXEC dbo.sp_RecordStockMovement @ProductID, @ShelfID, 'Dispatch', @ReservedQty, 'SalesOrder', @SONumber, NULL, @PerformedBy, 'Order Dispatched';

            -- Deduct CurrentQty and ReservedQty
            UPDATE InventoryStocks 
            SET CurrentQty = CurrentQty - @ReservedQty, 
                ReservedQty = ReservedQty - @ReservedQty,
                LastUpdated = GETDATE()
            WHERE ProductID = @ProductID AND ShelfID = @ShelfID;

            -- Update Reservation Status
            UPDATE StockReservations SET Status = 'Consumed' WHERE ReservationID = @ResID;
            INSERT INTO @Dispatched (ProductID, Quantity) VALUES (@ProductID, @ReservedQty);

            FETCH NEXT FROM curRes INTO @ResID, @ProductID, @ShelfID, @ReservedQty;
        END

        CLOSE curRes;
        DEALLOCATE curRes;

        UPDATE soi
        SET DispatchedQty = CASE
                WHEN soi.DispatchedQty + d.Quantity > soi.OrderedQty THEN soi.OrderedQty
                ELSE soi.DispatchedQty + d.Quantity
            END
        FROM SalesOrderItems soi
        INNER JOIN (
            SELECT ProductID, SUM(Quantity) AS Quantity
            FROM @Dispatched
            GROUP BY ProductID
        ) d ON soi.ProductID = d.ProductID
        WHERE soi.SOID = @SOID;

        -- Update Statuses
        UPDATE Dispatches SET Status = 'Dispatched', DispatchDate = GETDATE() WHERE DispatchID = @DispatchID;
        UPDATE SalesOrders SET Status = 'Dispatched' WHERE SOID = @SOID;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

-- 7. sp_IssueProductionMaterial
CREATE OR ALTER PROCEDURE dbo.sp_IssueProductionMaterial (
    @IssueID INT,
    @PerformedBy INT
)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;

    BEGIN TRY
        DECLARE @ProductID INT, @ShelfID INT, @Qty DECIMAL(18,2), @PlanID INT;
        SELECT @ProductID = ProductID, @ShelfID = ShelfID, @Qty = RequestedQty, @PlanID = PlanID 
        FROM ProductionMaterialIssues WHERE IssueID = @IssueID AND Status = 'Pending';

        IF @ProductID IS NULL
            THROW 50011, 'Invalid issue request.', 1;

        DECLARE @PlanNo NVARCHAR(50);
        SELECT @PlanNo = PlanNumber FROM ProductionPlans WHERE PlanID = @PlanID;

        -- Check Availability
        IF dbo.fn_GetAvailableStock(@ProductID, @ShelfID) < @Qty
            THROW 50012, 'Insufficient stock for production issue.', 1;

        -- Log Movement
        EXEC dbo.sp_RecordStockMovement @ProductID, @ShelfID, 'ProductionIssue', @Qty, 'ProductionPlan', @PlanNo, NULL, @PerformedBy, 'Issued to Production';

        -- Deduct Stock
        UPDATE InventoryStocks SET CurrentQty = CurrentQty - @Qty, LastUpdated = GETDATE()
        WHERE ProductID = @ProductID AND ShelfID = @ShelfID;

        UPDATE ProductionMaterialIssues SET IssuedQty = @Qty, Status = 'Issued', IssuedBy = @PerformedBy, IssueDate = GETDATE() WHERE IssueID = @IssueID;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

-- 8. sp_ReceiveFinishedGoods
CREATE OR ALTER PROCEDURE dbo.sp_ReceiveFinishedGoods (
    @FGReceiptID INT,
    @PerformedBy INT
)
AS
BEGIN
    SET NOCOUNT ON;
    -- Note: This procedure logs the receipt. The actual usable stock increase 
    -- happens when these items clear QC and go through Putaway.
    BEGIN TRANSACTION;

    BEGIN TRY
        DECLARE @Status NVARCHAR(50);
        SELECT @Status = Status FROM FinishedGoodsReceipts WHERE FGReceiptID = @FGReceiptID;

        IF @Status != 'Pending QC'
            THROW 50013, 'Receipt is not in pending state.', 1;

        -- In a full flow, this would insert into QCChecks similar to Purchasing ReceivingItems.
        -- For now, we update the status to show it entered the warehouse workflow.
        UPDATE FinishedGoodsReceipts SET Status = 'QC Required' WHERE FGReceiptID = @FGReceiptID;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO
