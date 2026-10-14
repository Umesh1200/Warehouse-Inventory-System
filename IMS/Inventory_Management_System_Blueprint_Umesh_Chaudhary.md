**Inventory Management System  
Detailed Project Blueprint**

Razor Pages \| SQL Server \| Layered Architecture \| Reusable SQL
Procedures

| **Prepared for**  | Umesh Chaudhary                                 |
|-------------------|-------------------------------------------------|
| **Project Type**  | Factory / Warehouse Inventory Management System |
| **Frontend**      | ASP.NET Core Razor Pages, HTML, CSS, JavaScript |
| **Database**      | SQL Server                                      |
| **Prepared Date** | June 11, 2026                                   |

*Purpose: To provide a complete design guide for building a maintainable
IMS using Razor Pages, SQL Server tables, views, functions, and stored
procedures.*

# Table of Contents

1.  1\. Executive Summary

2.  2\. Main Users and Responsibilities

3.  3\. Core Modules

4.  4\. Master Data

5.  5\. Inventory Core Operations

6.  6\. Purchasing Process

7.  7\. Sales and Dispatch Process

8.  8\. Production Support

9.  9\. Reporting Module

10. 10\. Full Architecture

11. 11\. Razor Pages Structure

12. 12\. Database Design

13. 13\. Reusable SQL Functions

14. 14\. Reusable Stored Procedures

15. 15\. SQL Views

16. 16\. Interrelated Workflows

17. 17\. Business Rules

18. 18\. Development Roadmap

19. 19\. References

# 1. Executive Summary

This document describes the full design for an Inventory Management
System (IMS) suitable for a factory, warehouse, or garment-related
business. The system is designed using ASP.NET Core Razor Pages for the
web application and SQL Server for the database. The architecture
separates presentation, business logic, data access, and database logic
to keep the project reusable and maintainable.

The system does not only store product quantities. It manages the
complete inventory life cycle: purchasing, receiving, quality control,
putaway, storage, transfers, reservations, picking, packing, dispatch,
adjustments, cycle counting, reports, and production material support.

| **Area**   | **Main Purpose**                                                                            |
|------------|---------------------------------------------------------------------------------------------|
| Inventory  | Track stock by product, warehouse, shelf, reserved quantity, QC hold, and damaged quantity. |
| Warehouse  | Manage floor, room, rack, and shelf-level storage.                                          |
| Purchasing | Create purchase requests and purchase orders, then receive goods.                           |
| Sales      | Create sales orders, reserve stock, pick, pack, and dispatch.                               |
| Production | Use BOM to calculate and issue raw materials, then receive finished goods.                  |
| Reports    | Provide stock, low-stock, movement, utilization, purchase, and sales reports.               |

# 2. Main Users and Responsibilities

| **User**        | **Main Responsibilities**                                                                                                      | **Access Level**    |
|-----------------|--------------------------------------------------------------------------------------------------------------------------------|---------------------|
| Admin           | Manage users, roles, products, suppliers, customers, warehouses, orders, approvals, reports, and settings.                     | Full access         |
| Manager         | Approve purchase requests, monitor stock, approve adjustments, assign tasks, review reports, and monitor warehouse operations. | Operational control |
| Warehouse Staff | Receive goods, perform QC, putaway, pick, pack, dispatch, transfer stock, perform cycle count, and request adjustments.        | Task-based access   |

# 3. Core Modules Required in the IMS

