namespace Imapster.Extensions;

internal static class FormatExtensions
{
    internal static string FormatSize(this uint bytes)
    {
        const double KB = 1024;
        const double MB = KB * 1024;
        const double GB = MB * 1024;

        if (bytes < KB)
            return $"{bytes} B";

        if (bytes < MB)
            return $"{bytes / KB:0} KB";

        if (bytes < GB)
            return $"{bytes / MB:0.0} MB";

        return $"{bytes / GB:0.0} GB";
    }
}
