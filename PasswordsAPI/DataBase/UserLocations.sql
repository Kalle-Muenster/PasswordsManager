DROP TABLE IF EXISTS [UserLocations]
CREATE TABLE [dbo].[UserLocations] (
    [Id]   INT           IDENTITY (1, 1) NOT NULL,
    [Area] NVARCHAR (50) NOT NULL,
    [Info] NVARCHAR (50) NULL,
    [User] INT           NOT NULL,
    [Name] NVARCHAR (50) NULL,
    [Pass] BINARY (50)   NOT NULL,
    PRIMARY KEY CLUSTERED ([Id] ASC),
    FOREIGN KEY ([User]) REFERENCES [dbo].[PasswordUsers] ([Id]) ON DELETE CASCADE
);