| **Module**         | **Description**                                                                               |
|--------------------|-----------------------------------------------------------------------------------------------|
| Dashboard          | Role-based summary of stock, orders, low stock, pending approvals, and daily tasks.           |
| Product/SKU Master | Stores all raw materials, finished goods, consumables, barcodes, reorder levels, and pricing. |
| Warehouse Layout   | Manages warehouse, floor, room, rack, shelf, capacity, and shelf type.                        |
| Inventory Overview | Displays current, reserved, available, QC hold, and damaged stock.                            |
| Stock Movement     | Permanent ledger for every stock increase, decrease, transfer, reservation, and adjustment.   |
| Receiving          | Records goods received from suppliers or production.                                          |
| QC Check           | Separates accepted, rejected, and damaged quantities before stock becomes usable.             |
| Putaway            | Places accepted stock into shelves and increases usable inventory.                            |
| Transfers          | Moves stock from shelf to shelf, room to room, or warehouse to warehouse.                     |
| Adjustments        | Corrects stock differences after approval.                                                    |
| Cycle Count        | Supports regular physical stock counting and variance tracking.                               |
| Sales Order        | Manages customer orders, stock reservation, picking, packing, and dispatch.                   |
| Production Support | Uses BOM for material requirement, raw material issue, and finished goods receipt.            |
| Reports            | Provides management-level stock, purchase, sales, warehouse, QC, and valuation reports.       |

# 4. Master Data Design

Master data is the base information used by all operational modules. A
clean master data structure makes the whole system easier to maintain.

## Product / SKU Master

- ProductId

- SKU

- ProductName

- CategoryId

- Brand

- UnitId

- Barcode

- ProductType

- ReorderLevel

- ReorderQuantity

- MinimumStock

- MaximumStock

- StandardCost

- SellingPrice

- Weight

- Length

- Width

- Height

- IsActive

## Supplier Master

- SupplierId

- SupplierName

- ContactPerson

- Phone

- Email

- Address

- PAN/VAT Number

- PaymentTerms

- LeadTimeDays

- Status

## Customer Master

- CustomerId

- CustomerName

- Phone

- Email

- Address

- CustomerType

- Status

## Warehouse Master

- WarehouseId

- WarehouseName

- Location

- ManagerId

- Contact

- IsActive

## Shelf Master

- ShelfId

- WarehouseId

- FloorId

- RoomId

- RackId

- ShelfCode

- MaxQuantity

- MaxWeightKg

- CurrentQuantity

- CurrentWeightKg

- ItemTypeAllowed

- IsQCHoldShelf

- IsActive

# 5. Inventory Core Operations

## 5.1 Inventory Overview

The inventory overview page should show stock position by product,
warehouse, and shelf. It should calculate available stock using current
quantity, reserved quantity, QC hold quantity, and damaged quantity.

AvailableStock = CurrentStock - ReservedStock - QCHoldStock -
DamagedStock

| **Column**   | **Purpose**                                              |
|--------------|----------------------------------------------------------|
| CurrentQty   | Total physical stock recorded in the system.             |
| ReservedQty  | Stock reserved for confirmed sales orders.               |
| QCHoldQty    | Stock under quality control or hold.                     |
| DamagedQty   | Stock that cannot be sold or used.                       |
| AvailableQty | Stock available for sale, transfer, or production issue. |

## 5.2 Stock Movement

Stock movement is the audit trail of inventory. Every stock change must
insert one record in the StockMovements table. Stock should never be
updated silently.

- StockIn

- StockOut

- Reserve

- Unreserve

- TransferOut

- TransferIn

- AdjustmentIn

- AdjustmentOut

- ReturnIn

- DamageOut

- ProductionIssue

- ProductionReturn

## 5.3 Receiving

### Receiving Flow

**Purchase Order Confirmed** → **Goods Arrive** → **Staff Receives** →
**QC Check** → **Putaway**

- Purchase Order Number

- Supplier

- Product

- Ordered Quantity

- Received Quantity

- Damaged Quantity

- Rejected Quantity

- Batch Number

- Received By

- Remarks

## 5.4 QC Check

### QC Flow

**Received Items** → **Inspection** → **Accepted / Rejected / Damaged**
→ **Accepted to Putaway** → **Rejected/Damaged to Hold**

## 5.5 Putaway

### Putaway Flow

**QC Accepted** → **Shelf Suggested** → **Staff Selects Shelf** →
**Stock Increased** → **Movement Recorded**

