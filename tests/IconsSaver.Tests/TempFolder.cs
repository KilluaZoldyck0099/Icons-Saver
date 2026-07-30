using IconsSaver.Core.Diagnostics;

namespace IconsSaver.Tests;

/// <summary>
/// A throwaway directory that cleans itself up.
/// </summary>
/// <remarks>
/// Storage tests write real files, because the bugs worth catching there are about encoding,
/// atomic replacement and malformed input, none of which survive being mocked away.
/// </remarks>
internal sealed class TempFolder : IDisposable
{
    static TempFolder()
    {
        // Keep the suite from scribbling in the real %APPDATA% log.
        AppLog.Enabled = false;
    }

    public TempFolder()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "IconsSaver.Tests", Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(Path);
    }

    /// <summary>Full path of the directory.</summary>
    public string Path { get; }

    /// <summary>Combines a file name onto the directory.</summary>
    public string File(string name) => System.IO.Path.Combine(Path, name);

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
        catch (IOException)
        {
            // A leftover temp directory is not a test failure.
        }
    }
}
