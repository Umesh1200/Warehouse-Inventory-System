-- ==========================================
-- 01_CreateTables.sql
-- Inventory Management System (IMS)
-- ==========================================

-- 1. Security Group
CREATE TABLE Roles (
    RoleID INT IDENTITY(1,1) PRIMARY KEY,
    RoleName NVARCHAR(50) NOT NULL UNIQUE,
    Description NVARCHAR(255),
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME DEFAULT GETDATE()
);

CREATE TABLE Users (
    UserID INT IDENTITY(1,1) PRIMARY KEY,
    Username NVARCHAR(50) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    FullName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(100),
    Phone NVARCHAR(20),
    RoleID INT FOREIGN KEY REFERENCES Roles(RoleID),
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME DEFAULT GETDATE(),
    UpdatedAt DATETIME DEFAULT GETDATE()
);

CREATE TABLE Permissions (
    PermissionID INT IDENTITY(1,1) PRIMARY KEY,
    PermissionName NVARCHAR(100) NOT NULL UNIQUE,
    Description NVARCHAR(255)
);

CREATE TABLE RolePermissions (
    RoleID INT FOREIGN KEY REFERENCES Roles(RoleID),
    PermissionID INT FOREIGN KEY REFERENCES Permissions(PermissionID),
    PRIMARY KEY (RoleID, PermissionID)
);

-- 2. Master Data Group
CREATE TABLE Categories (
    CategoryID INT IDENTITY(1,1) PRIMARY KEY,
    CategoryName NVARCHAR(100) NOT NULL UNIQUE,
    Description NVARCHAR(255),
    IsActive BIT DEFAULT 1
);

CREATE TABLE Units (
    UnitID INT IDENTITY(1,1) PRIMARY KEY,
    UnitName NVARCHAR(50) NOT NULL UNIQUE,
    UnitCode NVARCHAR(10) NOT NULL UNIQUE,
    IsActive BIT DEFAULT 1
);

CREATE TABLE Suppliers (
    SupplierID INT IDENTITY(1,1) PRIMARY KEY,
    SupplierCode NVARCHAR(50) NOT NULL UNIQUE,
    SupplierName NVARCHAR(150) NOT NULL,
    ContactPerson NVARCHAR(100),
    Phone NVARCHAR(50),
    Email NVARCHAR(100),
    Address NVARCHAR(255),
    SupplierType NVARCHAR(50),
    LeadTimeDays INT DEFAULT 0,
    PaymentTerms NVARCHAR(100),
    Status NVARCHAR(50) DEFAULT 'Active', -- Active, Inactive, Pending Review, Blacklisted
    CreatedAt DATETIME DEFAULT GETDATE(),
    UpdatedAt DATETIME DEFAULT GETDATE()
);

CREATE TABLE Customers (
    CustomerID INT IDENTITY(1,1) PRIMARY KEY,
    CustomerCode NVARCHAR(50) NOT NULL UNIQUE,
    CustomerName NVARCHAR(150) NOT NULL,
    ContactPerson NVARCHAR(100),
    Phone NVARCHAR(50),
    Email NVARCHAR(100),
    Address NVARCHAR(255),
    CustomerType NVARCHAR(50),
    Status NVARCHAR(50) DEFAULT 'Active', -- Active, Inactive, Pending Review, Blocked
    CreatedAt DATETIME DEFAULT GETDATE(),
    UpdatedAt DATETIME DEFAULT GETDATE()
);

CREATE TABLE Products (
    ProductID INT IDENTITY(1,1) PRIMARY KEY,
    SKU NVARCHAR(50) NOT NULL UNIQUE,
    ProductName NVARCHAR(150) NOT NULL,
    CategoryID INT FOREIGN KEY REFERENCES Categories(CategoryID),
    UnitID INT FOREIGN KEY REFERENCES Units(UnitID),
    ProductType NVARCHAR(50), -- Raw Material, Finished Goods, Consumable
    ReorderLevel DECIMAL(18,2) DEFAULT 0,
    StandardCost DECIMAL(18,2) DEFAULT 0,
    SellingPrice DECIMAL(18,2) DEFAULT 0,
    WeightKg DECIMAL(18,2) DEFAULT 0,
    Dimensions NVARCHAR(50),
    Status NVARCHAR(50) DEFAULT 'Active',
    CreatedAt DATETIME DEFAULT GETDATE(),
    UpdatedAt DATETIME DEFAULT GETDATE()
);

