namespace Imapster.Extensions;

public static class FolderViewModelExtensions
{
    public static ObservableCollection<FolderViewModel> BuildHierarchy(this List<FolderViewModel> flatFolders)
    {
        var result = new ObservableCollection<FolderViewModel>();
        var folderLookup = new Dictionary<string, FolderViewModel>();

        // First pass: create all folder view models
        foreach (var folder in flatFolders)
        {
            var folderVm = new FolderViewModel
            {
                Id = folder.Id,
                Name = folder.Name,
                AccountId = folder.AccountId,
                IsTrash = folder.IsTrash,
                IndentLevel = 0,
                HasChildren = false
            };
            folderLookup[folder.Id] = folderVm;
            result.Add(folderVm);
        }

        // Second pass: set IndentLevel and HasChildren based on folder name structure
        foreach (var kvp in folderLookup)
        {
            var folderVm = kvp.Value;
            var folderId = folderVm.Id;

            // Find parent by checking if any other folder ID is a parent path
            var parts = folderId.Split('.');
            if (parts.Length > 1)
            {
                // Try to find parent by progressively shorter paths
                for (int i = parts.Length - 1; i > 0; i--)
                {
                    var parentId = string.Join(".", parts.Take(i));
                    if (folderLookup.ContainsKey(parentId))
                    {
                        var parent = folderLookup[parentId];
                        parent.HasChildren = true;
                        folderVm.IndentLevel = parent.IndentLevel + 1;
                        break;
                    }
                }
            }
        }

        return result;
    }
}
