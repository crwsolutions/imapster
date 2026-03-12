namespace Imapster.Repositories;

using Dapper;
using Imapster.Models;
using Microsoft.Data.Sqlite;

public partial class PromptRepository : IPromptRepository
{
    private readonly string _dbPath;

    public PromptRepository()
    {
        _dbPath = Database.DbPath;
    }

    public async Task<PromptTemplate?> GetActivePromptAsync()
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        const string sql = "SELECT Id, Name, Prompt, CreatedAt, UpdatedAt FROM PromptTemplates WHERE IsActive = 1";
        var result = await connection.QueryFirstOrDefaultAsync<PromptTemplate>(sql);
        return result;
    }

    public async Task<PromptTemplate?> GetPromptByIdAsync(int id)
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        const string sql = "SELECT Id, Name, Prompt, CreatedAt, UpdatedAt FROM PromptTemplates WHERE Id = @Id";
        var result = await connection.QueryFirstOrDefaultAsync<PromptTemplate>(sql, new { Id = id });
        return result;
    }

    public async Task<int> InsertPromptAsync(PromptTemplate prompt)
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        const string sql = @"INSERT INTO PromptTemplates (Name, Prompt, CreatedAt, UpdatedAt, IsActive)
                            VALUES (@Name, @Prompt, @CreatedAt, @UpdatedAt, @IsActive);
                            SELECT Cast(last_insert_rowid() as integer)";
        var now = DateTime.UtcNow;
        prompt.CreatedAt = now;
        prompt.UpdatedAt = now;
        prompt.IsActive = false;

        var result = await connection.ExecuteScalarAsync<int>(sql, prompt);
        return result;
    }

    public async Task<int> UpdatePromptAsync(PromptTemplate prompt)
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        const string sql = @"UPDATE PromptTemplates 
                            SET Name = @Name, Prompt = @Prompt, UpdatedAt = @UpdatedAt
                            WHERE Id = @Id";
        prompt.UpdatedAt = DateTime.UtcNow;
        var rows = await connection.ExecuteAsync(sql, prompt);
        return rows;
    }

    public async Task<int> DeletePromptAsync(int id)
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        const string sql = "DELETE FROM PromptTemplates WHERE Id = @Id";
        return await connection.ExecuteAsync(sql, new { Id = id });
    }

    public async Task<List<PromptTemplate>> GetAllPromptsAsync()
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        const string sql = "SELECT Id, Name, Prompt, CreatedAt, UpdatedAt, IsActive FROM PromptTemplates ORDER BY UpdatedAt DESC";
        return (await connection.QueryAsync<PromptTemplate>(sql)).ToList();
    }

    public async Task<PromptTemplate?> GetPromptByNameAsync(string name)
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        const string sql = "SELECT Id, Name, Prompt, CreatedAt, UpdatedAt FROM PromptTemplates WHERE Name = @Name";
        var result = await connection.QueryFirstOrDefaultAsync<PromptTemplate>(sql, new { Name = name });
        return result;
    }

    public async Task<PromptTemplate?> GetVerwijderRegelsAsync()
    {
        return await GetPromptByNameAsync("VerwijderRegels");
    }

    public async Task<PromptTemplate?> GetBehoudenRegelsAsync()
    {
        return await GetPromptByNameAsync("BehoudenRegels");
    }

    public async Task SaveRulesAsync(PromptTemplate verwijderRegels, PromptTemplate behoudenRegels)
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        await connection.OpenAsync();

        using var transaction = connection.BeginTransaction();

        try
        {
            // Ensure both rules exist
            var existingVerwijder = await GetVerwijderRegelsAsync();
            if (existingVerwijder == null)
            {
                verwijderRegels.Name = "VerwijderRegels";
                await InsertPromptAsync(verwijderRegels);
            }
            else
            {
                verwijderRegels.Id = existingVerwijder.Id;
                await UpdatePromptAsync(verwijderRegels);
            }

            var existingBehouden = await GetBehoudenRegelsAsync();
            if (existingBehouden == null)
            {
                behoudenRegels.Name = "BehoudenRegels";
                await InsertPromptAsync(behoudenRegels);
            }
            else
            {
                behoudenRegels.Id = existingBehouden.Id;
                await UpdatePromptAsync(behoudenRegels);
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}