## 5.6 Transfers, Adjustments, and Cycle Counts

| **Operation** | **Purpose**                                                        | **Approval Needed**                           |
|---------------|--------------------------------------------------------------------|-----------------------------------------------|
| Transfer      | Move stock between shelves, rooms, racks, or warehouses.           | Optional / Manager for warehouse-to-warehouse |
| Adjustment    | Correct stock difference due to damage, loss, or counting mistake. | Yes                                           |
| Cycle Count   | Regular physical verification of selected stock or shelves.        | Manager creates task                          |

# 6. Purchasing Process

### Purchase to Stock Flow

**Purchase Request** → **Purchase Order** → **Receiving** → **QC Check**
→ **Putaway** → **Stock Increased**

| **Table**            | **Main Fields**                                                                                            |
|----------------------|------------------------------------------------------------------------------------------------------------|
| PurchaseRequests     | PurchaseRequestId, RequestedBy, RequestDate, Status, Remarks                                               |
| PurchaseRequestItems | PurchaseRequestItemId, PurchaseRequestId, ProductId, RequestedQty, Reason                                  |
| PurchaseOrders       | PurchaseOrderId, PONumber, SupplierId, OrderDate, ExpectedDate, Status, CreatedBy, ApprovedBy, TotalAmount |
| PurchaseOrderItems   | PurchaseOrderItemId, PurchaseOrderId, ProductId, OrderedQty, ReceivedQty, UnitCost, LineTotal              |
| ReceivingItems       | ReceivingItemId, PurchaseOrderItemId, ReceivedQty, DamagedQty, RejectedQty, ReceivedBy, ReceivedAt         |

# 7. Sales and Dispatch Process

### Sales to Dispatch Flow

**Sales Order** → **Stock Check** → **Reservation** → **Picking** →
**Packing** → **Dispatch** → **Stock Decreased**

| **Table**         | **Main Fields**                                                                             |
|-------------------|---------------------------------------------------------------------------------------------|
| SalesOrders       | SalesOrderId, SONumber, CustomerId, OrderDate, RequiredDate, Status, CreatedBy, TotalAmount |
| SalesOrderItems   | SalesOrderItemId, SalesOrderId, ProductId, OrderQty, ReservedQty, PickedQty, UnitPrice      |
| StockReservations | ReservationId, SalesOrderItemId, ProductId, WarehouseId, ShelfId, ReservedQty, Status       |
| PickTasks         | PickTaskId, SalesOrderId, ProductId, ShelfId, RequiredQty, PickedQty, AssignedTo, Status    |
| Dispatches        | DispatchId, SalesOrderId, CourierName, VehicleNo, TrackingNo, DispatchedBy, DispatchedAt    |

# 8. Production Support Module

For a garment or factory IMS, production must connect with inventory.
Raw materials are issued to production based on a Bill of Materials
(BOM), and finished goods are received back into inventory after
production and QC.

### Production Material Flow

**Production Plan** → **BOM Calculation** → **Material Availability
Check** → **Material Issue** → **Finished Goods Receive** → **QC** →
**Putaway**

| **Production Object**   | **Purpose**                                              |
|-------------------------|----------------------------------------------------------|
| BOM                     | Defines raw materials required for one finished product. |
| ProductionMaterialIssue | Issues raw materials from warehouse to production.       |
| FinishedGoodsReceipt    | Receives completed goods back into inventory.            |
| ProductionReturn        | Returns unused raw materials back to warehouse.          |

# 9. Reporting Module

- Current stock report

- Low stock report

- Out of stock report

- Stock movement report

- Purchase report

- Sales report

- Supplier performance report

- Warehouse utilization report

- Shelf capacity report

- QC report

- Adjustment report

- Cycle count variance report

- Fast moving / slow moving item report

- Inventory valuation report

# 10. Full System Architecture

