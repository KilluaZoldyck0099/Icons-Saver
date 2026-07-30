namespace IconsSaver.Cli;

/// <summary>
/// Console writing with a consistent voice: results on stdout, problems on stderr.
/// </summary>
/// <remarks>
/// Keeping diagnostics off stdout means <c>iconssaver list</c> can be piped somewhere without the
/// caller having to filter chatter out of the stream.
/// </remarks>
internal static class ConsoleOutput
{
    /// <summary>Whether colour is used. Turned off automatically when output is redirected.</summary>
    public static bool UseColor { get; set; } = !Console.IsOutputRedirected;

    public static void Line(string text = "") => Console.WriteLine(text);

    public static void Success(string text) => Write(text, ConsoleColor.Green);

    public static void Warn(string text) => Write(text, ConsoleColor.Yellow);

    public static void Detail(string text) => Write(text, ConsoleColor.DarkGray);

    public static void Error(string text)
    {
        if (!UseColor)
        {
            Console.Error.WriteLine(text);
            return;
        }

        ConsoleColor previous = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine(text);
        Console.ForegroundColor = previous;
    }

    /// <summary>Prints a simple left-aligned table with a header rule.</summary>
    public static void Table(IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows)
    {
        ArgumentNullException.ThrowIfNull(headers);
        ArgumentNullException.ThrowIfNull(rows);

        int[] widths = headers.Select(h => h.Length).ToArray();
        foreach (IReadOnlyList<string> row in rows)
        {
            for (int column = 0; column < widths.Length && column < row.Count; column++)
            {
                widths[column] = Math.Max(widths[column], row[column]?.Length ?? 0);
            }
        }

        Line(string.Join("  ", headers.Select((h, i) => h.PadRight(widths[i]))).TrimEnd());
        Detail(string.Join("  ", widths.Select(w => new string('-', w))));

        foreach (IReadOnlyList<string> row in rows)
        {
            var cells = new string[widths.Length];
            for (int column = 0; column < widths.Length; column++)
            {
                cells[column] = (column < row.Count ? row[column] : string.Empty).PadRight(widths[column]);
            }

            Line(string.Join("  ", cells).TrimEnd());
        }
    }

    private static void Write(string text, ConsoleColor color)
    {
        if (!UseColor)
        {
            Console.WriteLine(text);
            return;
        }

        ConsoleColor previous = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine(text);
        Console.ForegroundColor = previous;
    }
}
