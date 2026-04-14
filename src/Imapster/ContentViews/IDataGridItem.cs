namespace Imapster.ContentViews;

public interface IDataGridItem
{
    /// <summary>
    /// Gets the value associated with the specified key.
    /// </summary>
    object? GetValue(string key);

    bool IsSelected { get; set; }

    void OnDoubleTapped();
}