The system should use layered architecture. This keeps the Razor Page UI
separate from business rules, database calls, and SQL logic.

FactoryIMS

│

├── Presentation Layer

│ └── Razor Pages

│

├── Application Layer

│ └── Services

│

├── Data Access Layer

│ └── Repositories / Stored Procedure Access

│

├── Domain Layer

│ └── Models, Entities, DTOs

│

└── Database Layer

└── SQL Server Tables, Views, Functions, Stored Procedures

| **Layer**         | **Responsibility**                                             |
|-------------------|----------------------------------------------------------------|
| Razor Pages       | Display UI, accept form input, call services.                  |
| Services          | Apply business rules and validation.                           |
| Repositories      | Call stored procedures, views, and database queries.           |
| SQL Server        | Store data and execute reusable database logic.                |
| Stored Procedures | Handle stock-changing operations safely.                       |
| SQL Functions     | Reusable calculations like available stock and shelf capacity. |
| Views             | Dashboard and report-ready summarized data.                    |

# 11. Suggested Razor Pages Structure

FactoryIMS

│

├── Pages

│ ├── Dashboard

│ │ ├── Admin.cshtml

│ │ ├── Manager.cshtml

│ │ └── Staff.cshtml

│ ├── Products

│ ├── Suppliers

│ ├── Customers

│ ├── Warehouses

│ │ ├── Index.cshtml

│ │ ├── Layout.cshtml

│ │ ├── Shelves.cshtml

│ │ └── Visualization.cshtml

│ ├── Inventory

│ │ ├── Overview.cshtml

│ │ ├── StockMovements.cshtml

│ │ ├── Adjustments.cshtml

│ │ ├── CycleCounts.cshtml

│ │ └── LowStock.cshtml

│ ├── Purchasing

│ ├── WarehouseOperations

│ ├── Sales

│ ├── Production

│ ├── Reports

│ ├── Users

│ └── Shared

├── Models

├── DTOs

├── Services

├── Repositories

├── Data

└── wwwroot

# 12. Database Design

| **Group**   | **Tables**                                                                                                         |
|-------------|--------------------------------------------------------------------------------------------------------------------|
| Security    | Users, Roles, UserRoles, Permissions, RolePermissions                                                              |
| Master Data | Products, Categories, Units, Suppliers, Customers                                                                  |
| Warehouse   | Warehouses, Floors, Rooms, Racks, Shelves                                                                          |
| Inventory   | InventoryStocks, StockMovements, StockReservations, StockBatches                                                   |
| Purchasing  | PurchaseRequests, PurchaseRequestItems, PurchaseOrders, PurchaseOrderItems, ReceivingItems, QCChecks, PutawayTasks |
| Sales       | SalesOrders, SalesOrderItems, PickTasks, Packing, Dispatches                                                       |
| Operations  | Transfers, TransferItems, Adjustments, CycleCounts                                                                 |
| Production  | BOM, BOMItems, ProductionPlans, ProductionMaterialIssues, FinishedGoodsReceipts                                    |
| System      | AuditLogs, Notifications, SystemSettings                                                                           |

# 13. Important SQL Table Examples

## 13.1 Products Table

CREATE TABLE Products (

ProductId INT IDENTITY(1,1) PRIMARY KEY,

SKU NVARCHAR(50) NOT NULL UNIQUE,

ProductName NVARCHAR(150) NOT NULL,

CategoryId INT NOT NULL,

UnitId INT NOT NULL,

ProductType NVARCHAR(30) NOT NULL,

Barcode NVARCHAR(100) NULL,

ReorderLevel DECIMAL(18,2) NOT NULL DEFAULT 0,

ReorderQuantity DECIMAL(18,2) NOT NULL DEFAULT 0,

StandardCost DECIMAL(18,2) NOT NULL DEFAULT 0,

SellingPrice DECIMAL(18,2) NOT NULL DEFAULT 0,

WeightKg DECIMAL(18,2) NULL,

IsActive BIT NOT NULL DEFAULT 1,

CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME()

);

