namespace Imapster.Repositories;

using Imapster.Models;

public interface IPromptRepository
{
    Task<PromptTemplate?> GetActivePromptAsync();
    Task<PromptTemplate?> GetPromptByIdAsync(int id);
    Task<int> InsertPromptAsync(PromptTemplate prompt);
    Task<int> UpdatePromptAsync(PromptTemplate prompt);
    Task<int> DeletePromptAsync(int id);
    Task<List<PromptTemplate>> GetAllPromptsAsync();
}