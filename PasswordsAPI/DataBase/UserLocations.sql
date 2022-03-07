CREATE TABLE [dbo].[UserLocations]
(
	[Id] INT NOT NULL PRIMARY KEY, 
    [Area] NVARCHAR(50) NOT NULL, 
    [Info] NVARCHAR(50) NULL, 
    [User] INT NOT NULL, 
    [Name] NVARCHAR(50) NULL, 
    [Pass] BINARY(50) NOT NULL
)
