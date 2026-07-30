using IconsSaver.Core.Models;

namespace IconsSaver.Core.Services;

/// <summary>
/// Compares layouts by what actually matters: which icon is where.
/// </summary>
public static class LayoutComparer
{
    /// <summary>
    /// Whether two layouts place the same set of names at the same coordinates.
    /// </summary>
    /// <remarks>
    /// Ordering and list view indices are ignored on purpose. Explorer renumbers items whenever the
    /// folder changes, so comparing index-by-index would report a difference every time a download
    /// finished, and automatic snapshots would pile up recording nothing new.
    /// </remarks>
    public static bool HaveSameLayout(LayoutSnapshot? left, LayoutSnapshot? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is null || right is null || left.IconCount != right.IconCount)
        {
            return false;
        }

        Dictionary<string, List<IconPosition>> lookup = BuildLookup(left);

        foreach (IconEntry entry in right.Icons)
        {
            if (!lookup.TryGetValue(entry.Name, out List<IconPosition>? positions))
            {
                return false;
            }

            if (!positions.Remove(entry.Position))
            {
                return false;
            }

            if (positions.Count == 0)
            {
                lookup.Remove(entry.Name);
            }
        }

        return lookup.Count == 0;
    }

    /// <summary>Counts how many icons sit somewhere different between two layouts.</summary>
    public static int CountDifferences(LayoutSnapshot left, LayoutSnapshot right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        Dictionary<string, List<IconPosition>> lookup = BuildLookup(left);
        int differences = 0;

        foreach (IconEntry entry in right.Icons)
        {
            if (lookup.TryGetValue(entry.Name, out List<IconPosition>? positions) && positions.Remove(entry.Position))
            {
                continue;
            }

            differences++;
        }

        return differences + lookup.Values.Sum(v => v.Count);
    }

    private static Dictionary<string, List<IconPosition>> BuildLookup(LayoutSnapshot snapshot)
    {
        var lookup = new Dictionary<string, List<IconPosition>>(snapshot.IconCount, StringComparer.Ordinal);

        foreach (IconEntry entry in snapshot.Icons)
        {
            if (!lookup.TryGetValue(entry.Name, out List<IconPosition>? positions))
            {
                positions = new List<IconPosition>(1);
                lookup[entry.Name] = positions;
            }

            positions.Add(entry.Position);
        }

        return lookup;
    }
}
