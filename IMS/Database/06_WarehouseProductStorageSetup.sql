SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

USE FactoryIMS;
GO

/*
    Warehouse/product storage setup
    --------------------------------
    Adds explicit product-to-storage rules and improves seed product weights so
    putaway and transfer capacity checks can use both quantity and weight.
    This script is idempotent and safe to rerun.
*/

IF OBJECT_ID('dbo.ProductStorageRules', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProductStorageRules
    (
        RuleID INT IDENTITY(1,1) PRIMARY KEY,
        ProductID INT NOT NULL,
        AllowedShelfType NVARCHAR(50) NOT NULL,
        MaxStackQty DECIMAL(18,2) NULL,
        RequiresQCHold BIT NOT NULL DEFAULT 0,
        IsFragile BIT NOT NULL DEFAULT 0,
        IsHazardous BIT NOT NULL DEFAULT 0,
        PreferredWarehouseID INT NULL,
        PreferredRackID INT NULL,
        PreferredShelfID INT NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_ProductStorageRules_Products FOREIGN KEY (ProductID) REFERENCES dbo.Products(ProductID),
        CONSTRAINT FK_ProductStorageRules_Warehouses FOREIGN KEY (PreferredWarehouseID) REFERENCES dbo.Warehouses(WarehouseID),
        CONSTRAINT FK_ProductStorageRules_Racks FOREIGN KEY (PreferredRackID) REFERENCES dbo.Racks(RackID),
        CONSTRAINT FK_ProductStorageRules_Shelves FOREIGN KEY (PreferredShelfID) REFERENCES dbo.Shelves(ShelfID)
    );
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'UX_ProductStorageRules_Product_ShelfType'
      AND object_id = OBJECT_ID('dbo.ProductStorageRules')
)
BEGIN
    CREATE UNIQUE INDEX UX_ProductStorageRules_Product_ShelfType
    ON dbo.ProductStorageRules(ProductID, AllowedShelfType)
    WHERE IsActive = 1;
END
GO

UPDATE dbo.Products
SET WeightKg = CASE SKU
    WHEN 'RM-COP-005' THEN 0.02
    WHEN 'PRD-SRV-AX200' THEN 7.50
    WHEN 'PKG-BOX-MED' THEN 0.15
    ELSE WeightKg
END,
Dimensions = CASE SKU
    WHEN 'RM-COP-005' THEN 'Coil/raw material spool'
    WHEN 'PRD-SRV-AX200' THEN 'Finished goods carton'
    WHEN 'PKG-BOX-MED' THEN 'Medium packaging carton'
    ELSE Dimensions
END
WHERE SKU IN ('RM-COP-005', 'PRD-SRV-AX200', 'PKG-BOX-MED');
GO

UPDATE dbo.Shelves
SET MaxWeightKg = CASE ShelfCode
    WHEN 'KTM-F1-R02-B01-S1' THEN 500.00
    ELSE MaxWeightKg
END
WHERE ShelfCode IN ('KTM-F1-R02-B01-S1');
GO

MERGE dbo.ProductStorageRules AS target
USING (
    SELECT ProductID, 'Raw Material' AS AllowedShelfType, CAST(2000 AS DECIMAL(18,2)) AS MaxStackQty, CAST(0 AS BIT) AS RequiresQCHold, CAST(0 AS BIT) AS IsFragile, CAST(0 AS BIT) AS IsHazardous
    FROM dbo.Products WHERE SKU = 'RM-COP-005'
    UNION ALL
    SELECT ProductID, 'Finished Goods', CAST(100 AS DECIMAL(18,2)), CAST(0 AS BIT), CAST(1 AS BIT), CAST(0 AS BIT)
    FROM dbo.Products WHERE SKU = 'PRD-SRV-AX200'
    UNION ALL
    SELECT ProductID, 'General', CAST(1000 AS DECIMAL(18,2)), CAST(0 AS BIT), CAST(0 AS BIT), CAST(0 AS BIT)
    FROM dbo.Products WHERE SKU = 'PKG-BOX-MED'
) AS source
ON target.ProductID = source.ProductID
AND target.AllowedShelfType = source.AllowedShelfType
WHEN MATCHED THEN
    UPDATE SET
        MaxStackQty = source.MaxStackQty,
        RequiresQCHold = source.RequiresQCHold,
        IsFragile = source.IsFragile,
        IsHazardous = source.IsHazardous,
        IsActive = 1
WHEN NOT MATCHED THEN
    INSERT (ProductID, AllowedShelfType, MaxStackQty, RequiresQCHold, IsFragile, IsHazardous)
    VALUES (source.ProductID, source.AllowedShelfType, source.MaxStackQty, source.RequiresQCHold, source.IsFragile, source.IsHazardous);
GO

IF OBJECT_ID('dbo.fn_GetShelfAvailableWeightCapacity', 'FN') IS NULL
    EXEC('CREATE FUNCTION dbo.fn_GetShelfAvailableWeightCapacity(@ShelfID INT) RETURNS DECIMAL(18,2) AS BEGIN RETURN 0 END');
GO

ALTER FUNCTION dbo.fn_GetShelfAvailableWeightCapacity(@ShelfID INT)
RETURNS DECIMAL(18,2)
AS
BEGIN
    DECLARE @MaxWeight DECIMAL(18,2);
    DECLARE @UsedWeight DECIMAL(18,2);

    SELECT @MaxWeight = ISNULL(MaxWeightKg, 0)
    FROM dbo.Shelves
    WHERE ShelfID = @ShelfID;

    SELECT @UsedWeight = ISNULL(SUM(ISNULL(i.CurrentQty, 0) * ISNULL(p.WeightKg, 0)), 0)
    FROM dbo.InventoryStocks i
    INNER JOIN dbo.Products p ON i.ProductID = p.ProductID
    WHERE i.ShelfID = @ShelfID;

    RETURN CASE
        WHEN ISNULL(@MaxWeight, 0) - ISNULL(@UsedWeight, 0) < 0 THEN 0
        ELSE ISNULL(@MaxWeight, 0) - ISNULL(@UsedWeight, 0)
    END;
END;
GO
