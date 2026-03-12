using Microsoft.Maui.Controls;
using CommunityToolkit.Maui.Views;

namespace Imapster.Services;

public interface IPopupService
{
    Task<T?> ShowPopupAsync<TPopup, TViewModel>(Page page, IDictionary<string, object> parameters)
        where TPopup : Popup<T>
        where TViewModel : class;
}