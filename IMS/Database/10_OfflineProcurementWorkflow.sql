-- ================================================================
-- 10_OfflineProcurementWorkflow.sql
-- Staff-managed supplier communication, acknowledgement and AP flow
-- Safe to run more than once against an existing FactoryIMS database.
-- ================================================================
USE FactoryIMS;
GO

IF OBJECT_ID('dbo.PurchaseOrderCommunications', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PurchaseOrderCommunications (
        CommunicationID INT IDENTITY(1,1) PRIMARY KEY,
        POID INT NOT NULL FOREIGN KEY REFERENCES dbo.PurchaseOrders(POID),
        Channel NVARCHAR(30) NOT NULL, -- Email, Phone, Messaging, Print, Other
        Direction NVARCHAR(20) NOT NULL DEFAULT 'Outbound',
        ContactDetail NVARCHAR(150) NULL,
        CommunicatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
        Notes NVARCHAR(1000) NULL,
        RecordedBy INT NOT NULL FOREIGN KEY REFERENCES dbo.Users(UserID),
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME()
    );
    CREATE INDEX IX_POCommunications_POID ON dbo.PurchaseOrderCommunications(POID, CommunicatedAt DESC);
END
GO

IF OBJECT_ID('dbo.SupplierConfirmations', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SupplierConfirmations (
        ConfirmationID INT IDENTITY(1,1) PRIMARY KEY,
        POID INT NOT NULL FOREIGN KEY REFERENCES dbo.PurchaseOrders(POID),
        ConfirmationStatus NVARCHAR(30) NOT NULL, -- Pending, Confirmed, Declined, Changes Requested
        SupplierContactName NVARCHAR(150) NULL,
        ConfirmedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
        PromisedDeliveryDate DATE NULL,
        Notes NVARCHAR(1000) NULL,
        RecordedBy INT NOT NULL FOREIGN KEY REFERENCES dbo.Users(UserID),
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME()
    );
    CREATE INDEX IX_SupplierConfirmations_POID ON dbo.SupplierConfirmations(POID, ConfirmedAt DESC);
END
GO

IF OBJECT_ID('dbo.ProcurementDocuments', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProcurementDocuments (
        DocumentID INT IDENTITY(1,1) PRIMARY KEY,
        POID INT NOT NULL FOREIGN KEY REFERENCES dbo.PurchaseOrders(POID),
        ReceiptID INT NULL FOREIGN KEY REFERENCES dbo.ReceivingItems(ReceiptID),
        DocumentType NVARCHAR(40) NOT NULL, -- Supplier Confirmation, Delivery Challan, Packing List, Invoice, Other
        OriginalFileName NVARCHAR(260) NOT NULL,
        StoredFileName NVARCHAR(260) NOT NULL,
        ContentType NVARCHAR(100) NULL,
        FileSize BIGINT NOT NULL,
        Notes NVARCHAR(500) NULL,
        UploadedBy INT NOT NULL FOREIGN KEY REFERENCES dbo.Users(UserID),
        UploadedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME()
    );
    CREATE INDEX IX_ProcurementDocuments_POID ON dbo.ProcurementDocuments(POID, UploadedAt DESC);
END
GO

IF OBJECT_ID('dbo.SupplierInvoices', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SupplierInvoices (
        InvoiceID INT IDENTITY(1,1) PRIMARY KEY,
        POID INT NOT NULL FOREIGN KEY REFERENCES dbo.PurchaseOrders(POID),
        InvoiceNumber NVARCHAR(100) NOT NULL,
        InvoiceDate DATE NOT NULL,
        DueDate DATE NULL,
        Subtotal DECIMAL(18,2) NOT NULL,
        TaxAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
        TotalAmount DECIMAL(18,2) NOT NULL,
        Status NVARCHAR(30) NOT NULL DEFAULT 'Pending Review', -- Pending Review, Approved, On Hold, Partially Paid, Paid, Rejected
        Notes NVARCHAR(1000) NULL,
        RecordedBy INT NOT NULL FOREIGN KEY REFERENCES dbo.Users(UserID),
        ApprovedBy INT NULL FOREIGN KEY REFERENCES dbo.Users(UserID),
        ApprovedAt DATETIME2 NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
        CONSTRAINT UQ_SupplierInvoices_PO_InvoiceNo UNIQUE (POID, InvoiceNumber)
    );
    CREATE INDEX IX_SupplierInvoices_Status ON dbo.SupplierInvoices(Status, DueDate);
END
GO

IF OBJECT_ID('dbo.SupplierPayments', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SupplierPayments (
        PaymentID INT IDENTITY(1,1) PRIMARY KEY,
        InvoiceID INT NOT NULL FOREIGN KEY REFERENCES dbo.SupplierInvoices(InvoiceID),
        PaymentDate DATE NOT NULL,
        Amount DECIMAL(18,2) NOT NULL,
        PaymentMethod NVARCHAR(40) NOT NULL,
        ReferenceNumber NVARCHAR(100) NULL,
        Notes NVARCHAR(500) NULL,
        RecordedBy INT NOT NULL FOREIGN KEY REFERENCES dbo.Users(UserID),
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME()
    );
    CREATE INDEX IX_SupplierPayments_InvoiceID ON dbo.SupplierPayments(InvoiceID, PaymentDate DESC);
END
GO

PRINT 'Offline procurement workflow tables are ready.';
GO