## 13.2 InventoryStocks Table

CREATE TABLE InventoryStocks (

InventoryStockId INT IDENTITY(1,1) PRIMARY KEY,

ProductId INT NOT NULL,

WarehouseId INT NOT NULL,

ShelfId INT NULL,

CurrentQty DECIMAL(18,2) NOT NULL DEFAULT 0,

ReservedQty DECIMAL(18,2) NOT NULL DEFAULT 0,

QCHoldQty DECIMAL(18,2) NOT NULL DEFAULT 0,

DamagedQty DECIMAL(18,2) NOT NULL DEFAULT 0,

UpdatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),

CONSTRAINT UQ_InventoryStock UNIQUE(ProductId, WarehouseId, ShelfId)

);

## 13.3 StockMovements Table

CREATE TABLE StockMovements (

StockMovementId BIGINT IDENTITY(1,1) PRIMARY KEY,

ProductId INT NOT NULL,

WarehouseId INT NOT NULL,

ShelfId INT NULL,

MovementType NVARCHAR(50) NOT NULL,

Quantity DECIMAL(18,2) NOT NULL,

ReferenceType NVARCHAR(50) NULL,

ReferenceId INT NULL,

BeforeQty DECIMAL(18,2) NOT NULL,

AfterQty DECIMAL(18,2) NOT NULL,

PerformedBy INT NOT NULL,

MovementDate DATETIME2 NOT NULL DEFAULT SYSDATETIME(),

Remarks NVARCHAR(300) NULL

);

# 14. Reusable SQL Functions

## 14.1 Get Available Stock

CREATE FUNCTION dbo.fn_GetAvailableStock

(

@ProductId INT,

@WarehouseId INT

)

RETURNS DECIMAL(18,2)

AS

BEGIN

DECLARE @AvailableQty DECIMAL(18,2);

SELECT @AvailableQty =

ISNULL(SUM(CurrentQty - ReservedQty - QCHoldQty - DamagedQty), 0)

FROM InventoryStocks

WHERE ProductId = @ProductId

AND WarehouseId = @WarehouseId;

RETURN ISNULL(@AvailableQty, 0);

END;

## 14.2 Get Shelf Available Capacity

CREATE FUNCTION dbo.fn_GetShelfAvailableCapacity

(

@ShelfId INT

)

RETURNS DECIMAL(18,2)

AS

BEGIN

DECLARE @AvailableCapacity DECIMAL(18,2);

SELECT @AvailableCapacity = MaxQuantity - CurrentQuantity

FROM Shelves

WHERE ShelfId = @ShelfId;

RETURN ISNULL(@AvailableCapacity, 0);

END;

## 14.3 Get Stock Status

CREATE FUNCTION dbo.fn_GetStockStatus

(

@ProductId INT,

@WarehouseId INT

)

RETURNS NVARCHAR(30)

AS

BEGIN

DECLARE @AvailableQty DECIMAL(18,2);

DECLARE @ReorderLevel DECIMAL(18,2);

DECLARE @Status NVARCHAR(30);

SELECT @AvailableQty = dbo.fn_GetAvailableStock(@ProductId,
@WarehouseId);

SELECT @ReorderLevel = ReorderLevel

FROM Products

WHERE ProductId = @ProductId;

IF @AvailableQty \<= 0

SET @Status = 'Out of Stock';

ELSE IF @AvailableQty \<= @ReorderLevel

SET @Status = 'Low Stock';

ELSE

SET @Status = 'In Stock';

RETURN @Status;

END;

# 15. Reusable Stored Procedures

## 15.1 Record Stock Movement

CREATE PROCEDURE dbo.sp_RecordStockMovement

@ProductId INT,

@WarehouseId INT,

@ShelfId INT = NULL,

@MovementType NVARCHAR(50),

