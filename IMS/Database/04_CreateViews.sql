USE FactoryIMS;
GO

-- ==========================================
-- 04_CreateViews.sql
-- Inventory Management System (IMS)
-- ==========================================

-- 1. vw_InventoryOverview
CREATE OR ALTER VIEW dbo.vw_InventoryOverview
AS
SELECT 
    p.ProductID,
    p.SKU,
    p.ProductName,
    c.CategoryName,
    p.ProductType,
    w.WarehouseName,
    s.ShelfCode,
    s.ShelfType,
    inv.CurrentQty,
    inv.ReservedQty,
    inv.QCHoldQty,
    inv.DamagedQty,
    dbo.fn_GetAvailableStock(p.ProductID, s.ShelfID) AS AvailableQty,
    dbo.fn_GetStockStatus(p.ProductID, s.ShelfID) AS StockStatus,
    p.ReorderLevel,
    p.StandardCost,
    (inv.CurrentQty * p.StandardCost) AS TotalValue
FROM InventoryStocks inv
INNER JOIN Products p ON inv.ProductID = p.ProductID
INNER JOIN Categories c ON p.CategoryID = c.CategoryID
INNER JOIN Shelves s ON inv.ShelfID = s.ShelfID
INNER JOIN Racks r ON s.RackID = r.RackID
INNER JOIN Rooms rm ON r.RoomID = rm.RoomID
INNER JOIN Floors f ON rm.FloorID = f.FloorID
INNER JOIN Warehouses w ON f.WarehouseID = w.WarehouseID
WHERE p.Status = 'Active';
GO

-- 2. vw_LowStockItems
CREATE OR ALTER VIEW dbo.vw_LowStockItems
AS
SELECT 
    ProductID,
    SKU,
    ProductName,
    CategoryName,
    WarehouseName,
    ShelfCode,
    AvailableQty,
    ReorderLevel,
    StockStatus
FROM dbo.vw_InventoryOverview
WHERE StockStatus IN ('Low Stock', 'Out of Stock');
GO

-- 3. vw_ShelfUtilization
CREATE OR ALTER VIEW dbo.vw_ShelfUtilization
AS
SELECT 
    w.WarehouseName,
    f.FloorName,
    rm.RoomName,
    r.RackName,
    s.ShelfID,
    s.ShelfCode,
    s.ShelfType,
    s.MaxQty,
    ISNULL(SUM(inv.CurrentQty), 0) AS CurrentTotalQty,
    CASE 
        WHEN s.MaxQty > 0 
            THEN (ISNULL(SUM(inv.CurrentQty), 0) / s.MaxQty) * 100 
        ELSE 0 
    END AS UtilizationPercentage
FROM Shelves s
INNER JOIN Racks r ON s.RackID = r.RackID
INNER JOIN Rooms rm ON r.RoomID = rm.RoomID
INNER JOIN Floors f ON rm.FloorID = f.FloorID
INNER JOIN Warehouses w ON f.WarehouseID = w.WarehouseID
LEFT JOIN InventoryStocks inv ON s.ShelfID = inv.ShelfID
WHERE s.IsActive = 1
GROUP BY 
    w.WarehouseName,
    f.FloorName,
    rm.RoomName,
    r.RackName,
    s.ShelfID,
    s.ShelfCode,
    s.ShelfType,
    s.MaxQty;
GO

-- 4. vw_DashboardSummary
CREATE OR ALTER VIEW dbo.vw_DashboardSummary
AS
SELECT 
    (SELECT COUNT(*) FROM Products WHERE Status = 'Active') AS TotalActiveProducts,
    (SELECT COUNT(*) FROM dbo.vw_LowStockItems) AS TotalLowStockAlerts,
    (SELECT ISNULL(SUM(TotalValue), 0) FROM dbo.vw_InventoryOverview) AS TotalInventoryValue,
    (SELECT COUNT(*) FROM PurchaseOrders WHERE Status IN ('Draft', 'Confirmed', 'InTransit')) AS PendingPurchaseOrders,
    (SELECT COUNT(*) FROM SalesOrders WHERE Status IN ('Confirmed', 'Allocated', 'ReadyToPick', 'Picking', 'Packed')) AS PendingSalesOrders,
    (SELECT COUNT(*) FROM Dispatches WHERE CAST(DispatchDate AS DATE) = CAST(GETDATE() AS DATE)) AS DailyShipments;
GO

-- 5. vw_StockMovementReport
CREATE OR ALTER VIEW dbo.vw_StockMovementReport
AS
SELECT 
    m.MovementID,
    m.CreatedAt AS MovementDate,
    p.SKU,
    p.ProductName,
    w.WarehouseName,
    s.ShelfCode,
    m.MovementType,
    m.Quantity,
    m.BeforeQty,
    m.AfterQty,
    m.ReferenceType,
    m.ReferenceNo,
    u.FullName AS PerformedBy,
    m.Remarks
FROM StockMovements m
INNER JOIN Products p ON m.ProductID = p.ProductID
INNER JOIN Warehouses w ON m.WarehouseID = w.WarehouseID
INNER JOIN Shelves s ON m.ShelfID = s.ShelfID
LEFT JOIN Users u ON m.PerformedBy = u.UserID;
GO