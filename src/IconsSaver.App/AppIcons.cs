namespace IconsSaver.App;

/// <summary>
/// Loads the application icon at the size the caller needs it.
/// </summary>
/// <remarks>
/// The icon is embedded rather than read back off the executable, so the tray gets a properly
/// authored small variant instead of a downscaled large one.
/// </remarks>
internal static class AppIcons
{
    private const string ResourceName = "IconsSaver.App.app.ico";

    /// <summary>The window and taskbar icon.</summary>
    public static Icon Application { get; } = Load(32);

    /// <summary>The notification area icon, at whatever size this system uses.</summary>
    public static Icon Tray { get; } = Load(SystemInformation.SmallIconSize.Width);

    private static Icon Load(int size)
    {
        try
        {
            using Stream? stream = typeof(AppIcons).Assembly.GetManifestResourceStream(ResourceName);
            if (stream is not null)
            {
                return new Icon(stream, new Size(size, size));
            }
        }
        catch (ArgumentException)
        {
            // A malformed resource is not worth crashing over; the stock icon will do.
        }

        return SystemIcons.Application;
    }
}
