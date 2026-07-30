namespace IconsSaver.Core.Models;

/// <summary>
/// One monitor as it was configured when a snapshot was taken.
/// </summary>
/// <param name="DeviceName">The GDI device name, for example <c>\\.\DISPLAY1</c>. Not stable across reboots.</param>
/// <param name="DeviceId">
/// The device interface path of the physical monitor. Unlike <paramref name="DeviceName"/> this
/// stays the same when displays are re-enumerated, so it is what identity is keyed on.
/// </param>
/// <param name="Left">Left edge in virtual screen coordinates. Negative for monitors left of the primary.</param>
/// <param name="Top">Top edge in virtual screen coordinates.</param>
/// <param name="Width">Width in physical pixels.</param>
/// <param name="Height">Height in physical pixels.</param>
/// <param name="IsPrimary">Whether this is the monitor the shell treats as primary.</param>
/// <param name="Dpi">Effective DPI, where 96 means 100% scaling.</param>
public sealed record MonitorDescriptor(
    string DeviceName,
    string DeviceId,
    int Left,
    int Top,
    int Width,
    int Height,
    bool IsPrimary,
    uint Dpi)
{
    /// <summary>Display scaling as a percentage, where 96 DPI is 100%.</summary>
    public int ScalePercent => (int)Math.Round(Dpi * 100.0 / 96.0);

    /// <summary>
    /// A compact, order-independent description used to build the fingerprint of a whole setup.
    /// </summary>
    /// <remarks>
    /// The GDI device name is left out on purpose: Windows hands out <c>\\.\DISPLAY1</c> and
    /// <c>\\.\DISPLAY2</c> in whatever order it enumerated the adapters, and that order changes
    /// across reboots even when nothing about the hardware has.
    /// </remarks>
    public string Signature =>
        $"{Normalize(DeviceId)}@{Left},{Top}:{Width}x{Height}:{Dpi}{(IsPrimary ? ":primary" : string.Empty)}";

    public override string ToString() =>
        $"{DeviceName} {Width}x{Height} at {Left},{Top} ({ScalePercent}%){(IsPrimary ? " primary" : string.Empty)}";

    private static string Normalize(string deviceId) =>
        string.IsNullOrWhiteSpace(deviceId) ? "unknown" : deviceId.Trim();
}
