-- ==========================================
-- 07_SupplierProducts_AVL.sql
-- Approved Vendor List / Supplier-Product Mapping
-- ==========================================

IF OBJECT_ID('dbo.SupplierProducts', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SupplierProducts (
        SupplierProductID INT IDENTITY(1,1) PRIMARY KEY,
        SupplierID INT NOT NULL FOREIGN KEY REFERENCES dbo.Suppliers(SupplierID),
        ProductID INT NOT NULL FOREIGN KEY REFERENCES dbo.Products(ProductID),
        SupplierSKU NVARCHAR(100),
        LastQuotedPrice DECIMAL(18,2),
        LeadTimeDays INT NOT NULL DEFAULT 0,
        MinOrderQty DECIMAL(18,2) NOT NULL DEFAULT 1,
        PackSize DECIMAL(18,2) NOT NULL DEFAULT 1,
        IsPreferred BIT NOT NULL DEFAULT 0,
        Status NVARCHAR(50) NOT NULL DEFAULT 'Approved',
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
        UpdatedAt DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT UQ_SupplierProducts_Supplier_Product UNIQUE (SupplierID, ProductID),
        CONSTRAINT CK_SupplierProducts_Status CHECK (Status IN ('Approved', 'Pending', 'Blocked', 'Inactive'))
    );
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_SupplierProducts_Product'
      AND object_id = OBJECT_ID('dbo.SupplierProducts')
)
BEGIN
    CREATE INDEX IX_SupplierProducts_Product ON dbo.SupplierProducts(ProductID, Status);
END
GO

-- Seed AVL from historical purchase order lines.
;WITH LatestSupplierProduct AS (
    SELECT
        po.SupplierID,
        poi.ProductID,
        poi.UnitPrice,
        s.LeadTimeDays,
        ROW_NUMBER() OVER (
            PARTITION BY po.SupplierID, poi.ProductID
            ORDER BY po.OrderDate DESC, poi.POItemID DESC
        ) AS rn
    FROM dbo.PurchaseOrderItems poi
    INNER JOIN dbo.PurchaseOrders po ON poi.POID = po.POID
    INNER JOIN dbo.Suppliers s ON po.SupplierID = s.SupplierID
    WHERE po.Status <> 'Cancelled'
)
INSERT INTO dbo.SupplierProducts (SupplierID, ProductID, SupplierSKU, LastQuotedPrice, LeadTimeDays, MinOrderQty, PackSize, IsPreferred, Status)
SELECT
    SupplierID,
    ProductID,
    NULL,
    UnitPrice,
    ISNULL(LeadTimeDays, 0),
    1,
    1,
    1,
    'Approved'
FROM LatestSupplierProduct src
WHERE src.rn = 1
  AND NOT EXISTS (
      SELECT 1
      FROM dbo.SupplierProducts sp
      WHERE sp.SupplierID = src.SupplierID
        AND sp.ProductID = src.ProductID
  );
GO

-- Demo-friendly fallback for fresh databases without PO history.
IF NOT EXISTS (SELECT 1 FROM dbo.SupplierProducts)
BEGIN
    INSERT INTO dbo.SupplierProducts (SupplierID, ProductID, SupplierSKU, LastQuotedPrice, LeadTimeDays, MinOrderQty, PackSize, IsPreferred, Status)
    SELECT
        s.SupplierID,
        p.ProductID,
        p.SKU,
        NULLIF(p.StandardCost, 0),
        ISNULL(s.LeadTimeDays, 0),
        1,
        1,
        CASE
            WHEN s.SupplierType = 'Manufacturer' AND p.ProductType IN ('Raw Material', 'Finished Goods') THEN 1
            WHEN s.SupplierType = 'Distributor' AND p.ProductType IN ('Consumable', 'Finished Goods') THEN 1
            ELSE 0
        END,
        'Approved'
    FROM dbo.Suppliers s
    CROSS JOIN dbo.Products p
    WHERE s.Status = 'Active'
      AND p.Status = 'Active'
      AND (
          (s.SupplierType = 'Manufacturer' AND p.ProductType IN ('Raw Material', 'Finished Goods'))
          OR (s.SupplierType = 'Distributor' AND p.ProductType IN ('Consumable', 'Finished Goods'))
          OR s.SupplierType IS NULL
      );
END
GO
