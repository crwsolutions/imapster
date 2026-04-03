namespace Imapster.Repositories;

public interface IAccountRepository
{
    Task<List<ImapAccountViewModel>> GetAllAccountsAsync();
    Task<ImapAccountViewModel> GetAccountByIdAsync(string id);
    Task AddAccountAsync(ImapAccountViewModel account);
    Task UpdateAccountAsync(ImapAccountViewModel account);
    Task DeleteAccountAsync(string id);
}