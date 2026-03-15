namespace Imapster.Repositories;

using Imapster.Models;

public interface IPromptRepository
{
    Task<PromptTemplate?> GetVerwijderRegelsAsync();
    Task<PromptTemplate?> GetBehoudenRegelsAsync();
    Task UpsertRulesAsync(string verwijderRegels, string behoudenRegels);
}