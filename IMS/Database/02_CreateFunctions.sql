-- ==========================================
-- 02_CreateFunctions.sql
-- Inventory Management System (IMS)
-- ==========================================

-- 1. fn_GetAvailableStock
-- AvailableStock = CurrentQty - ReservedQty - QCHoldQty - DamagedQty
CREATE OR ALTER FUNCTION dbo.fn_GetAvailableStock (
    @ProductID INT,
    @ShelfID INT
)
RETURNS DECIMAL(18,2)
AS
BEGIN
    DECLARE @AvailableQty DECIMAL(18,2) = 0;

    SELECT @AvailableQty = (CurrentQty - ReservedQty - QCHoldQty - DamagedQty)
    FROM InventoryStocks
    WHERE ProductID = @ProductID AND ShelfID = @ShelfID;

    RETURN ISNULL(@AvailableQty, 0);
END;
GO

-- 2. fn_GetShelfAvailableCapacity
-- Returns the remaining quantity capacity of a shelf for a specific product
-- Checks MaxQty against current utilized quantity on the shelf
CREATE OR ALTER FUNCTION dbo.fn_GetShelfAvailableCapacity (
    @ShelfID INT
)
RETURNS DECIMAL(18,2)
AS
BEGIN
    DECLARE @MaxQty DECIMAL(18,2) = 0;
    DECLARE @CurrentTotalQty DECIMAL(18,2) = 0;
    DECLARE @AvailableCapacity DECIMAL(18,2) = 0;

    -- Get Shelf Max Capacity
    SELECT @MaxQty = MaxQty
    FROM Shelves
    WHERE ShelfID = @ShelfID AND IsActive = 1;

    -- Get Current Total Qty on Shelf
    SELECT @CurrentTotalQty = SUM(CurrentQty)
    FROM InventoryStocks
    WHERE ShelfID = @ShelfID;

    SET @AvailableCapacity = ISNULL(@MaxQty, 0) - ISNULL(@CurrentTotalQty, 0);
    
    -- Ensure we don't return negative capacity (though it shouldn't happen)
    IF @AvailableCapacity < 0
        SET @AvailableCapacity = 0;

    RETURN @AvailableCapacity;
END;
GO

-- 3. fn_GetStockStatus
-- Returns status: Out of Stock, Low Stock, In Stock
CREATE OR ALTER FUNCTION dbo.fn_GetStockStatus (
    @ProductID INT,
    @ShelfID INT
)
RETURNS NVARCHAR(50)
AS
BEGIN
    DECLARE @AvailableQty DECIMAL(18,2) = 0;
    DECLARE @ReorderLevel DECIMAL(18,2) = 0;
    DECLARE @Status NVARCHAR(50);

    -- Get Available Qty
    SET @AvailableQty = dbo.fn_GetAvailableStock(@ProductID, @ShelfID);

    -- Get Reorder Level
    SELECT @ReorderLevel = ReorderLevel
    FROM Products
    WHERE ProductID = @ProductID;

    IF @AvailableQty <= 0
        SET @Status = 'Out of Stock';
    ELSE IF @AvailableQty <= @ReorderLevel
        SET @Status = 'Low Stock';
    ELSE
        SET @Status = 'In Stock';

    RETURN @Status;
END;
GO
