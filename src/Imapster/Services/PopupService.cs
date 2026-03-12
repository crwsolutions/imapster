using Imapster.ViewModels;
using Microsoft.Maui.Controls;
using CommunityToolkit.Maui;

namespace Imapster.Services;

public class PopupService : IPopupService
{
    public async Task<T?> ShowPopupAsync<TViewModel>(Page page, IDictionary<string, object> parameters)
        where TViewModel : class
    {
        var popup = Activator.CreateInstance<TViewModel>();
        
        if (popup is IQueryAttributable queryAttributable)
        {
            queryAttributable.ApplyQueryAttributes(parameters);
        }
        
        var result = await Shell.Current.ShowPopupAsync(popup, parameters);
        
        return result as T;
    }
}