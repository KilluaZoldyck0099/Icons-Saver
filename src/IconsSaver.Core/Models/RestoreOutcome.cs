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

    /// <summary>
    /// Nothing was applied because the selected part of the desktop cannot be reached right now —
    /// the screen it describes is unplugged, or the desktop origin has moved so the saved
    /// coordinates would land somewhere else entirely.
    /// </summary>
    SkippedScopeUnavailable,

    /// <summary>There was nothing to do: the layout was empty, or every icon was already in place.</summary>
    NothingToDo,

    /// <summary>The restore was a dry run and moved nothing on purpose.</summary>
    Previewed,
}