-- 3. Warehouse Group
CREATE TABLE Warehouses (
    WarehouseID INT IDENTITY(1,1) PRIMARY KEY,
    WarehouseCode NVARCHAR(50) NOT NULL UNIQUE,
    WarehouseName NVARCHAR(100) NOT NULL,
    Location NVARCHAR(255),
    IsActive BIT DEFAULT 1
);

CREATE TABLE Floors (
    FloorID INT IDENTITY(1,1) PRIMARY KEY,
    WarehouseID INT FOREIGN KEY REFERENCES Warehouses(WarehouseID),
    FloorCode NVARCHAR(50) NOT NULL,
    FloorName NVARCHAR(100),
    IsActive BIT DEFAULT 1
);

CREATE TABLE Rooms (
    RoomID INT IDENTITY(1,1) PRIMARY KEY,
    FloorID INT FOREIGN KEY REFERENCES Floors(FloorID),
    RoomCode NVARCHAR(50) NOT NULL,
    RoomName NVARCHAR(100),
    IsActive BIT DEFAULT 1
);

CREATE TABLE Racks (
    RackID INT IDENTITY(1,1) PRIMARY KEY,
    RoomID INT FOREIGN KEY REFERENCES Rooms(RoomID),
    RackCode NVARCHAR(50) NOT NULL,
    RackName NVARCHAR(100),
    IsActive BIT DEFAULT 1
);

CREATE TABLE Shelves (
    ShelfID INT IDENTITY(1,1) PRIMARY KEY,
    RackID INT FOREIGN KEY REFERENCES Racks(RackID),
    ShelfCode NVARCHAR(100) NOT NULL UNIQUE, -- e.g., KTM-F1-R02-A05-S1
    ShelfName NVARCHAR(100),
    ShelfType NVARCHAR(50) DEFAULT 'General', -- General, QC Hold, Raw Material, Finished Goods
    MaxQty DECIMAL(18,2) DEFAULT 0,
    MaxWeightKg DECIMAL(18,2) DEFAULT 0,
    IsActive BIT DEFAULT 1
);

-- 4. Inventory Group
CREATE TABLE InventoryStocks (
    StockID INT IDENTITY(1,1) PRIMARY KEY,
    ProductID INT FOREIGN KEY REFERENCES Products(ProductID),
    ShelfID INT FOREIGN KEY REFERENCES Shelves(ShelfID),
    CurrentQty DECIMAL(18,2) DEFAULT 0,
    ReservedQty DECIMAL(18,2) DEFAULT 0,
    QCHoldQty DECIMAL(18,2) DEFAULT 0,
    DamagedQty DECIMAL(18,2) DEFAULT 0,
    LastUpdated DATETIME DEFAULT GETDATE(),
    CONSTRAINT UQ_Product_Shelf UNIQUE (ProductID, ShelfID)
);

CREATE TABLE StockBatches (
    BatchID INT IDENTITY(1,1) PRIMARY KEY,
    BatchNumber NVARCHAR(100) NOT NULL UNIQUE,
    ProductID INT FOREIGN KEY REFERENCES Products(ProductID),
    MfgDate DATE,
    ExpDate DATE,
    OriginalQty DECIMAL(18,2) DEFAULT 0,
    RemainingQty DECIMAL(18,2) DEFAULT 0,
    Status NVARCHAR(50) DEFAULT 'Active',
    CreatedAt DATETIME DEFAULT GETDATE()
);

