# Factory IMS - Database Setup Guide

This directory contains the SQL Server schema scripts required to build the backend for the Factory Inventory Management System.

## Prerequisites
- SQL Server (2019 or later recommended)
- SQL Server Management Studio (SSMS) or Azure Data Studio

## Setup Instructions

### 1. Initialize the Database
Open **`00_RunOrder_And_Setup.sql`** in SSMS and execute it.
- This will create the `FactoryIMS` database if it doesn't exist.
- It also serves as your checklist for the following steps.

### 2. Run Schema Scripts in Order
Open and execute the following files **sequentially**. Do not skip any steps, as the scripts have strict referential dependencies.

1.  **`01_CreateTables.sql`**
    - Creates Security, Master Data, Warehouse, Inventory, and Production tables.
2.  **`02_CreateFunctions.sql`**
    - Implements stock availability and shelf capacity calculation logic.
3.  **`03_CreateStoredProcedures.sql`**
    - Implements core business logic for stock movements, putaway, and dispatch.
4.  **`04_CreateViews.sql`**
    - Sets up reporting views for inventory and dashboard KPIs.
5.  **`05_SeedData.sql`**
    - Populates the database with default roles, users (admin/arjun/deepak), and sample products.
6.  **`06_WarehouseProductStorageSetup.sql`**
    - Adds product storage rules, realistic seed weights, corrected shelf weight capacity, and shelf weight-capacity function used by putaway and transfer validation.
7.  **`10_OfflineProcurementWorkflow.sql`**
    - Adds staff-managed PO communication, supplier confirmation, delivery documents, supplier invoices, three-way matching support, and payments.

### 3. Verification
After running script `05`, go back to **`00_RunOrder_And_Setup.sql`** and run the **VERIFICATION SUITE** at the bottom of the file.
- You should see counts for 3 Roles, 3 Users, and sample Inventory items.
- The `vw_InventoryOverview` should show the initial stock seed.

## Troubleshooting
- **Foreign Key Errors:** Ensure you ran `01_CreateTables.sql` first.
- **Function/Proc Missing:** Ensure you didn't miss `02` or `03`.
- **Permission Errors:** Ensure your SQL user has `db_owner` or `CREATE` permissions on the instance.

---

**Note:** The Razor Pages UI is currently using mock data. Once the database is ready, the next phase will involve connecting the C# Service Layer to these SQL objects.
