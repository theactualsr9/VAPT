-- SecureApiVAPT Stored Procedures
-- These procedures provide secure database access with parameterized queries

-- User Stored Procedures
CREATE PROCEDURE sp_GetUserById
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Username, Email, PasswordHash, Age, CreatedAt, LastLoginAt, IsActive, Roles
    FROM Users 
    WHERE Id = @UserId AND IsActive = 1;
END
GO

CREATE PROCEDURE sp_GetUserByUsername
    @Username NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Username, Email, PasswordHash, Age, CreatedAt, LastLoginAt, IsActive, Roles
    FROM Users 
    WHERE Username = @Username AND IsActive = 1;
END
GO

CREATE PROCEDURE sp_CreateUser
    @Username NVARCHAR(100),
    @Email NVARCHAR(255),
    @PasswordHash NVARCHAR(255),
    @Age INT,
    @Roles NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO Users (Username, Email, PasswordHash, Age, Roles, CreatedAt, IsActive)
    VALUES (@Username, @Email, @PasswordHash, @Age, @Roles, GETUTCDATE(), 1);
    
    SELECT SCOPE_IDENTITY() AS Id;
END
GO

CREATE PROCEDURE sp_UpdateUser
    @UserId INT,
    @Username NVARCHAR(100),
    @Email NVARCHAR(255),
    @Age INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Users 
    SET Username = @Username, Email = @Email, Age = @Age, UpdatedAt = GETUTCDATE()
    WHERE Id = @UserId AND IsActive = 1;
END
GO

CREATE PROCEDURE sp_DeleteUser
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Users 
    SET IsActive = 0, UpdatedAt = GETUTCDATE()
    WHERE Id = @UserId;
END
GO

-- Product Stored Procedures
CREATE PROCEDURE sp_GetProductById
    @ProductId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Name, Price, Description, StockQuantity, CreatedAt, UpdatedAt, IsActive
    FROM Products 
    WHERE Id = @ProductId AND IsActive = 1;
END
GO

CREATE PROCEDURE sp_GetAllProducts
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Name, Price, Description, StockQuantity, CreatedAt, UpdatedAt, IsActive
    FROM Products 
    WHERE IsActive = 1
    ORDER BY CreatedAt DESC;
END
GO

CREATE PROCEDURE sp_CreateProduct
    @Name NVARCHAR(200),
    @Price DECIMAL(18,2),
    @Description NVARCHAR(1000),
    @StockQuantity INT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO Products (Name, Price, Description, StockQuantity, CreatedAt, IsActive)
    VALUES (@Name, @Price, @Description, @StockQuantity, GETUTCDATE(), 1);
    
    SELECT SCOPE_IDENTITY() AS Id;
END
GO

CREATE PROCEDURE sp_UpdateProduct
    @ProductId INT,
    @Name NVARCHAR(200),
    @Price DECIMAL(18,2),
    @Description NVARCHAR(1000),
    @StockQuantity INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Products 
    SET Name = @Name, Price = @Price, Description = @Description, 
        StockQuantity = @StockQuantity, UpdatedAt = GETUTCDATE()
    WHERE Id = @ProductId AND IsActive = 1;
END
GO

CREATE PROCEDURE sp_DeleteProduct
    @ProductId INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Products 
    SET IsActive = 0, UpdatedAt = GETUTCDATE()
    WHERE Id = @ProductId;
END
GO

-- File Upload Stored Procedures
CREATE PROCEDURE sp_GetFileById
    @FileId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, FileName, OriginalFileName, ContentType, FilePath, FileSize, 
           UploadedBy, UploadedAt, IsActive, Description, Category, IsPublic, Checksum
    FROM FileUploads 
    WHERE Id = @FileId AND IsActive = 1;
END
GO

CREATE PROCEDURE sp_CreateFile
    @FileName NVARCHAR(255),
    @OriginalFileName NVARCHAR(255),
    @ContentType NVARCHAR(100),
    @FilePath NVARCHAR(500),
    @FileSize BIGINT,
    @UploadedBy NVARCHAR(100),
    @Description NVARCHAR(500),
    @Category NVARCHAR(50),
    @IsPublic BIT,
    @Checksum NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO FileUploads (FileName, OriginalFileName, ContentType, FilePath, FileSize, 
                           UploadedBy, Description, Category, IsPublic, Checksum, UploadedAt, IsActive)
    VALUES (@FileName, @OriginalFileName, @ContentType, @FilePath, @FileSize, 
            @UploadedBy, @Description, @Category, @IsPublic, @Checksum, GETUTCDATE(), 1);
    
    SELECT SCOPE_IDENTITY() AS Id;
