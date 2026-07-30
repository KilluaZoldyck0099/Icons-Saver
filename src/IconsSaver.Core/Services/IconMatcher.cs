using IconsSaver.Core.Models;

namespace IconsSaver.Core.Services;

/// <summary>
/// Decides which live icon each saved entry refers to.
/// </summary>
/// <remarks>
/// <para>
/// The obvious key, the list view index, is useless across time: creating one file renumbers every
/// icon after it. Display name is the practical alternative, but it is not guaranteed unique —
/// the desktop is a merged view of the user's Desktop folder and the shared public one, so the
/// same name really can appear twice.
/// </para>
/// <para>
/// Matching therefore runs in two passes. The first pairs names that agree exactly; the second
/// gives anything left over a case-insensitive chance, which rescues layouts saved before a file
/// was renamed only in case. Within a pass, ties are broken by whichever candidate sat closest to
/// the recorded index, on the assumption that the desktop has not been reordered wholesale.
/// </para>
/// </remarks>
public static class IconMatcher
{
    /// <summary>Pairs saved entries with the icons currently on the desktop.</summary>
    public static IconMatchResult Match(IReadOnlyList<IconEntry> entries, IReadOnlyList<DesktopIcon> icons)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(icons);

        if (entries.Count == 0 || icons.Count == 0)
        {
            return new IconMatchResult(
                Array.Empty<IconMatch>(),
                entries.ToList(),
                icons.ToList());
        }

        var available = new List<DesktopIcon>(icons);
        var matches = new List<IconMatch>(entries.Count);
        var pending = new List<IconEntry>();

        foreach (IconEntry entry in entries)
        {
            DesktopIcon? best = TakeBest(available, entry, StringComparison.Ordinal);
            if (best is not null)
            {
                matches.Add(new IconMatch(entry, best));
            }
            else
            {
                pending.Add(entry);
            }
        }

        var missing = new List<IconEntry>();
        foreach (IconEntry entry in pending)
        {
            DesktopIcon? best = TakeBest(available, entry, StringComparison.OrdinalIgnoreCase);
            if (best is not null)
            {
                matches.Add(new IconMatch(entry, best));
            }
            else
            {
                missing.Add(entry);
            }
        }

        return new IconMatchResult(matches, missing, available);
    }

    /// <summary>
    /// Removes and returns the best candidate for <paramref name="entry"/>, or <see langword="null"/>
    /// when nothing in <paramref name="available"/> carries that name.
    /// </summary>
    private static DesktopIcon? TakeBest(List<DesktopIcon> available, IconEntry entry, StringComparison comparison)
    {
        if (string.IsNullOrEmpty(entry.Name))
        {
            return null;
        }

        DesktopIcon? best = null;
        int bestDistance = int.MaxValue;

        foreach (DesktopIcon candidate in available)
        {
            if (!string.Equals(candidate.Name, entry.Name, comparison))
            {
                continue;
            }

            int distance = Math.Abs(candidate.Index - entry.Index);
            if (distance < bestDistance)
            {
                best = candidate;
                bestDistance = distance;
            }
        }

        if (best is not null)
        {
            available.Remove(best);
        }

        return best;
    }
}
