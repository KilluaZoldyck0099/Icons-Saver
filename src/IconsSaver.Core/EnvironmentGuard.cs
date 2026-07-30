namespace IconsSaver.Core;

/// <summary>
/// Startup checks for conditions that would otherwise fail silently and corrupt saved layouts.
/// </summary>
public static class EnvironmentGuard
{
    /// <summary>
    /// Throws when the process cannot exchange structures with explorer correctly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Explorer is 64-bit on every supported version of Windows, and <c>LVITEMW</c> is 88 bytes
    /// there. A 32-bit build would hand it a 60 byte structure with 32-bit pointers, and comctl32
    /// would read the wrong fields.
    /// </para>
    /// <para>
    /// The reason this deserves a hard failure rather than a warning is the shape of the bug:
    /// positions keep working, because a <c>POINT</c> is 8 bytes either way, while
    /// <c>LVM_GETITEMTEXTW</c> quietly returns an empty name for every single icon. There is no
    /// error and no last-error value. The result is a layout file full of correct coordinates
    /// attached to blank names, which saves without complaint and restores nothing.
    /// </para>
    /// </remarks>
    /// <exception cref="PlatformNotSupportedException">The process is not 64-bit.</exception>
    public static void EnsureSupportedProcess()
    {
        if (IntPtr.Size != 8)
        {
            throw new PlatformNotSupportedException(
                $"{AppInfo.Name} must run as a 64-bit process. Explorer is 64-bit, and a 32-bit " +
                "build would read every icon name as empty while still reading positions correctly, " +
                "producing layouts that look fine and restore nothing.");
        }
    }
}
