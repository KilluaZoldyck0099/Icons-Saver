using System.Text;
using IconsSaver.Core.Storage;

namespace IconsSaver.Core.Diagnostics;

/// <summary>
/// A deliberately small append-only log.
/// </summary>
/// <remarks>
/// Restores fail for reasons the user cannot see: explorer restarted, auto arrange was on, a
/// monitor reported a different DPI. A plain text log in the data folder is enough to answer
/// "why did my icons not come back" without dragging in a logging framework.
/// </remarks>
public static class AppLog
{
    private const long MaxBytes = 1024 * 1024;

    private static readonly object Gate = new();

    /// <summary>Turns logging off entirely, for tests and for users who would rather not have it.</summary>
    public static bool Enabled { get; set; } = true;

    public static void Info(string message) => Write("INFO ", message);

    public static void Warn(string message) => Write("WARN ", message);

    public static void Error(string message, Exception? exception = null) =>
        Write("ERROR", exception is null ? message : $"{message} :: {exception.GetType().Name}: {exception.Message}");

    private static void Write(string level, string message)
    {
        if (!Enabled)
        {
            return;
        }

        try
        {
            lock (Gate)
            {
                string path = AppPaths.LogFile;
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                RotateIfNeeded(path);

                string line = $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff} {level} {message}{Environment.NewLine}";
                File.AppendAllText(path, line, Encoding.UTF8);
            }
        }
        catch (IOException)
        {
            // Logging must never be the reason an operation fails.
        }
        catch (UnauthorizedAccessException)
        {
            // Same reasoning.
        }
    }

    private static void RotateIfNeeded(string path)
    {
        var file = new FileInfo(path);
        if (!file.Exists || file.Length < MaxBytes)
        {
            return;
        }

        string previous = path + ".1";
        if (File.Exists(previous))
        {
            File.Delete(previous);
        }

        File.Move(path, previous);
    }
}
