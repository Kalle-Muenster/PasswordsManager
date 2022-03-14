DROP TABLE IF EXISTS [PasswordUsers]
CREATE TABLE [dbo].[PasswordUsers] (
    [Id]   INT           IDENTITY (1, 1) NOT NULL,
    [Name] NVARCHAR (50) NULL,
    [Mail] NVARCHAR (50) NULL,
    [Info] NVARCHAR (50) NULL,
    [Icon] BINARY (50)   NULL,
    PRIMARY KEY CLUSTERED ([Id] ASC)
);