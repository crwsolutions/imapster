namespace Imapster.Repositories;

using Imapster.Models;

public interface IPromptRepository
{
    Task<PromptTemplate?> GetActivePromptAsync();
    Task<PromptTemplate?> GetPromptByNameAsync(string name);
    Task<PromptTemplate?> GetPromptByIdAsync(int id);
    Task<int> InsertPromptAsync(PromptTemplate prompt);
    Task<int> UpdatePromptAsync(PromptTemplate prompt);
    Task<int> DeletePromptAsync(int id);
    Task<List<PromptTemplate>> GetAllPromptsAsync();
    Task<PromptTemplate?> GetVerwijderRegelsAsync();
    Task<PromptTemplate?> GetBehoudenRegelsAsync();
    Task SaveRulesAsync(PromptTemplate verwijderRegels, PromptTemplate behoudenRegels);
}