namespace IconsSaver.Core.Models;

/// <summary>
/// Which part of a saved layout a restore should apply.
/// </summary>
/// <remarks>
/// <para>
/// An icon is in scope when its <em>saved</em> position falls inside the region — not its current
/// one. That is the rule that solves the problem people actually have: unplug a monitor and Windows
/// packs everything that lived on it onto the screen that remains. Those icons are now nowhere near
/// where they belong, so a rule based on where they currently sit would decide there is nothing to
/// do. Filtering on the destination puts them back and leaves the screen that still looks right
/// untouched.
/// </para>
/// <para>
/// The region is expressed in the coordinate space of the <em>snapshot</em>, which is the space the
/// preview draws. Selecting on a picture of the saved layout therefore means what it looks like it
/// means, even when the layout is being forced onto a display setup it was not captured on.
/// </para>
/// </remarks>
public sealed record RestoreScope
{
    /// <summary>Restore everything in the layout.</summary>
    public static readonly RestoreScope Everything = new();

    /// <summary>An explicit rectangle, in the snapshot's icon coordinates.</summary>
    public IconRegion? Region { get; init; }

    /// <summary>A monitor from the snapshot's display, identified by its stable device id.</summary>
    public string? MonitorDeviceId { get; init; }

    /// <summary>A short description for the UI and for the restore report.</summary>
    public string? Label { get; init; }

    /// <summary>True when nothing is being filtered out.</summary>
    public bool IsEverything => Region is null && string.IsNullOrEmpty(MonitorDeviceId);

    /// <summary>Restores only the icons that belong to one monitor of the saved setup.</summary>
    public static RestoreScope ForMonitor(MonitorDescriptor monitor)
    {
        ArgumentNullException.ThrowIfNull(monitor);

        return new RestoreScope
        {
            MonitorDeviceId = monitor.DeviceId,
            Label = monitor.DeviceName,
        };
    }

    /// <summary>Restores only the icons whose saved position falls inside a rectangle.</summary>
    public static RestoreScope ForRegion(IconRegion region) => new()
    {
        Region = region,
        Label = $"selected area ({region})",
    };

    /// <summary>
    /// Works out the actual rectangle, or <see langword="null"/> when the whole layout is in scope.
    /// </summary>
    /// <remarks>
    /// A monitor id that no longer appears in the snapshot resolves to an empty region rather than
    /// to "everything": the safe reading of "restore the monitor that is not there" is to move
    /// nothing, not to move all of it.
    /// </remarks>
    public IconRegion? Resolve(DisplayFingerprint display)
    {
        ArgumentNullException.ThrowIfNull(display);

        if (Region is IconRegion explicitRegion)
        {
            return explicitRegion;
        }

        if (string.IsNullOrEmpty(MonitorDeviceId))
        {
            return null;
        }

        MonitorDescriptor? monitor = display.Monitors
            .FirstOrDefault(m => string.Equals(m.DeviceId, MonitorDeviceId, StringComparison.OrdinalIgnoreCase));

        return monitor is null
            ? new IconRegion(0, 0, 0, 0)
            : IconRegion.ForMonitor(display, monitor);
    }

    /// <summary>Whether a saved entry should be restored under this scope.</summary>
    public bool Includes(IconEntry entry, DisplayFingerprint display)
    {
        ArgumentNullException.ThrowIfNull(entry);

        IconRegion? region = Resolve(display);
        return region is null || region.Value.Contains(entry.Position);
    }

    public override string ToString() => IsEverything ? "the whole desktop" : Label ?? "a selection";
}