CREATE TABLE StockMovements (
    MovementID INT IDENTITY(1,1) PRIMARY KEY,
    ProductID INT FOREIGN KEY REFERENCES Products(ProductID),
    WarehouseID INT FOREIGN KEY REFERENCES Warehouses(WarehouseID),
    ShelfID INT FOREIGN KEY REFERENCES Shelves(ShelfID),
    MovementType NVARCHAR(50) NOT NULL, -- StockIn, StockOut, Reserve, Unreserve, TransferOut, TransferIn, AdjustmentIn, AdjustmentOut, ReturnIn, DamageOut, ProductionIssue, ProductionReturn
    Quantity DECIMAL(18,2) NOT NULL,
    BeforeQty DECIMAL(18,2) NOT NULL,
    AfterQty DECIMAL(18,2) NOT NULL,
    ReferenceType NVARCHAR(50), -- PO, SO, Transfer, Adjustment, Putaway, etc.
    ReferenceNo NVARCHAR(100),
    BatchID INT FOREIGN KEY REFERENCES StockBatches(BatchID),
    PerformedBy INT FOREIGN KEY REFERENCES Users(UserID),
    Remarks NVARCHAR(255),
    CreatedAt DATETIME DEFAULT GETDATE()
);

CREATE TABLE StockReservations (
    ReservationID INT IDENTITY(1,1) PRIMARY KEY,
    ProductID INT FOREIGN KEY REFERENCES Products(ProductID),
    ShelfID INT FOREIGN KEY REFERENCES Shelves(ShelfID),
    ReferenceType NVARCHAR(50) NOT NULL, -- SalesOrder
    ReferenceNo NVARCHAR(100) NOT NULL,
    ReservedQty DECIMAL(18,2) NOT NULL,
    Status NVARCHAR(50) DEFAULT 'Active', -- Active, Consumed, Cancelled
    CreatedBy INT FOREIGN KEY REFERENCES Users(UserID),
    CreatedAt DATETIME DEFAULT GETDATE()
);

-- 5. Purchasing Group
CREATE TABLE PurchaseRequests (
    PRID INT IDENTITY(1,1) PRIMARY KEY,
    PRNumber NVARCHAR(50) NOT NULL UNIQUE,
    RequestedBy INT FOREIGN KEY REFERENCES Users(UserID),
    ApprovedBy INT FOREIGN KEY REFERENCES Users(UserID),
    RequestDate DATETIME DEFAULT GETDATE(),
    RequiredDate DATE,
    Status NVARCHAR(50) DEFAULT 'Pending',
    Remarks NVARCHAR(255)
);

CREATE TABLE PurchaseRequestItems (
    PRItemID INT IDENTITY(1,1) PRIMARY KEY,
    PRID INT FOREIGN KEY REFERENCES PurchaseRequests(PRID),
    ProductID INT FOREIGN KEY REFERENCES Products(ProductID),
    Quantity DECIMAL(18,2) NOT NULL,
    EstimatedCost DECIMAL(18,2)
);

CREATE TABLE PurchaseOrders (
    POID INT IDENTITY(1,1) PRIMARY KEY,
    PONumber NVARCHAR(50) NOT NULL UNIQUE,
    SupplierID INT FOREIGN KEY REFERENCES Suppliers(SupplierID),
    OrderDate DATETIME DEFAULT GETDATE(),
    ExpectedDate DATE,
    TotalAmount DECIMAL(18,2) DEFAULT 0,
    Status NVARCHAR(50) DEFAULT 'Draft', -- Draft, Confirmed, InTransit, Received, QCCheck, Putaway, Closed, Cancelled
    CreatedBy INT FOREIGN KEY REFERENCES Users(UserID),
    ApprovedBy INT FOREIGN KEY REFERENCES Users(UserID),
    Remarks NVARCHAR(255)
);

CREATE TABLE PurchaseOrderItems (
    POItemID INT IDENTITY(1,1) PRIMARY KEY,
    POID INT FOREIGN KEY REFERENCES PurchaseOrders(POID),
    ProductID INT FOREIGN KEY REFERENCES Products(ProductID),
    OrderedQty DECIMAL(18,2) NOT NULL,
    ReceivedQty DECIMAL(18,2) DEFAULT 0,
    UnitPrice DECIMAL(18,2) NOT NULL,
    TotalPrice AS (OrderedQty * UnitPrice)
);

