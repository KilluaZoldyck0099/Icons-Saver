using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace IconsSaver.Core.Models;

/// <summary>
/// A description of the display setup a layout belongs to.
/// </summary>
/// <remarks>
/// Icon positions are only meaningful on the arrangement they were captured on: restoring a
/// three-monitor layout onto a single laptop screen would fling most icons off-screen. Every
/// snapshot therefore carries one of these, and automatic restores only fire when the fingerprint
/// of the current setup matches.
/// </remarks>
public sealed class DisplayFingerprint : IEquatable<DisplayFingerprint>
{
    private string? _signature;

    /// <summary>Left edge of the virtual screen, in screen coordinates.</summary>
    public int VirtualLeft { get; init; }

    /// <summary>Top edge of the virtual screen, in screen coordinates.</summary>
    public int VirtualTop { get; init; }

    /// <summary>Total width of the virtual screen, in pixels.</summary>
    public int VirtualWidth { get; init; }

    /// <summary>Total height of the virtual screen, in pixels.</summary>
    public int VirtualHeight { get; init; }

    /// <summary>Every monitor that was attached, ordered by their signatures for stability.</summary>
    public IReadOnlyList<MonitorDescriptor> Monitors { get; init; } = Array.Empty<MonitorDescriptor>();

    /// <summary>Number of attached monitors.</summary>
    [JsonIgnore]
    public int MonitorCount => Monitors.Count;

    /// <summary>
    /// A short, stable hash of the whole arrangement. Two setups with the same signature accept
    /// each other's layouts.
    /// </summary>
    public string Signature => _signature ??= ComputeSignature();

    /// <summary>
    /// Whether a layout captured on <paramref name="other"/> can be restored here without moving
    /// icons onto coordinates that no longer exist.
    /// </summary>
    public bool Matches(DisplayFingerprint? other) =>
        other is not null && string.Equals(Signature, other.Signature, StringComparison.Ordinal);

    /// <summary>
    /// A weaker comparison that ignores DPI and monitor identity, and only asks whether the drawable
    /// area is the same shape.
    /// </summary>
    /// <remarks>
    /// Useful as a fallback: swapping a monitor for an identical model changes the device id but not
    /// where any icon can legally sit.
    /// </remarks>
    public bool MatchesGeometry(DisplayFingerprint? other) =>
        other is not null &&
        VirtualLeft == other.VirtualLeft &&
        VirtualTop == other.VirtualTop &&
        VirtualWidth == other.VirtualWidth &&
        VirtualHeight == other.VirtualHeight &&
        MonitorCount == other.MonitorCount;

    /// <summary>A one-line summary suitable for a tooltip or a list column.</summary>
    public string Describe()
    {
        string plural = MonitorCount == 1 ? "monitor" : "monitors";
        return $"{MonitorCount} {plural}, {VirtualWidth}x{VirtualHeight}";
    }

    private string ComputeSignature()
    {
        var builder = new StringBuilder();
        builder.Append(VirtualLeft).Append(',')
               .Append(VirtualTop).Append(',')
               .Append(VirtualWidth).Append('x')
               .Append(VirtualHeight);

        foreach (string monitor in Monitors.Select(m => m.Signature).OrderBy(s => s, StringComparer.Ordinal))
        {
            builder.Append('|').Append(monitor);
        }

        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()));
        return Convert.ToHexString(hash, 0, 8).ToLowerInvariant();
    }

    public bool Equals(DisplayFingerprint? other) => Matches(other);

    public override bool Equals(object? obj) => obj is DisplayFingerprint other && Matches(other);

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Signature);

    public override string ToString() => $"{Signature} ({Describe()})";
}
