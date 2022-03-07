CREATE TABLE [dbo].[UserPasswords]
(
    [Id] INT NOT NULL PRIMARY KEY,
	[User] INT NOT NULL, 
    [Hash] BIGINT NOT NULL, 
    [Pass] NVARCHAR(50) NULL
)
