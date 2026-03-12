using Dapper;
using Microsoft.Data.Sqlite;

namespace Imapster.Repositories;

internal static class Database
{
    internal static string DbPath { get; private set; } = default!;

    internal static void Initialize()
    {
        DbPath = Path.Combine(FileSystem.AppDataDirectory, "imapster_data.db");
        Debug.WriteLine("SQLite: " + DbPath);
        //File.Delete(DbPath);
        if (!File.Exists(DbPath))
        {
            CreateDatabase();
        }
        else
        {
            MigrateDatabase();
        }
    }

    private static void CreateDatabase()
    {
        using var connection = new SqliteConnection($"Data Source={DbPath}");
        connection.Open();

        // Create Accounts table with auto-incrementing Id
        using var command1 = new SqliteCommand(
            """
            CREATE TABLE Accounts (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Name NVARCHAR(255) NOT NULL,
            Server NVARCHAR(255) NOT NULL,
            Port INTEGER NOT NULL,
            UseSsl BOOLEAN NOT NULL,
            Username NVARCHAR(255) NOT NULL,
            Password NVARCHAR(255) NOT NULL
            );
            """, connection);
        command1.ExecuteNonQuery();

        // Create Folders table with foreign key to Accounts
        using var command2 = new SqliteCommand(
            """
            CREATE TABLE Folders (
            AccountId INTEGER NOT NULL,
            Id NVARCHAR(255) NOT NULL,
            Name NVARCHAR(255) NOT NULL,
            UnreadCount INTEGER NOT NULL,
            IsTrash BOOLEAN NOT NULL,
            PRIMARY KEY (AccountId, Id),
            FOREIGN KEY (AccountId) REFERENCES Accounts(Id)
            );
            """, connection);
        command2.ExecuteNonQuery();

        // Create Emails table with composite primary key (AccountId, FolderId, Id) and uint Id
        using var command3 = new SqliteCommand(
            """
            CREATE TABLE Emails (
            AccountId INTEGER NOT NULL,
            FolderId NVARCHAR(255) NOT NULL,
            Id INTEGER NOT NULL,
            FromAddress NVARCHAR(255) NOT NULL,
            ToAddress NVARCHAR(255) NOT NULL,
            Date DATETIME NOT NULL,
            Subject NVARCHAR(255) NOT NULL,
            Body TEXT NOT NULL,
            IsRead BOOLEAN NOT NULL,
            HasAttachments BOOLEAN NOT NULL DEFAULT 0,
            Size INTEGER NULL,
            AiSummary TEXT NULL,
            AiCategory NVARCHAR(255) NULL,
            AiDelete BOOLEAN NULL,
            AiDeleteMotivation TEXT NULL,
            PRIMARY KEY (AccountId, FolderId, Id),
            FOREIGN KEY (AccountId, FolderId) REFERENCES Folders(AccountId, Id),
            FOREIGN KEY (AccountId) REFERENCES Accounts(Id)
            );
            """, connection);
        command3.ExecuteNonQuery();

        // Create PromptTemplates table
        using var command4 = new SqliteCommand(
            """
            CREATE TABLE PromptTemplates (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Name NVARCHAR(255) NOT NULL,
            Prompt TEXT NOT NULL,
            CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
            UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
            IsActive BOOLEAN NOT NULL DEFAULT 0
            );
            """, connection);
        command4.ExecuteNonQuery();
    }

    private static void MigrateDatabase()
    {
        using var connection = new SqliteConnection($"Data Source={DbPath}");
        connection.Open();

        // Check if PromptTemplates table exists
        var checkSql = "SELECT name FROM sqlite_master WHERE type='table' AND name='PromptTemplates'";
        var tableExists = connection.ExecuteScalar<string>(checkSql) != null;

        if (!tableExists)
        {
            var createSql = """
            CREATE TABLE PromptTemplates (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name NVARCHAR(255) NOT NULL,
                Prompt TEXT NOT NULL,
                CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                IsActive BOOLEAN NOT NULL DEFAULT 0
            );
            """;
            using var command = new SqliteCommand(createSql, connection);
            command.ExecuteNonQuery();
        }
    }
}