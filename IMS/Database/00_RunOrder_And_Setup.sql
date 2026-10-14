-- ==========================================
-- 00_RunOrder_And_Setup.sql
-- Inventory Management System (IMS)
-- ==========================================

-- 1. Create Database if not exists
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'FactoryIMS')
BEGIN
    CREATE DATABASE FactoryIMS;
    PRINT 'Database FactoryIMS created successfully.';
END
GO

USE FactoryIMS;
GO

/*
================================================================================
SCRIPT EXECUTION ORDER CHECKLIST:
================================================================================
Run the following files in EXACTLY this order to maintain referential integrity:

[ ] 1. 01_CreateTables.sql           - Creates all tables and FK relationships.
[ ] 2. 02_CreateFunctions.sql        - Creates business logic calculation functions.
[ ] 3. 03_CreateStoredProcedures.sql - Creates transactional procedures and audit logic.
[ ] 4. 04_CreateViews.sql            - Creates reporting and dashboard views.
[ ] 5. 05_SeedData.sql               - Populates initial roles, users, and masters.
[ ] 6. 07_SupplierProducts_AVL.sql   - Creates Approved Vendor List mappings.
[ ] 7. 10_OfflineProcurementWorkflow.sql - Adds offline supplier communication, documents, invoices, and payments.
[ ] 8. 11_FinishedGoods_WarehouseWorkflow_And_BOMAutoIssue.sql - Enables Finished Goods QC/Putaway in warehouse operations and BOM material auto-issuance.
================================================================================
*/

PRINT '--- VERIFICATION SUITE ---';

-- 1. Count Core Entities
SELECT 'Roles' as Entity, COUNT(*) as [Count] FROM Roles
UNION ALL
SELECT 'Users', COUNT(*) FROM Users
UNION ALL
SELECT 'Products', COUNT(*) FROM Products
UNION ALL
SELECT 'Warehouses', COUNT(*) FROM Warehouses
UNION ALL
SELECT 'Shelves', COUNT(*) FROM Shelves
UNION ALL
SELECT 'Initial Inventory Rows', COUNT(*) FROM InventoryStocks;

-- 2. Check Inventory Overview View
PRINT '--- Inventory Overview Snapshot ---';
SELECT TOP 5 SKU, ProductName, WarehouseName, ShelfCode, CurrentQty, AvailableQty, StockStatus 
FROM vw_InventoryOverview;

-- 3. Check Dashboard Summary View
PRINT '--- Dashboard Summary Snapshot ---';
SELECT * FROM vw_DashboardSummary;

-- 4. Check Stored Procedures Existence
PRINT '--- Stored Procedures Status ---';
SELECT name, type_desc 
FROM sys.procedures 
WHERE name IN ('sp_RecordStockMovement', 'sp_ConfirmPutaway', 'sp_ReserveStock', 'sp_TransferStock', 'sp_PostAdjustment', 'sp_DispatchStock', 'sp_IssueProductionMaterial', 'sp_ReceiveFinishedGoods');
GO
