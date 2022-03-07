CREATE TABLE [dbo].[PasswordUsers]
(
	[Id] INT NOT NULL PRIMARY KEY, 
    [Name] NVARCHAR(50) NULL, 
    [Mail] NVARCHAR(50) NULL, 
    [Info] NVARCHAR(50) NULL, 
    [Icon] BINARY(50) NULL
)
