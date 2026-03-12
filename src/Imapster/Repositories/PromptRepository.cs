namespace Imapster.Repositories;

using System.Data;
using Dapper;
using Imapster.Models;

public class PromptRepository : IPromptRepository
{
    private readonly IDbConnection _db;

    public PromptRepository(IDbConnection db)
    {
        _db = db;
    }

    public async Task<PromptTemplate?> GetActivePromptAsync()
    {
        const string sql = "SELECT Id, Name, Prompt, CreatedAt, UpdatedAt FROM PromptTemplates WHERE IsActive = 1";
        var result = await _db.QueryFirstOrDefaultAsync<PromptTemplate>(sql);
        return result;
    }

    public async Task<PromptTemplate?> GetPromptByIdAsync(int id)
    {
        const string sql = "SELECT Id, Name, Prompt, CreatedAt, UpdatedAt FROM PromptTemplates WHERE Id = @Id";
        var result = await _db.QueryFirstOrDefaultAsync<PromptTemplate>(sql, new { Id = id });
        return result;
    }

    public async Task<int> InsertPromptAsync(PromptTemplate prompt)
    {
        const string sql = @"INSERT INTO PromptTemplates (Name, Prompt, CreatedAt, UpdatedAt, IsActive)
                            VALUES (@Name, @Prompt, @CreatedAt, @UpdatedAt, @IsActive);
                            SELECT Cast(last_insert_rowid() as integer)";
        var now = DateTime.UtcNow;
        prompt.CreatedAt = now;
        prompt.UpdatedAt = now;
        prompt.IsActive = false;

        var result = await _db.ExecuteScalarAsync<int>(sql, prompt);
        return result;
    }

    public async Task<int> UpdatePromptAsync(PromptTemplate prompt)
    {
        const string sql = @"UPDATE PromptTemplates 
                            SET Name = @Name, Prompt = @Prompt, UpdatedAt = @UpdatedAt
                            WHERE Id = @Id";
        prompt.UpdatedAt = DateTime.UtcNow;
        var rows = await _db.ExecuteAsync(sql, prompt);
        return rows;
    }

    public async Task<int> DeletePromptAsync(int id)
    {
        const string sql = "DELETE FROM PromptTemplates WHERE Id = @Id";
        return await _db.ExecuteAsync(sql, new { Id = id });
    }

    public async Task<List<PromptTemplate>> GetAllPromptsAsync()
    {
        const string sql = "SELECT Id, Name, Prompt, CreatedAt, UpdatedAt, IsActive FROM PromptTemplates ORDER BY UpdatedAt DESC";
        return (await _db.QueryAsync<PromptTemplate>(sql)).ToList();
    }
}