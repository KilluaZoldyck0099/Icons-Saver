using IconsSaver.Core.Models;
using IconsSaver.Core.Native;

namespace IconsSaver.Core.Desktop;

/// <summary>
/// The half of <see cref="DesktopListView"/> that moves icons around.
/// </summary>
public sealed partial class DesktopListView
{
    /// <summary>Moves one item to an absolute desktop position.</summary>
    /// <remarks>
    /// <para>
    /// This deliberately uses <c>LVM_SETITEMPOSITION32</c> rather than <c>LVM_SETITEMPOSITION</c>.
    /// The older message packs both coordinates into a single 32-bit <c>lParam</c>, which silently
    /// truncates anything past 32767 and mangles negative values; the wide desktops this tool
    /// exists for routinely exceed that. The 32-bit variant instead takes a pointer to a
    /// <c>POINT</c>, which is exact.
    /// </para>
    /// <para>
    /// The call is a no-op as far as the shell is concerned while auto arrange is on, so callers
    /// should wrap batches in <see cref="SuspendAutoArrange"/>.
    /// </para>
    /// </remarks>
    /// <returns><see langword="true"/> when the shell accepted the move.</returns>
    public bool SetPosition(int index, IconPosition position)
    {
        ThrowIfDisposed();
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        _memory.WriteInt32(PointOffset, position.X);
        _memory.WriteInt32(PointOffset + 4, position.Y);
        _memory.Push(PointOffset, PointSize);

        IntPtr accepted = Send(
            NativeConstants.LVM_SETITEMPOSITION32,
            index,
            _memory.Remote(PointOffset),
            $"a move request for item {index}");

        return accepted != IntPtr.Zero;
    }

    /// <summary>Applies a batch of moves and reports how many the shell accepted.</summary>
    public int SetPositions(IEnumerable<KeyValuePair<int, IconPosition>> placements)
    {
        ArgumentNullException.ThrowIfNull(placements);
        ThrowIfDisposed();

        int applied = 0;
        foreach ((int index, IconPosition position) in placements)
        {
            if (SetPosition(index, position))
            {
                applied++;
            }
        }

        return applied;
    }

    /// <summary>Repaints the icon area after a batch of moves.</summary>
    /// <remarks>
    /// The list view invalidates itself on every accepted move, but a batch of a few hundred moves
    /// can leave trails behind on some drivers, so one explicit redraw at the end is cheap insurance.
    /// </remarks>
    public void Redraw()
    {
        ThrowIfDisposed();

        int count = Count;
        if (count <= 0)
        {
            return;
        }

        Send(NativeConstants.LVM_REDRAWITEMS, 0, count - 1, "a redraw request");
    }

    /// <summary>
    /// Asks the shell to re-pack every icon into its default arrangement.
    /// </summary>
    /// <remarks>Destructive: this is the "tidy up" the user may want, never part of a restore.</remarks>
    public void ArrangeIcons()
    {
        ThrowIfDisposed();
        Send(NativeConstants.LVM_ARRANGE, IntPtr.Zero, IntPtr.Zero, "an arrange request");
    }
}
