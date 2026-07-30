namespace IconsSaver.Cli;

/// <summary>
/// A very small argument parser.
/// </summary>
/// <remarks>
/// The CLI has a handful of verbs and a handful of switches, which is well short of the point where
/// a parsing library earns its dependency. Anything of the form <c>--name value</c> becomes an
/// option; <c>--name</c> on its own becomes a flag; everything else is positional.
/// </remarks>
internal sealed class CommandLineArgs
{
    private readonly Dictionary<string, string> _options = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _flags = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _positional = new();

    private CommandLineArgs()
    {
    }

    /// <summary>The verb, or an empty string when none was given.</summary>
    public string Command { get; private set; } = string.Empty;

    /// <summary>Positional arguments after the verb.</summary>
    public IReadOnlyList<string> Positional => _positional;

    public static CommandLineArgs Parse(string[] arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        var parsed = new CommandLineArgs();

        for (int index = 0; index < arguments.Length; index++)
        {
            string argument = arguments[index];

            if (argument.StartsWith("--", StringComparison.Ordinal) || argument.StartsWith('-'))
            {
                string name = argument.TrimStart('-');
                bool hasValue = index + 1 < arguments.Length && !arguments[index + 1].StartsWith('-');

                if (hasValue)
                {
                    parsed._options[name] = arguments[++index];
                }
                else
                {
                    parsed._flags.Add(name);
                }

                continue;
            }

            if (parsed.Command.Length == 0)
            {
                parsed.Command = argument;
            }
            else
            {
                parsed._positional.Add(argument);
            }
        }

        return parsed;
    }

    /// <summary>Reads an option, accepting any of the given names.</summary>
    public string? Option(params string[] names)
    {
        foreach (string name in names)
        {
            if (_options.TryGetValue(name, out string? value))
            {
                return value;
            }
        }

        return null;
    }

    /// <summary>Whether any of the given switch names was present.</summary>
    public bool Flag(params string[] names) => names.Any(_flags.Contains);

    /// <summary>The positional argument at <paramref name="index"/>, or <see langword="null"/>.</summary>
    public string? At(int index) => index >= 0 && index < _positional.Count ? _positional[index] : null;
}
