using System.Text;

namespace IconsSaver.Core.Storage;

/// <summary>
/// File writes that cannot leave a half-written document behind.
/// </summary>
/// <remarks>
/// A layout file is the only record of where several hundred icons belong. Writing it in place
/// means a power cut or a crash mid-write destroys it, so every write lands in a temporary file
/// first and is then moved over the target, which the file system does as a single operation.
/// </remarks>
public static class AtomicFile
{
    /// <summary>UTF-8 without a byte order mark, so the files stay diff-friendly.</summary>
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    /// <summary>Writes <paramref name="contents"/> to <paramref name="path"/> atomically.</summary>
    public static void WriteAllText(string path, string contents)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(contents);

        string directory = Path.GetDirectoryName(Path.GetFullPath(path))!;
        Directory.CreateDirectory(directory);

        string temporary = Path.Combine(directory, $".{Path.GetFileName(path)}.{Guid.NewGuid():n}.tmp");

        try
        {
            File.WriteAllText(temporary, contents, Utf8NoBom);
            File.Move(temporary, path, overwrite: true);
        }
        catch
        {
            TryDelete(temporary);
            throw;
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
            // A leftover temp file is untidy but harmless; never mask the original failure.
        }
        catch (UnauthorizedAccessException)
        {
            // Same reasoning.
        }
    }
}
