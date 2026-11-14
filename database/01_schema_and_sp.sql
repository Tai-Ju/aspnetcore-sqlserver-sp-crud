/* =========================================================
   ProductApi - SQL Server schema + CRUD stored procedure
   在 SSMS 或 Visual Studio「SQL Server 物件總管」執行此檔即可
   ========================================================= */

IF DB_ID(N'ProductDb') IS NULL
    CREATE DATABASE ProductDb;
GO

USE ProductDb;
GO

/* ---------- 1. 資料表 ---------- */
IF OBJECT_ID(N'dbo.Products', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Products
    (
        Id         INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Products PRIMARY KEY,
        Name       NVARCHAR(100)     NOT NULL,
        Price      DECIMAL(10,2)     NOT NULL CONSTRAINT DF_Products_Price DEFAULT (0),
        Stock      INT               NOT NULL CONSTRAINT DF_Products_Stock DEFAULT (0),
        CreatedAt  DATETIME2(0)      NOT NULL CONSTRAINT DF_Products_CreatedAt DEFAULT (SYSDATETIME()),
        UpdatedAt  DATETIME2(0)      NULL,
        CONSTRAINT CK_Products_Price CHECK (Price >= 0),
        CONSTRAINT CK_Products_Stock CHECK (Stock >= 0)
    );
END
GO

/* ---------- 2. 一支 SP 處理 CRUD ----------
   @Action:
     'LIST'   - 查全部
     'GET'    - 依 Id 查一筆
     'CREATE' - 新增，回傳新增後的那筆
     'UPDATE' - 修改，回傳修改後的那筆（找不到則無資料列）
     'DELETE' - 刪除，回傳 AffectedRows
------------------------------------------- */
CREATE OR ALTER PROCEDURE dbo.usp_Products_CRUD
    @Action NVARCHAR(10),
    @Id     INT           = NULL,
    @Name   NVARCHAR(100) = NULL,
    @Price  DECIMAL(10,2) = NULL,
    @Stock  INT           = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @Action = N'LIST'
    BEGIN
        SELECT Id, Name, Price, Stock, CreatedAt, UpdatedAt
        FROM dbo.Products
        ORDER BY Id;
        RETURN;
    END

    IF @Action = N'GET'
    BEGIN
        SELECT Id, Name, Price, Stock, CreatedAt, UpdatedAt
        FROM dbo.Products
        WHERE Id = @Id;
        RETURN;
    END

    IF @Action = N'CREATE'
    BEGIN
        INSERT INTO dbo.Products (Name, Price, Stock)
        OUTPUT inserted.Id, inserted.Name, inserted.Price, inserted.Stock,
               inserted.CreatedAt, inserted.UpdatedAt
        VALUES (@Name, ISNULL(@Price, 0), ISNULL(@Stock, 0));
        RETURN;
    END

    IF @Action = N'UPDATE'
    BEGIN
        UPDATE dbo.Products
        SET Name      = @Name,
            Price     = @Price,
            Stock     = @Stock,
            UpdatedAt = SYSDATETIME()
        OUTPUT inserted.Id, inserted.Name, inserted.Price, inserted.Stock,
               inserted.CreatedAt, inserted.UpdatedAt
        WHERE Id = @Id;
        RETURN;
    END

    IF @Action = N'DELETE'
    BEGIN
        DELETE FROM dbo.Products WHERE Id = @Id;
        SELECT @@ROWCOUNT AS AffectedRows;
        RETURN;
    END;  -- THROW 的前一句必須以分號結尾

    THROW 50001, N'Unknown @Action. Use LIST / GET / CREATE / UPDATE / DELETE.', 1;
END
GO

/* ---------- 3. 範例資料（可略） ---------- */
IF NOT EXISTS (SELECT 1 FROM dbo.Products)
BEGIN
    EXEC dbo.usp_Products_CRUD @Action = N'CREATE', @Name = N'口罩 (50入)',   @Price = 199,  @Stock = 120;
    EXEC dbo.usp_Products_CRUD @Action = N'CREATE', @Name = N'酒精乾洗手',    @Price = 89.5, @Stock = 300;
    EXEC dbo.usp_Products_CRUD @Action = N'CREATE', @Name = N'電子體溫計',    @Price = 450,  @Stock = 25;
END
GO