END
GO

CREATE PROCEDURE sp_DeleteFile
    @FileId INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE FileUploads 
    SET IsActive = 0
    WHERE Id = @FileId;
END
GO

-- Audit Log Stored Procedure
CREATE PROCEDURE sp_CreateAuditLog
    @UserId NVARCHAR(50),
    @UserName NVARCHAR(100),
    @Action NVARCHAR(50),
    @EntityName NVARCHAR(100),
    @EntityId NVARCHAR(50),
    @OldValues NVARCHAR(MAX),
    @NewValues NVARCHAR(MAX),
    @IpAddress NVARCHAR(45),
    @UserAgent NVARCHAR(500),
    @AdditionalData NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO AuditLogs (UserId, UserName, Action, EntityName, EntityId, OldValues, 
                          NewValues, IpAddress, UserAgent, AdditionalData, Timestamp)
    VALUES (@UserId, @UserName, @Action, @EntityName, @EntityId, @OldValues, 
            @NewValues, @IpAddress, @UserAgent, @AdditionalData, GETUTCDATE());
END
GO

-- Search and Filter Procedures
CREATE PROCEDURE sp_SearchUsers
    @Username NVARCHAR(100) = NULL,
    @Email NVARCHAR(255) = NULL,
    @Page INT = 1,
    @PageSize INT = 20
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @Offset INT = (@Page - 1) * @PageSize;
    
    SELECT Id, Username, Email, Age, CreatedAt, LastLoginAt, IsActive, Roles
    FROM Users 
    WHERE IsActive = 1
      AND (@Username IS NULL OR Username LIKE '%' + @Username + '%')
      AND (@Email IS NULL OR Email LIKE '%' + @Email + '%')
    ORDER BY CreatedAt DESC
    OFFSET @Offset ROWS
    FETCH NEXT @PageSize ROWS ONLY;
    
    SELECT COUNT(*) AS TotalCount
    FROM Users 
    WHERE IsActive = 1
      AND (@Username IS NULL OR Username LIKE '%' + @Username + '%')
      AND (@Email IS NULL OR Email LIKE '%' + @Email + '%');
END
GO

CREATE PROCEDURE sp_SearchProducts
    @SearchTerm NVARCHAR(200) = NULL,
    @Category NVARCHAR(100) = NULL,
    @Page INT = 1,
    @PageSize INT = 20
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @Offset INT = (@Page - 1) * @PageSize;
    
    SELECT Id, Name, Price, Description, StockQuantity, CreatedAt, UpdatedAt, IsActive
    FROM Products 
    WHERE IsActive = 1
      AND (@SearchTerm IS NULL OR Name LIKE '%' + @SearchTerm + '%' OR Description LIKE '%' + @SearchTerm + '%')
      AND (@Category IS NULL OR Name LIKE '%' + @Category + '%')
    ORDER BY CreatedAt DESC
    OFFSET @Offset ROWS
    FETCH NEXT @PageSize ROWS ONLY;
    
    SELECT COUNT(*) AS TotalCount
    FROM Products 
    WHERE IsActive = 1
      AND (@SearchTerm IS NULL OR Name LIKE '%' + @SearchTerm + '%' OR Description LIKE '%' + @SearchTerm + '%')
      AND (@Category IS NULL OR Name LIKE '%' + @Category + '%');
END
GO

CREATE PROCEDURE sp_GetFilesByUser
    @UserId NVARCHAR(100),
    @IsPublic BIT = NULL,
    @Page INT = 1,
    @PageSize INT = 20
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @Offset INT = (@Page - 1) * @PageSize;
    
    SELECT Id, FileName, OriginalFileName, ContentType, FileSize, 
           UploadedBy, UploadedAt, Description, Category, IsPublic
    FROM FileUploads 
    WHERE IsActive = 1
      AND (UploadedBy = @UserId OR (@IsPublic IS NULL OR IsPublic = @IsPublic))
    ORDER BY UploadedAt DESC
    OFFSET @Offset ROWS
    FETCH NEXT @PageSize ROWS ONLY;
    
    SELECT COUNT(*) AS TotalCount
    FROM FileUploads 
    WHERE IsActive = 1
      AND (UploadedBy = @UserId OR (@IsPublic IS NULL OR IsPublic = @IsPublic));
END
GO 