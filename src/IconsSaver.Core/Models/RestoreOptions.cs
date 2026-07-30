namespace IconsSaver.Core.Models;

/// <summary>
/// How a restore should behave.
/// </summary>
public sealed record RestoreOptions
{
    /// <summary>
    /// Refuse to run when the snapshot was taken on a different display arrangement.
    /// </summary>
    /// <remarks>
    /// On for automatic restores, where a wrong guess would scatter icons unattended, and off for
    /// manual ones, where the user has asked for it explicitly and can undo it by restoring another
    /// layout.
    /// </remarks>
    public bool RequireMatchingDisplay { get; init; }

    /// <summary>
    /// Turn "Auto arrange icons" back on afterwards.
    /// </summary>
    /// <remarks>
    /// Off by default, and that is not an oversight. Auto arrange repacks every icon the moment it
    /// is switched on, which would undo the restore that just finished. The setting is reported back
    /// in <see cref="RestoreReport.AutoArrangeSuspended"/> so the user can be told it was changed.
    /// </remarks>
    public bool ReenableAutoArrange { get; init; }

    /// <summary>Work out what would happen without moving anything.</summary>
    public bool DryRun { get; init; }

    /// <summary>Which part of the layout to apply. Defaults to all of it.</summary>
    public RestoreScope Scope { get; init; } = RestoreScope.Everything;

    /// <summary>Returns a copy of these options narrowed to part of the desktop.</summary>
    public RestoreOptions Within(RestoreScope scope)
    {
        ArgumentNullException.ThrowIfNull(scope);
        return this with { Scope = scope };
    }

    /// <summary>Defaults for a restore the user asked for.</summary>
    public static RestoreOptions Manual { get; } = new();

    /// <summary>Defaults for a restore triggered by a display change.</summary>
    public static RestoreOptions Automatic { get; } = new() { RequireMatchingDisplay = true };

    /// <summary>Defaults for previewing a restore.</summary>
    public static RestoreOptions Preview { get; } = new() { DryRun = true };
}
