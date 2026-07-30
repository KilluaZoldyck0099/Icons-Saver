using System.Text.Json.Serialization;

namespace IconsSaver.Core.Models;

/// <summary>
/// A saved desktop layout: where every icon was, and on which display setup.
/// </summary>
/// <remarks>
/// This is the on-disk document. Every field is written even when empty so a file is readable on
/// its own, and <see cref="SchemaVersion"/> is checked on load so a future format change can be
/// migrated rather than silently misread.
/// </remarks>
public sealed class LayoutSnapshot
{
    /// <summary>The format version this build writes.</summary>
    public const int CurrentSchemaVersion = 1;

    /// <summary>Version of the on-disk format this document was written with.</summary>
    public int SchemaVersion { get; init; } = CurrentSchemaVersion;

    /// <summary>Stable identifier, also used as the file name.</summary>
    public string Id { get; init; } = Guid.NewGuid().ToString("n");

    /// <summary>The name the user gave this layout.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Free-form note the user can attach.</summary>
    public string? Note { get; set; }

    /// <summary>When the layout was captured.</summary>
    public DateTimeOffset CapturedAt { get; init; } = DateTimeOffset.Now;

    /// <summary>True when a background job took this snapshot rather than the user.</summary>
    public bool IsAutomatic { get; init; }

    /// <summary>True when the user pinned this layout so automatic pruning leaves it alone.</summary>
    public bool IsPinned { get; set; }

    /// <summary>The desktop view settings in force at capture time.</summary>
    public DesktopViewOptions ViewOptions { get; init; }

    /// <summary>The monitor arrangement this layout belongs to.</summary>
    public DisplayFingerprint Display { get; init; } = new();

    /// <summary>Every icon that was on the desktop, in list view order.</summary>
    public IReadOnlyList<IconEntry> Icons { get; init; } = Array.Empty<IconEntry>();

    /// <summary>The version of Icons Saver that wrote the file.</summary>
    public string AppVersion { get; init; } = AppInfo.Version;

    /// <summary>Number of icons in the layout.</summary>
    [JsonIgnore]
    public int IconCount => Icons.Count;

    /// <summary>A label for lists: the name, or a timestamp when the user never named it.</summary>
    [JsonIgnore]
    public string DisplayName =>
        string.IsNullOrWhiteSpace(Name)
            ? CapturedAt.LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss")
            : Name;

    /// <summary>Builds a snapshot from a live reading of the desktop.</summary>
    public static LayoutSnapshot Create(
        string name,
        IEnumerable<DesktopIcon> icons,
        DisplayFingerprint display,
        DesktopViewOptions viewOptions,
        bool isAutomatic = false)
    {
        ArgumentNullException.ThrowIfNull(icons);
        ArgumentNullException.ThrowIfNull(display);

        return new LayoutSnapshot
        {
            Name = name ?? string.Empty,
            Icons = icons.Select(IconEntry.FromDesktopIcon).ToList(),
            Display = display,
            ViewOptions = viewOptions,
            IsAutomatic = isAutomatic,
        };
    }

    public override string ToString() => $"{DisplayName} ({IconCount} icons, {Display.Describe()})";
}
