using Dapper;
using Microsoft.Data.Sqlite;

namespace Imapster.Repositories;

public class AccountRepository : IAccountRepository
{
    private readonly string _dbPath;

    public AccountRepository()
    {
        _dbPath = Database.DbPath;
    }

    public async Task<List<ImapAccountViewModel>> GetAllAccountsAsync()
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        var accounts = await connection.QueryAsync<ImapAccountViewModel>(
            "SELECT Id, Name, Server, Port, UseSsl, Username, Password FROM Accounts");
        return accounts.ToList();
    }

    public async Task<ImapAccountViewModel> GetAccountByIdAsync(string id)
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        var account = await connection.QuerySingleOrDefaultAsync<ImapAccountViewModel>(
            "SELECT Id, Name, Server, Port, UseSsl, Username, Password FROM Accounts WHERE Id = @Id",
            new { Id = id });
        if (account != null)
            return account;

        throw new KeyNotFoundException($"Account with ID '{id}' not found.");
    }

    public async Task AddAccountAsync(ImapAccountViewModel account)
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        await connection.ExecuteAsync(
            "INSERT INTO Accounts (Id, Name, Server, Port, UseSsl, Username, Password) VALUES (@Id, @Name, @Server, @Port, @UseSsl, @Username, @Password)",
            account);
    }

    public async Task UpdateAccountAsync(ImapAccountViewModel account)
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        await connection.ExecuteAsync(
            "UPDATE Accounts SET Name = @Name, Server = @Server, Port = @Port, UseSsl = @UseSsl, Username = @Username, Password = @Password WHERE Id = @Id",
            account);
    }

    public async Task DeleteAccountAsync(string id)
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        await connection.ExecuteAsync(
            "DELETE FROM Accounts WHERE Id = @Id",
            new { Id = id });
    }
}