@Quantity DECIMAL(18,2),

@ReferenceType NVARCHAR(50) = NULL,

@ReferenceId INT = NULL,

@PerformedBy INT,

@Remarks NVARCHAR(300) = NULL

AS

BEGIN

SET NOCOUNT ON;

DECLARE @BeforeQty DECIMAL(18,2);

DECLARE @AfterQty DECIMAL(18,2);

SELECT @BeforeQty = ISNULL(CurrentQty, 0)

FROM InventoryStocks

WHERE ProductId = @ProductId

AND WarehouseId = @WarehouseId

AND ISNULL(ShelfId, 0) = ISNULL(@ShelfId, 0);

SET @BeforeQty = ISNULL(@BeforeQty, 0);

IF NOT EXISTS (

SELECT 1 FROM InventoryStocks

WHERE ProductId = @ProductId

AND WarehouseId = @WarehouseId

AND ISNULL(ShelfId, 0) = ISNULL(@ShelfId, 0)

)

BEGIN

INSERT INTO InventoryStocks(ProductId, WarehouseId, ShelfId, CurrentQty)

VALUES(@ProductId, @WarehouseId, @ShelfId, 0);

END;

IF @MovementType IN ('StockIn', 'TransferIn', 'AdjustmentIn',
'ReturnIn', 'ProductionReturn')

BEGIN

UPDATE InventoryStocks

SET CurrentQty = CurrentQty + @Quantity,

UpdatedAt = SYSDATETIME()

WHERE ProductId = @ProductId

AND WarehouseId = @WarehouseId

AND ISNULL(ShelfId, 0) = ISNULL(@ShelfId, 0);

END

ELSE IF @MovementType IN ('StockOut', 'TransferOut', 'AdjustmentOut',
'DamageOut', 'ProductionIssue')

BEGIN

UPDATE InventoryStocks

SET CurrentQty = CurrentQty - @Quantity,

UpdatedAt = SYSDATETIME()

WHERE ProductId = @ProductId

AND WarehouseId = @WarehouseId

AND ISNULL(ShelfId, 0) = ISNULL(@ShelfId, 0);

END;

SELECT @AfterQty = CurrentQty

FROM InventoryStocks

WHERE ProductId = @ProductId

AND WarehouseId = @WarehouseId

AND ISNULL(ShelfId, 0) = ISNULL(@ShelfId, 0);

INSERT INTO StockMovements

(ProductId, WarehouseId, ShelfId, MovementType, Quantity, ReferenceType,

ReferenceId, BeforeQty, AfterQty, PerformedBy, Remarks)

VALUES

(@ProductId, @WarehouseId, @ShelfId, @MovementType, @Quantity,
@ReferenceType,

@ReferenceId, @BeforeQty, @AfterQty, @PerformedBy, @Remarks);

END;

## 15.2 Confirm Putaway

CREATE PROCEDURE dbo.sp_ConfirmPutaway

@PutawayTaskId INT,

@ProductId INT,

@WarehouseId INT,

@ShelfId INT,

@Quantity DECIMAL(18,2),

@PerformedBy INT

AS

BEGIN

SET NOCOUNT ON;

IF dbo.fn_GetShelfAvailableCapacity(@ShelfId) \< @Quantity

BEGIN

RAISERROR('Shelf does not have enough capacity.', 16, 1);

RETURN;

END;

EXEC dbo.sp_RecordStockMovement

@ProductId = @ProductId,

@WarehouseId = @WarehouseId,

@ShelfId = @ShelfId,

@MovementType = 'StockIn',

@Quantity = @Quantity,

@ReferenceType = 'Putaway',

@ReferenceId = @PutawayTaskId,

@PerformedBy = @PerformedBy,

@Remarks = 'Stock added through putaway';

UPDATE PutawayTasks

SET Status = 'Completed',

CompletedBy = @PerformedBy,

CompletedAt = SYSDATETIME()

