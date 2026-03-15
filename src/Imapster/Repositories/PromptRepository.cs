namespace Imapster.Repositories;

using Dapper;
using Imapster.Models;
using Microsoft.Data.Sqlite;

public partial class PromptRepository : IPromptRepository
{
    private readonly string _dbPath;

    // Fixed IDs for prompt templates
    public const int VerwijderRegelsId = 1;
    public const int BehoudenRegelsId = 2;

    public PromptRepository()
    {
        _dbPath = Database.DbPath;
    }

    public async Task<PromptTemplate?> GetVerwijderRegelsAsync()
    {
        return await GetPromptByIdAsync(VerwijderRegelsId);
    }

    public async Task<PromptTemplate?> GetBehoudenRegelsAsync()
    {
        return await GetPromptByIdAsync(BehoudenRegelsId);
    }

    public async Task<PromptTemplate?> GetPromptByIdAsync(int id)
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        const string sql = "SELECT Id, Prompt FROM PromptTemplates WHERE Id = @Id";
        var result = await connection.QueryFirstOrDefaultAsync<PromptTemplate>(sql, new { Id = id });
        return result;
    }

    public async Task UpsertRulesAsync(string verwijderRegels, string behoudenRegels)
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        await connection.OpenAsync();

        const string upsertSql = "INSERT INTO PromptTemplates (Id, Prompt) VALUES (@Id, @Prompt) ON CONFLICT(Id) DO UPDATE SET Prompt = @Prompt";

        await connection.ExecuteAsync(upsertSql, new { Id = VerwijderRegelsId, Prompt = verwijderRegels });
        await connection.ExecuteAsync(upsertSql, new { Id = BehoudenRegelsId, Prompt = behoudenRegels });
    }
}