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
/// means.
/// </para>
/// </remarks>
public sealed record RestoreScope
{
    /// <summary>Restore everything in the layout.</summary>
    public static readonly RestoreScope Everything = new();

    /// <summary>
    /// The area to restore, in the snapshot's icon coordinates, or <see langword="null"/> for all
    /// of it.
    /// </summary>
    /// <remarks>
    /// Resolved when the scope is created rather than looked up later. An earlier version stored a
    /// monitor's device id and resolved it on demand, which failed open in the worst possible way:
    /// <see cref="MonitorDescriptor.DeviceId"/> is empty whenever Windows declines to report a
    /// device interface path, an empty id made the scope indistinguishable from "everything", and
    /// clicking a single screen would then have restored the whole desktop.
    /// </remarks>
    public IconRegion? Region { get; init; }

    /// <summary>Which monitor this scope came from, when it came from one. Never used for lookup.</summary>
    public string? MonitorDeviceId { get; init; }

    /// <summary>A short description for the UI and for the restore report.</summary>
    public string? Label { get; init; }

    /// <summary>True when nothing is being filtered out.</summary>
    public bool IsEverything => Region is null;

    /// <summary>Restores only the icons that belong to one monitor of the saved setup.</summary>
    public static RestoreScope ForMonitor(DisplayFingerprint display, MonitorDescriptor monitor)
    {
        ArgumentNullException.ThrowIfNull(display);
        ArgumentNullException.ThrowIfNull(monitor);

        return new RestoreScope
        {
            Region = IconRegion.ForMonitor(display, monitor),
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

    /// <summary>The area to restore, or <see langword="null"/> when the whole layout is in scope.</summary>
    /// <remarks>
    /// The parameter is kept so callers read naturally and so the signature survives if a future
    /// scope kind does need the display to resolve itself.
    /// </remarks>
    public IconRegion? Resolve(DisplayFingerprint display)
    {
        ArgumentNullException.ThrowIfNull(display);
        return Region;
    }

    /// <summary>Whether a saved entry should be restored under this scope.</summary>
    public bool Includes(IconEntry entry, DisplayFingerprint display)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(display);

        return Region is not IconRegion region || region.Contains(entry.Position);
    }

    public override string ToString() => IsEverything ? "the whole desktop" : Label ?? "a selection";
}