WHERE PutawayTaskId = @PutawayTaskId;

UPDATE Shelves

SET CurrentQuantity = CurrentQuantity + @Quantity

WHERE ShelfId = @ShelfId;

END;

## 15.3 Reserve Stock

CREATE PROCEDURE dbo.sp_ReserveStock

@SalesOrderItemId INT,

@ProductId INT,

@WarehouseId INT,

@RequiredQty DECIMAL(18,2)

AS

BEGIN

SET NOCOUNT ON;

DECLARE @AvailableQty DECIMAL(18,2);

SELECT @AvailableQty = dbo.fn_GetAvailableStock(@ProductId,
@WarehouseId);

IF @AvailableQty \< @RequiredQty

BEGIN

RAISERROR('Not enough available stock to reserve.', 16, 1);

RETURN;

END;

UPDATE InventoryStocks

SET ReservedQty = ReservedQty + @RequiredQty,

UpdatedAt = SYSDATETIME()

WHERE ProductId = @ProductId

AND WarehouseId = @WarehouseId;

INSERT INTO StockReservations

(SalesOrderItemId, ProductId, WarehouseId, ReservedQty, ReservedAt,
Status)

VALUES

(@SalesOrderItemId, @ProductId, @WarehouseId, @RequiredQty,
SYSDATETIME(), 'Reserved');

END;

## 15.4 Transfer Stock

CREATE PROCEDURE dbo.sp_TransferStock

@ProductId INT,

@FromWarehouseId INT,

@ToWarehouseId INT,

@FromShelfId INT,

@ToShelfId INT,

@Quantity DECIMAL(18,2),

@PerformedBy INT

AS

BEGIN

SET NOCOUNT ON;

BEGIN TRANSACTION;

BEGIN TRY

EXEC dbo.sp_RecordStockMovement @ProductId, @FromWarehouseId,
@FromShelfId,

'TransferOut', @Quantity, 'Transfer', NULL, @PerformedBy, 'Stock
transferred out';

EXEC dbo.sp_RecordStockMovement @ProductId, @ToWarehouseId, @ToShelfId,

'TransferIn', @Quantity, 'Transfer', NULL, @PerformedBy, 'Stock
transferred in';

COMMIT TRANSACTION;

END TRY

BEGIN CATCH

ROLLBACK TRANSACTION;

THROW;

END CATCH

END;

# 16. SQL Views for Reports

## 16.1 Inventory Overview View

CREATE VIEW vw_InventoryOverview AS

SELECT

p.ProductId,

p.SKU,

p.ProductName,

w.WarehouseName,

SUM(s.CurrentQty) AS CurrentQty,

SUM(s.ReservedQty) AS ReservedQty,

SUM(s.QCHoldQty) AS QCHoldQty,

SUM(s.DamagedQty) AS DamagedQty,

SUM(s.CurrentQty - s.ReservedQty - s.QCHoldQty - s.DamagedQty) AS
AvailableQty,

p.ReorderLevel

FROM InventoryStocks s

INNER JOIN Products p ON s.ProductId = p.ProductId

INNER JOIN Warehouses w ON s.WarehouseId = w.WarehouseId

GROUP BY p.ProductId, p.SKU, p.ProductName, w.WarehouseName,
p.ReorderLevel;

## 16.2 Low Stock View

CREATE VIEW vw_LowStockItems AS

SELECT ProductId, SKU, ProductName, WarehouseName, AvailableQty,
ReorderLevel

FROM vw_InventoryOverview

WHERE AvailableQty \<= ReorderLevel;

## 16.3 Shelf Utilization View

CREATE VIEW vw_ShelfUtilization AS

SELECT

s.ShelfId,

s.ShelfCode,

w.WarehouseName,

s.MaxQuantity,

s.CurrentQuantity,

CASE

WHEN s.MaxQuantity = 0 THEN 0

ELSE (s.CurrentQuantity / s.MaxQuantity) \* 100

