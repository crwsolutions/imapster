using System.Diagnostics;

namespace Imapster.Diagnostics;

/// <summary>
/// Appends timestamped lines to a load-trace log file (%AppData% imapster load-trace.log) and
/// mirrors them to the debug output, so the cost of opening an email or syncing a folder can be
/// followed end to end. Diagnostics only: it is a no-op outside DEBUG and must never break the app.
/// </summary>
public static class LoadTrace
{
    private static readonly object Gate = new();

    private static string? LogPath;

    public static void Log(string message)
    {
#if DEBUG
        Debug.WriteLine($"[LoadTrace] {message}");

        try
        {
            LogPath ??= Path.Combine(FileSystem.AppDataDirectory, "load-trace.log");
            lock (Gate)
            {
                File.AppendAllText(LogPath,
                    $"[{DateTime.Now:HH:mm:ss.fff}] [t{Environment.CurrentManagedThreadId,3}] {message}{Environment.NewLine}");
            }
        }
        catch
        {
            // A diagnostic must never take the app down.
        }
#endif
    }
}