CREATE TABLE ReceivingItems (
    ReceiptID INT IDENTITY(1,1) PRIMARY KEY,
    POItemID INT FOREIGN KEY REFERENCES PurchaseOrderItems(POItemID),
    ReceivedQty DECIMAL(18,2) NOT NULL,
    DamagedQty DECIMAL(18,2) DEFAULT 0,
    RejectedQty DECIMAL(18,2) DEFAULT 0,
    BatchNumber NVARCHAR(100),
    ReceivedBy INT FOREIGN KEY REFERENCES Users(UserID),
    ReceiptDate DATETIME DEFAULT GETDATE(),
    Status NVARCHAR(50) DEFAULT 'QC Required' -- QC Required, Accepted, Rejected
);

CREATE TABLE QCChecks (
    QCID INT IDENTITY(1,1) PRIMARY KEY,
    ReceiptID INT NULL FOREIGN KEY REFERENCES ReceivingItems(ReceiptID),
    FGReceiptID INT NULL FOREIGN KEY REFERENCES FinishedGoodsReceipts(FGReceiptID),
    InspectorID INT FOREIGN KEY REFERENCES Users(UserID),
    InspectionDate DATETIME DEFAULT GETDATE(),
    AcceptedQty DECIMAL(18,2) NOT NULL,
    RejectedQty DECIMAL(18,2) DEFAULT 0,
    DamagedQty DECIMAL(18,2) DEFAULT 0,
    Remarks NVARCHAR(255),
    Status NVARCHAR(50) DEFAULT 'Completed'
);

CREATE TABLE PutawayTasks (
    TaskID INT IDENTITY(1,1) PRIMARY KEY,
    TaskNo NVARCHAR(50) NOT NULL UNIQUE,
    QCID INT FOREIGN KEY REFERENCES QCChecks(QCID),
    ProductID INT FOREIGN KEY REFERENCES Products(ProductID),
    QtyToPutaway DECIMAL(18,2) NOT NULL,
    SuggestedShelfID INT FOREIGN KEY REFERENCES Shelves(ShelfID),
    ActualShelfID INT FOREIGN KEY REFERENCES Shelves(ShelfID),
    AssignedTo INT FOREIGN KEY REFERENCES Users(UserID),
    Status NVARCHAR(50) DEFAULT 'Pending', -- Pending, In Progress, Completed
    CompletedDate DATETIME
);

-- 6. Sales Group
CREATE TABLE SalesOrders (
    SOID INT IDENTITY(1,1) PRIMARY KEY,
    SONumber NVARCHAR(50) NOT NULL UNIQUE,
    CustomerID INT FOREIGN KEY REFERENCES Customers(CustomerID),
    OrderDate DATETIME DEFAULT GETDATE(),
    RequiredDate DATE,
    TotalAmount DECIMAL(18,2) DEFAULT 0,
    Status NVARCHAR(50) DEFAULT 'Draft', -- Draft, Confirmed, Allocated, ReadyToPick, Picking, Packed, Dispatched, Fulfilled, Cancelled
    CreatedBy INT FOREIGN KEY REFERENCES Users(UserID),
    Remarks NVARCHAR(255)
);

CREATE TABLE SalesOrderItems (
    SOItemID INT IDENTITY(1,1) PRIMARY KEY,
    SOID INT FOREIGN KEY REFERENCES SalesOrders(SOID),
    ProductID INT FOREIGN KEY REFERENCES Products(ProductID),
    OrderedQty DECIMAL(18,2) NOT NULL,
    DispatchedQty DECIMAL(18,2) DEFAULT 0,
    UnitPrice DECIMAL(18,2) NOT NULL,
    TotalPrice AS (OrderedQty * UnitPrice)
);

CREATE TABLE PickTasks (
    PickID INT IDENTITY(1,1) PRIMARY KEY,
    SOID INT FOREIGN KEY REFERENCES SalesOrders(SOID),
    AssignedTo INT FOREIGN KEY REFERENCES Users(UserID),
    Status NVARCHAR(50) DEFAULT 'Pending', -- Pending, Picking, Picked
    CreatedAt DATETIME DEFAULT GETDATE(),
    CompletedDate DATETIME
);