END AS UtilizationPercent

FROM Shelves s

INNER JOIN Warehouses w ON s.WarehouseId = w.WarehouseId;

# 17. Interrelated Workflows

### Purchase to Stock

**Purchase Request** → **Purchase Order** → **Receiving** → **QC** →
**Putaway** → **Inventory**

### Sales to Dispatch

**Sales Order** → **Reserve** → **Pick** → **Pack** → **Dispatch** →
**StockOut**

### Production Material

**Production Plan** → **BOM** → **Material Issue** → **Production** →
**Finished Goods** → **Putaway**

### Adjustment

**Stock Difference** → **Adjustment Request** → **Approval** → **Stock
Update** → **Audit Log**

### Cycle Count

**Count Task** → **Physical Count** → **Variance** → **Adjustment
Request**

# 18. Business Rules

20. Stock should never become negative.

21. Reserved stock should not exceed current stock.

22. Staff should not approve their own adjustment request.

23. Usable stock should increase only after putaway, not immediately
    after receiving if QC is enabled.

24. Sales dispatch should reduce stock and consume reservation.

25. Every stock change must create a StockMovements record.

26. Low stock should be calculated from available stock, not only
    current stock.

27. Shelf capacity must be checked before putaway or transfer.

28. Damaged and QC hold stock should not be available for sale or
    production.

29. Important operations should be written to AuditLogs.

30. Role-based access must protect every Razor Page.

31. All repeated stock calculations should use SQL functions or service
    methods.

# 19. Development Roadmap

| **Order** | **Build First**             |
|-----------|-----------------------------|
| 1         | Login and role-based layout |
| 2         | Product master              |
| 3         | Warehouse and shelf setup   |
| 4         | Inventory overview          |
| 5         | Stock movement system       |
| 6         | Purchase order              |
| 7         | Receiving                   |
| 8         | QC check                    |
| 9         | Putaway                     |
| 10        | Sales order                 |
| 11        | Stock reservation           |
| 12        | Picking                     |
| 13        | Packing                     |
| 14        | Dispatch                    |
| 15        | Adjustment                  |
| 16        | Cycle count                 |
| 17        | Reports                     |
| 18        | Production material issue   |

# 20. Final Maintainability Guide

| **Part**          | **Rule**                                             |
|-------------------|------------------------------------------------------|
| Razor Pages       | Use only for UI, form binding, and calling services. |
| Services          | Keep business validation and process rules here.     |
| Repositories      | Use for stored procedure and view access.            |
| Stored Procedures | Use for stock-changing operations.                   |
| SQL Functions     | Use for reusable calculations.                       |
| SQL Views         | Use for reports and dashboards.                      |
| AuditLogs         | Track important actions for accountability.          |

# 21. References

Microsoft. (2026). Tutorial: Get started with Razor Pages in ASP.NET
Core. Microsoft Learn.
https://learn.microsoft.com/en-us/aspnet/core/data/ef-rp/intro

Microsoft. (2025). Reserve inventory quantities. Microsoft Learn.
https://learn.microsoft.com/en-us/dynamics365/supply-chain/inventory/reserve-inventory-quantities

Cerexio. (n.d.). Warehouse management system process flow.
https://cerexio.com/blog/warehouse-management-system-process-flow

Packem WMS. (n.d.). What is warehouse management software?
https://packemwms.com/what-is-a-warehouse-management-software/

Unleashed Software. (n.d.). Warehouse inventory management.
https://www.unleashedsoftware.com/blog/warehouse-inventory-management/

Netstock. (n.d.). Reorder point formula.
https://www.netstock.com/blog/reorder-point-formula/

Stockount. (n.d.). What is cycle count?
https://www.stockount.com/articles/what-is-cycle-count

Mukesh Murugan. (n.d.). Repository pattern in ASP.NET Core.
https://codewithmukesh.com/blog/repository-pattern-do-you-really-need-it/
