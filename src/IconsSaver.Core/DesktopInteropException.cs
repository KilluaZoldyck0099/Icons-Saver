using System.ComponentModel;

namespace IconsSaver.Core;

/// <summary>
/// Thrown when the desktop cannot be inspected or updated through the shell's list view.
/// </summary>
/// <remarks>
/// Every failure mode of the interop layer surfaces as this exception so callers never have to
/// deal with raw <c>GetLastError</c> values or bare <see cref="IntPtr"/> results.
/// </remarks>
public sealed class DesktopInteropException : Exception
{
    public DesktopInteropException(string message)
        : base(message)
    {
    }

    public DesktopInteropException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    private DesktopInteropException(string message, int nativeErrorCode, string describedMessage)
        : base(describedMessage)
    {
        NativeErrorCode = nativeErrorCode;
        BareMessage = message;
    }

    /// <summary>The <c>GetLastError</c> value captured at the failure site, or zero when unavailable.</summary>
    public int NativeErrorCode { get; }

    /// <summary>The message without the appended Win32 error description.</summary>
    public string? BareMessage { get; }

    /// <summary>
    /// Creates an exception that appends the human readable form of <paramref name="nativeErrorCode"/>.
    /// </summary>
    public static DesktopInteropException FromWin32(string message, int nativeErrorCode)
    {
        string described = nativeErrorCode == 0
            ? message
            : $"{message} (Win32 error {nativeErrorCode}: {new Win32Exception(nativeErrorCode).Message})";

        return new DesktopInteropException(message, nativeErrorCode, described);
    }

    /// <summary>
    /// Creates an exception describing the calling thread's current last-error value.
    /// </summary>
    public static DesktopInteropException FromLastWin32Error(string message) =>
        FromWin32(message, System.Runtime.InteropServices.Marshal.GetLastWin32Error());
}
