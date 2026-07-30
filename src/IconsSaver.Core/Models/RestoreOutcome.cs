namespace IconsSaver.Core.Models;

/// <summary>Why a restore ended the way it did.</summary>
public enum RestoreOutcome
{
    /// <summary>Every saved entry was matched and placed.</summary>
    Completed = 0,

    /// <summary>The layout was applied, but some entries had no icon left to move.</summary>
    CompletedWithGaps,

    /// <summary>Nothing was applied because the display arrangement no longer matches.</summary>
    SkippedDisplayMismatch,

    /// <summary>There was nothing to do: the layout was empty, or every icon was already in place.</summary>
    NothingToDo,

    /// <summary>The restore was a dry run and moved nothing on purpose.</summary>
    Previewed,
}