CREATE TABLE Packing (
    PackID INT IDENTITY(1,1) PRIMARY KEY,
    SOID INT FOREIGN KEY REFERENCES SalesOrders(SOID),
    PackedBy INT FOREIGN KEY REFERENCES Users(UserID),
    ContainerType NVARCHAR(50),
    Status NVARCHAR(50) DEFAULT 'Packing', -- Packing, Packed
    CreatedAt DATETIME DEFAULT GETDATE()
);

CREATE TABLE Dispatches (
    DispatchID INT IDENTITY(1,1) PRIMARY KEY,
    SOID INT FOREIGN KEY REFERENCES SalesOrders(SOID),
    PackID INT FOREIGN KEY REFERENCES Packing(PackID),
    CourierName NVARCHAR(100),
    VehicleNumber NVARCHAR(50),
    TrackingNumber NVARCHAR(100),
    DispatchedBy INT FOREIGN KEY REFERENCES Users(UserID),
    DispatchDate DATETIME,
    Status NVARCHAR(50) DEFAULT 'Ready To Dispatch' -- Ready To Dispatch, Dispatched, InTransit, Delivered, Failed
);

-- 7. Operations Group
CREATE TABLE Transfers (
    TransferID INT IDENTITY(1,1) PRIMARY KEY,
    TransferNo NVARCHAR(50) NOT NULL UNIQUE,
    TransferType NVARCHAR(50), -- Inter-Shelf, Inter-Rack, Inter-Warehouse
    RequestedBy INT FOREIGN KEY REFERENCES Users(UserID),
    ApprovedBy INT FOREIGN KEY REFERENCES Users(UserID),
    Status NVARCHAR(50) DEFAULT 'Pending', -- Pending, Awaiting Approval, In Transit, Completed, Cancelled
    RequestDate DATETIME DEFAULT GETDATE(),
    CompletionDate DATETIME
);

CREATE TABLE TransferItems (
    TransferItemID INT IDENTITY(1,1) PRIMARY KEY,
    TransferID INT FOREIGN KEY REFERENCES Transfers(TransferID),
    ProductID INT FOREIGN KEY REFERENCES Products(ProductID),
    FromShelfID INT FOREIGN KEY REFERENCES Shelves(ShelfID),
    ToShelfID INT FOREIGN KEY REFERENCES Shelves(ShelfID),
    Quantity DECIMAL(18,2) NOT NULL
);

CREATE TABLE Adjustments (
    AdjustmentID INT IDENTITY(1,1) PRIMARY KEY,
    AdjustmentNo NVARCHAR(50) NOT NULL UNIQUE,
    AdjustmentType NVARCHAR(50) NOT NULL, -- AdjustmentIn, AdjustmentOut
    Reason NVARCHAR(100),
    RequestedBy INT FOREIGN KEY REFERENCES Users(UserID),
    ApprovedBy INT FOREIGN KEY REFERENCES Users(UserID),
    Status NVARCHAR(50) DEFAULT 'Pending', -- Pending, Awaiting Approval, Approved, Posted, Rejected
    RequestDate DATETIME DEFAULT GETDATE()
);

CREATE TABLE AdjustmentItems (
    AdjItemID INT IDENTITY(1,1) PRIMARY KEY,
    AdjustmentID INT FOREIGN KEY REFERENCES Adjustments(AdjustmentID),
    ProductID INT FOREIGN KEY REFERENCES Products(ProductID),
    ShelfID INT FOREIGN KEY REFERENCES Shelves(ShelfID),
    SystemQty DECIMAL(18,2) NOT NULL,
    CountedQty DECIMAL(18,2) NOT NULL,
    DifferenceQty AS (CountedQty - SystemQty)
);

CREATE TABLE CycleCounts (
    CountID INT IDENTITY(1,1) PRIMARY KEY,
    CountNo NVARCHAR(50) NOT NULL UNIQUE,
    CountType NVARCHAR(50),
    AssignedTo INT FOREIGN KEY REFERENCES Users(UserID),
    ScheduledDate DATE,
    Status NVARCHAR(50) DEFAULT 'Scheduled', -- Scheduled, In Progress, Completed, Variance Found
    CreatedAt DATETIME DEFAULT GETDATE()
);

