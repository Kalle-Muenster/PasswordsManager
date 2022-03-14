DROP TABLE IF EXISTS [UserPasswords]
CREATE TABLE [dbo].[UserPasswords] (
    [Id]   INT           IDENTITY (1, 1) NOT NULL,
    [User] INT           NOT NULL,
    [Hash] BIGINT        NOT NULL,
    [Pass] NVARCHAR (50) NULL,
    PRIMARY KEY CLUSTERED ([Id] ASC),
    FOREIGN KEY ([User]) REFERENCES [dbo].[PasswordUsers] ([Id]) ON DELETE CASCADE
);

