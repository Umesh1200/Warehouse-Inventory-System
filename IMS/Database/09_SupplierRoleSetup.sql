-- ==========================================
-- 09_SupplierRoleSetup.sql
-- Introduce Supplier Role and Link column in Users table
-- ==========================================

-- 1. Alter Users table to add SupplierID link if it does not exist
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'SupplierID')
BEGIN
    ALTER TABLE Users ADD SupplierID INT NULL FOREIGN KEY REFERENCES Suppliers(SupplierID);
END
GO

-- 2. Seed 'Supplier' role if it doesn't exist
IF NOT EXISTS (SELECT 1 FROM Roles WHERE RoleName = 'Supplier')
BEGIN
    INSERT INTO Roles (RoleName, Description, IsActive, CreatedAt)
    VALUES ('Supplier', 'Supplier portal external user', 1, GETDATE());
END
GO

-- 3. Seed a test supplier user linked to Acme Global Manufacturing (SupplierID = 1)
IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'supplier_acme')
BEGIN
    DECLARE @SupplierRoleID INT;
    SELECT @SupplierRoleID = RoleID FROM Roles WHERE RoleName = 'Supplier';

    DECLARE @AcmeSupplierID INT;
    SELECT TOP 1 @AcmeSupplierID = SupplierID FROM Suppliers WHERE SupplierCode = 'SUP-001';

    IF @SupplierRoleID IS NOT NULL AND @AcmeSupplierID IS NOT NULL
    BEGIN
        INSERT INTO Users (Username, PasswordHash, FullName, Email, RoleID, SupplierID, IsActive, CreatedAt)
        VALUES ('supplier_acme', 'HASHED_PWD_HERE', 'Acme Supplier Representative', 'supplier@acmeglobal.com', @SupplierRoleID, @AcmeSupplierID, 1, GETDATE());
    END
END
GO