-- 8. Production Group
CREATE TABLE BOM (
    BOMID INT IDENTITY(1,1) PRIMARY KEY,
    BOMVersion NVARCHAR(50) NOT NULL UNIQUE,
    ProductID INT FOREIGN KEY REFERENCES Products(ProductID), -- Finished Good
    EffectiveDate DATE,
    Status NVARCHAR(50) DEFAULT 'Draft', -- Draft, Active, Under Review, Archived
    CreatedBy INT FOREIGN KEY REFERENCES Users(UserID),
    CreatedAt DATETIME DEFAULT GETDATE()
);

CREATE TABLE BOMItems (
    BOMItemID INT IDENTITY(1,1) PRIMARY KEY,
    BOMID INT FOREIGN KEY REFERENCES BOM(BOMID),
    RawMaterialID INT FOREIGN KEY REFERENCES Products(ProductID),
    RequiredQty DECIMAL(18,2) NOT NULL,
    WastagePercent DECIMAL(5,2) DEFAULT 0
);

CREATE TABLE ProductionPlans (
    PlanID INT IDENTITY(1,1) PRIMARY KEY,
    PlanNumber NVARCHAR(50) NOT NULL UNIQUE,
    BOMID INT FOREIGN KEY REFERENCES BOM(BOMID),
    PlannedQty DECIMAL(18,2) NOT NULL,
    ProducedQty DECIMAL(18,2) DEFAULT 0,
    StartDate DATETIME,
    DueDate DATE,
    Priority NVARCHAR(50) DEFAULT 'Normal',
    Status NVARCHAR(50) DEFAULT 'Draft', -- Draft, Material Check, Approved, In Production, QC, Completed, Cancelled
    AssignedSupervisor INT FOREIGN KEY REFERENCES Users(UserID)
);

CREATE TABLE ProductionMaterialIssues (
    IssueID INT IDENTITY(1,1) PRIMARY KEY,
    PlanID INT FOREIGN KEY REFERENCES ProductionPlans(PlanID),
    ProductID INT FOREIGN KEY REFERENCES Products(ProductID),
    ShelfID INT FOREIGN KEY REFERENCES Shelves(ShelfID),
    RequestedQty DECIMAL(18,2) NOT NULL,
    IssuedQty DECIMAL(18,2) DEFAULT 0,
    ReturnedQty DECIMAL(18,2) DEFAULT 0,
    Status NVARCHAR(50) DEFAULT 'Pending',
    IssuedBy INT FOREIGN KEY REFERENCES Users(UserID),
    IssueDate DATETIME DEFAULT GETDATE()
);

CREATE TABLE FinishedGoodsReceipts (
    FGReceiptID INT IDENTITY(1,1) PRIMARY KEY,
    ReceiptNumber NVARCHAR(50) NOT NULL UNIQUE,
    PlanID INT FOREIGN KEY REFERENCES ProductionPlans(PlanID),
    ProductID INT FOREIGN KEY REFERENCES Products(ProductID),
    ProducedQty DECIMAL(18,2) NOT NULL,
    AcceptedQty DECIMAL(18,2) DEFAULT 0,
    RejectedQty DECIMAL(18,2) DEFAULT 0,
    BatchNumber NVARCHAR(100),
    ReceivedBy INT FOREIGN KEY REFERENCES Users(UserID),
    ReceiptDate DATETIME DEFAULT GETDATE(),
    Status NVARCHAR(50) DEFAULT 'Pending QC'
);

-- 9. System Group
CREATE TABLE AuditLogs (
    LogID INT IDENTITY(1,1) PRIMARY KEY,
    UserID INT FOREIGN KEY REFERENCES Users(UserID),
    Action NVARCHAR(100) NOT NULL,
    EntityName NVARCHAR(100),
    EntityID INT,
    OldValues NVARCHAR(MAX),
    NewValues NVARCHAR(MAX),
    IPAddress NVARCHAR(50),
    CreatedAt DATETIME DEFAULT GETDATE()
);

CREATE TABLE SystemSettings (
    SettingID INT IDENTITY(1,1) PRIMARY KEY,
    SettingKey NVARCHAR(100) NOT NULL UNIQUE,
    SettingValue NVARCHAR(MAX),
    Description NVARCHAR(255),
    UpdatedAt DATETIME DEFAULT GETDATE()
);
