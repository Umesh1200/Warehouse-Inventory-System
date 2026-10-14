-- ==========================================
-- 05_SeedData.sql
-- Inventory Management System (IMS)
-- ==========================================

BEGIN TRANSACTION;

-- 1. Roles & Users
SET IDENTITY_INSERT Roles ON;
INSERT INTO Roles (RoleID, RoleName, Description) VALUES 
(1, 'Admin', 'System Administrator with full access'),
(2, 'Manager', 'Operational Manager with approval rights'),
(3, 'Warehouse Staff', 'Task-based operational staff');
SET IDENTITY_INSERT Roles OFF;

SET IDENTITY_INSERT Users ON;
INSERT INTO Users (UserID, Username, PasswordHash, FullName, Email, RoleID) VALUES 
(1, 'admin', 'HASHED_PWD_HERE', 'Umesh Chaudhary', 'admin@factoryims.com', 1),
(2, 'arjun', 'HASHED_PWD_HERE', 'Arjun Thapa', 'arjun@factoryims.com', 2),
(3, 'deepak', 'HASHED_PWD_HERE', 'Deepak K.', 'deepak@factoryims.com', 3);
SET IDENTITY_INSERT Users OFF;

-- 2. Master Data (Categories & Units)
SET IDENTITY_INSERT Categories ON;
INSERT INTO Categories (CategoryID, CategoryName) VALUES 
(1, 'Electronics'), (2, 'Mechanical'), (3, 'Packaging'), (4, 'Raw Material');
SET IDENTITY_INSERT Categories OFF;

SET IDENTITY_INSERT Units ON;
INSERT INTO Units (UnitID, UnitName, UnitCode) VALUES 
(1, 'Pieces', 'pcs'), (2, 'Kilograms', 'kg'), (3, 'Meters', 'm'), (4, 'Liters', 'L');
SET IDENTITY_INSERT Units OFF;

-- 3. Warehouse Hierarchy
SET IDENTITY_INSERT Warehouses ON;
INSERT INTO Warehouses (WarehouseID, WarehouseCode, WarehouseName, Location) VALUES 
(1, 'KTM-DC', 'Kathmandu Central DC', 'Maharajgunj, Kathmandu');
SET IDENTITY_INSERT Warehouses OFF;

SET IDENTITY_INSERT Floors ON;
INSERT INTO Floors (FloorID, WarehouseID, FloorCode, FloorName) VALUES 
(1, 1, 'F1', 'Ground Floor'), (2, 1, 'F2', 'Mezzanine');
SET IDENTITY_INSERT Floors OFF;

SET IDENTITY_INSERT Rooms ON;
INSERT INTO Rooms (RoomID, FloorID, RoomCode, RoomName) VALUES 
(1, 1, 'R01', 'Bulk Storage'), (2, 1, 'R02', 'High Value');
SET IDENTITY_INSERT Rooms OFF;

SET IDENTITY_INSERT Racks ON;
INSERT INTO Racks (RackID, RoomID, RackCode, RackName) VALUES 
(1, 1, 'A01', 'Rack A1'), (2, 2, 'B01', 'Secure Rack B1');
SET IDENTITY_INSERT Racks OFF;

SET IDENTITY_INSERT Shelves ON;
INSERT INTO Shelves (ShelfID, RackID, ShelfCode, ShelfType, MaxQty, MaxWeightKg) VALUES 
(1, 1, 'KTM-F1-R01-A01-S1', 'General', 1000, 500),
(2, 1, 'KTM-F1-R01-A01-S2', 'Raw Material', 2000, 1000),
(3, 2, 'KTM-F1-R02-B01-S1', 'Finished Goods', 100, 200),
(4, 2, 'KTM-F1-R02-B01-QC1', 'QC Hold', 50, 100);
SET IDENTITY_INSERT Shelves OFF;

-- 4. Products
SET IDENTITY_INSERT Products ON;
INSERT INTO Products (ProductID, SKU, ProductName, CategoryID, UnitID, ProductType, ReorderLevel, StandardCost) VALUES 
(1, 'PRD-SRV-AX200', 'Industrial Servo Motor - AX200', 1, 1, 'Finished Goods', 10, 35000),
(2, 'RM-COP-005', 'Copper Winding Wire 0.5mm', 4, 3, 'Raw Material', 500, 45),
(3, 'PKG-BOX-MED', 'Standard Shipping Box', 3, 1, 'Consumable', 100, 85);
SET IDENTITY_INSERT Products OFF;

-- 5. Partners
SET IDENTITY_INSERT Suppliers ON;
INSERT INTO Suppliers (SupplierID, SupplierCode, SupplierName, SupplierType) VALUES 
(1, 'SUP-001', 'Acme Global Manufacturing', 'Manufacturer'),
(2, 'SUP-042', 'Himalayan Tech Parts', 'Distributor');
SET IDENTITY_INSERT Suppliers OFF;

SET IDENTITY_INSERT Customers ON;
INSERT INTO Customers (CustomerID, CustomerCode, CustomerName, CustomerType) VALUES 
(1, 'CUS-001', 'Himalayan Logistics Pvt. Ltd.', 'Wholesale'),
(2, 'CUS-042', 'Northwest Retailers', 'Retail');
SET IDENTITY_INSERT Customers OFF;

-- 6. Initial Inventory (Mock Data)
INSERT INTO InventoryStocks (ProductID, ShelfID, CurrentQty) VALUES 
(1, 3, 45),  -- 45 Motors on Finished Goods shelf
(2, 2, 1250), -- 1250m Wire on Raw Material shelf
(3, 1, 850);  -- 850 Boxes on General shelf

-- Log the initial stock as Adjustments (Seed)
INSERT INTO StockMovements (ProductID, WarehouseID, ShelfID, MovementType, Quantity, BeforeQty, AfterQty, ReferenceType, ReferenceNo, PerformedBy, Remarks)
VALUES 
(1, 1, 3, 'AdjustmentIn', 45, 0, 45, 'SystemSeed', 'SYS-001', 1, 'Initial System Seed'),
(2, 1, 2, 'AdjustmentIn', 1250, 0, 1250, 'SystemSeed', 'SYS-001', 1, 'Initial System Seed'),
(3, 1, 1, 'AdjustmentIn', 850, 0, 850, 'SystemSeed', 'SYS-001', 1, 'Initial System Seed');

-- 7. System Settings
INSERT INTO SystemSettings (SettingKey, SettingValue, SettingGroup, Description) VALUES 
('ProductionPagesEnabled', 'true', 'Production', 'Enable or disable Production module pages');

COMMIT TRANSACTION;
GO
