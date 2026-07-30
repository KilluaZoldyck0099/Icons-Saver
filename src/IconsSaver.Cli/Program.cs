using System.Globalization;
using IconsSaver.Core;
using IconsSaver.Core.Displays;
using IconsSaver.Core.Models;
using IconsSaver.Core.Services;
using IconsSaver.Core.Storage;

namespace IconsSaver.Cli;

/// <summary>
/// Command line front end, for scripting and for scheduled tasks.
/// </summary>
internal static class Program
{
    private const int ExitOk = 0;
    private const int ExitUsage = 1;
    private const int ExitDesktopUnavailable = 2;
    private const int ExitNotFound = 3;

    private static int Main(string[] rawArguments)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        try
        {
            EnvironmentGuard.EnsureSupportedProcess();
        }
        catch (PlatformNotSupportedException exception)
        {
            ConsoleOutput.Error(exception.Message);
            return ExitUsage;
        }

        AppPaths.EnsureCreated();

        CommandLineArgs arguments = CommandLineArgs.Parse(rawArguments);
        var service = new LayoutService();

        try
        {
            return arguments.Command.ToLowerInvariant() switch
            {
                "save" => Save(service, arguments),
                "list" or "ls" => List(service),
                "restore" => Restore(service, arguments),
                "show" => Show(service, arguments),
                "delete" or "rm" => Delete(service, arguments),
                "export" => Export(service, arguments),
                "import" => Import(service, arguments),
                "status" => Status(),
                "version" => Version(),
                "" or "help" or "--help" => Help(),
                _ => UnknownCommand(arguments.Command),
            };
        }
        catch (DesktopInteropException exception)
        {
            ConsoleOutput.Error(exception.Message);
            return ExitDesktopUnavailable;
        }
        catch (FileNotFoundException exception)
        {
            ConsoleOutput.Error(exception.Message);
            return ExitNotFound;
        }
    }

    private static int Save(LayoutService service, CommandLineArgs arguments)
    {
        string name = arguments.Option("name", "n")
            ?? arguments.At(0)
            ?? DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

        LayoutSnapshot snapshot = service.CaptureAndSave(name);

        ConsoleOutput.Success($"Saved '{snapshot.DisplayName}' with {snapshot.IconCount} icons.");
        ConsoleOutput.Detail($"  id       {snapshot.Id}");
        ConsoleOutput.Detail($"  display  {snapshot.Display}");
        ConsoleOutput.Detail($"  view     {snapshot.ViewOptions}");
        return ExitOk;
    }

    private static int List(LayoutService service)
    {
        IReadOnlyList<LayoutSnapshot> snapshots = service.Snapshots.LoadAll();
        if (snapshots.Count == 0)
        {
            ConsoleOutput.Warn("No saved layouts yet. Run 'iconssaver-cli save' to make one.");
            return ExitOk;
        }

        DisplayFingerprint current = DisplayEnumerator.Capture();

        ConsoleOutput.Table(
            new[] { "ID", "NAME", "ICONS", "CAPTURED", "DISPLAY" },
            snapshots.Select(s => (IReadOnlyList<string>)new[]
            {
                s.Id[..8],
                s.DisplayName,
                s.IconCount.ToString(CultureInfo.InvariantCulture),
                s.CapturedAt.LocalDateTime.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                current.Matches(s.Display) ? s.Display.Describe() : $"{s.Display.Describe()} (other setup)",
            }).ToList());

        return ExitOk;
    }

    private static int Restore(LayoutService service, CommandLineArgs arguments)
    {
        string? id = arguments.At(0) ?? arguments.Option("id");
        LayoutSnapshot? snapshot;

        if (string.IsNullOrWhiteSpace(id) || id.Equals("latest", StringComparison.OrdinalIgnoreCase))
        {
            snapshot = service.FindBestForCurrentDisplay() ?? service.Snapshots.LoadAll().FirstOrDefault();
        }
        else
        {
            snapshot = Resolve(service, id);
        }

        if (snapshot is null)
        {
            ConsoleOutput.Error("No matching layout found.");
            return ExitNotFound;
        }

        RestoreOptions options = arguments.Flag("dry-run", "preview")
            ? RestoreOptions.Preview
            : arguments.Flag("force", "f")
                ? RestoreOptions.Manual
                : RestoreOptions.Automatic;

        if (!TryReadScope(arguments, snapshot, out RestoreScope scope))
        {
            return ExitUsage;
        }

        RestoreReport report = service.Restore(snapshot, options.Within(scope));

        if (report.Outcome == RestoreOutcome.SkippedDisplayMismatch)
        {
            ConsoleOutput.Warn(
                $"'{snapshot.DisplayName}' was saved on a different display setup " +
                $"({snapshot.Display.Describe()}). Pass --force to restore it anyway.");
            return ExitOk;
        }

        ConsoleOutput.Success(report.Summarize());

        if (report.AutoArrangeSuspended)
        {
            ConsoleOutput.Warn("Auto arrange was turned off, otherwise the icons would snap straight back.");
        }

        return ExitOk;
    }

    /// <summary>
    /// Reads <c>--monitor</c> or <c>--region</c> into a restore scope.
    /// </summary>
    /// <remarks>
    /// Monitors are addressed by their position in <c>iconssaver-cli show</c>, because a device
    /// interface path is not something anyone is going to type. A bad value is refused rather than
    /// quietly ignored: silently restoring the whole desktop when the user asked for one screen is
    /// the worst possible reading of a typo.
    /// </remarks>
    private static bool TryReadScope(CommandLineArgs arguments, LayoutSnapshot snapshot, out RestoreScope scope)
    {
        scope = RestoreScope.Everything;

        string? monitor = arguments.Option("monitor", "m");
        if (monitor is not null)
        {
            if (!int.TryParse(monitor, NumberStyles.Integer, CultureInfo.InvariantCulture, out int index) ||
                index < 1 || index > snapshot.Display.Monitors.Count)
            {
                ConsoleOutput.Error(
                    $"--monitor must be between 1 and {snapshot.Display.Monitors.Count}. " +
                    $"Run 'iconssaver-cli show {snapshot.Id[..8]}' to see them.");
                return false;
            }

            scope = RestoreScope.ForMonitor(snapshot.Display, snapshot.Display.Monitors[index - 1]);
            return true;
        }

        string? region = arguments.Option("region", "r");
        if (region is not null)
        {
            string[] parts = region.Split(',', StringSplitOptions.TrimEntries);
            if (parts.Length != 4 || !parts.All(p => int.TryParse(p, NumberStyles.Integer, CultureInfo.InvariantCulture, out _)))
            {
                ConsoleOutput.Error("--region takes four whole numbers: x,y,width,height");
                return false;
            }

            int[] numbers = parts.Select(p => int.Parse(p, CultureInfo.InvariantCulture)).ToArray();
            scope = RestoreScope.ForRegion(new IconRegion(numbers[0], numbers[1], numbers[2], numbers[3]));
        }

        return true;
    }

    private static int Show(LayoutService service, CommandLineArgs arguments)
    {
        string? id = arguments.At(0);
        if (id is null)
        {
            ConsoleOutput.Error("Usage: iconssaver-cli show <id>");
            return ExitUsage;
        }

        LayoutSnapshot? snapshot = Resolve(service, id);
        if (snapshot is null)
        {
            ConsoleOutput.Error($"No layout matching '{id}'.");
            return ExitNotFound;
        }

        ConsoleOutput.Line($"{snapshot.DisplayName}");
        ConsoleOutput.Detail($"  id        {snapshot.Id}");
        ConsoleOutput.Detail($"  captured  {snapshot.CapturedAt.LocalDateTime:yyyy-MM-dd HH:mm:ss}");
        ConsoleOutput.Detail($"  icons     {snapshot.IconCount}");
        ConsoleOutput.Detail($"  display   {snapshot.Display}");
        ConsoleOutput.Detail($"  view      {snapshot.ViewOptions}");
        ConsoleOutput.Line();

        ConsoleOutput.Line("Monitors (the number is what --monitor takes):");
        for (int index = 0; index < snapshot.Display.Monitors.Count; index++)
        {
            MonitorDescriptor monitor = snapshot.Display.Monitors[index];
            IconRegion region = IconRegion.ForMonitor(snapshot.Display, monitor);
            int held = snapshot.Icons.Count(icon => region.Contains(icon.Position));

            ConsoleOutput.Detail($"  {index + 1}  {monitor}  -  {held} icons  -  region {region}");
        }

        ConsoleOutput.Line();

        ConsoleOutput.Table(
            new[] { "X", "Y", "NAME" },
            snapshot.Icons.Select(i => (IReadOnlyList<string>)new[]
            {
                i.X.ToString(CultureInfo.InvariantCulture),
                i.Y.ToString(CultureInfo.InvariantCulture),
                i.Name,
            }).ToList());

        return ExitOk;
    }

    private static int Delete(LayoutService service, CommandLineArgs arguments)
    {
        string? id = arguments.At(0);
        if (id is null)
        {
            ConsoleOutput.Error("Usage: iconssaver-cli delete <id>");
            return ExitUsage;
        }

        LayoutSnapshot? snapshot = Resolve(service, id);
        if (snapshot is null || !service.Snapshots.Delete(snapshot.Id))
        {
            ConsoleOutput.Error($"No layout matching '{id}'.");
            return ExitNotFound;
        }

        ConsoleOutput.Success($"Deleted '{snapshot.DisplayName}'.");
        return ExitOk;
    }

    private static int Export(LayoutService service, CommandLineArgs arguments)
    {
        string? id = arguments.At(0);
        string? path = arguments.At(1);

        if (id is null || path is null)
        {
            ConsoleOutput.Error("Usage: iconssaver-cli export <id> <file.json>");
            return ExitUsage;
        }

        LayoutSnapshot? snapshot = Resolve(service, id);
        if (snapshot is null)
        {
            ConsoleOutput.Error($"No layout matching '{id}'.");
            return ExitNotFound;
        }

        SnapshotStore.Export(snapshot, path);
        ConsoleOutput.Success($"Exported '{snapshot.DisplayName}' to {path}.");
        return ExitOk;
    }

    private static int Import(LayoutService service, CommandLineArgs arguments)
    {
        string? path = arguments.At(0);
        if (path is null)
        {
            ConsoleOutput.Error("Usage: iconssaver-cli import <file.json>");
            return ExitUsage;
        }

        LayoutSnapshot snapshot = SnapshotStore.Import(path);
        service.Snapshots.Save(snapshot);

        ConsoleOutput.Success($"Imported '{snapshot.DisplayName}' with {snapshot.IconCount} icons as {snapshot.Id[..8]}.");
        return ExitOk;
    }

    private static int Status()
    {
        DisplayFingerprint display = DisplayEnumerator.Capture();

        ConsoleOutput.Line($"{AppInfo.Name} {AppInfo.Version}");
        ConsoleOutput.Detail($"  data folder  {AppPaths.Root}");
        ConsoleOutput.Detail($"  display      {display}");

        foreach (MonitorDescriptor monitor in display.Monitors)
        {
            ConsoleOutput.Detail($"    {monitor}");
        }

        if (!LayoutService.IsDesktopAvailable)
        {
            ConsoleOutput.Error("  desktop      not reachable");
            return ExitDesktopUnavailable;
        }

        var service = new LayoutService();
        ConsoleOutput.Detail($"  desktop      {service.ReadDesktop().Count} icons, {service.ReadViewOptions()}");
        return ExitOk;
    }

    private static int Version()
    {
        ConsoleOutput.Line($"{AppInfo.Name} {AppInfo.FullVersion}");
        return ExitOk;
    }

    /// <summary>Resolves a full id or any unambiguous prefix of one.</summary>
    private static LayoutSnapshot? Resolve(LayoutService service, string id)
    {
        LayoutSnapshot? exact = service.Snapshots.Load(id);
        if (exact is not null)
        {
            return exact;
        }

        List<LayoutSnapshot> candidates = service.Snapshots.LoadAll()
            .Where(s => s.Id.StartsWith(id, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (candidates.Count > 1)
        {
            ConsoleOutput.Warn($"'{id}' matches {candidates.Count} layouts; use more characters of the id.");
            return null;
        }

        return candidates.FirstOrDefault();
    }

    private static int UnknownCommand(string command)
    {
        ConsoleOutput.Error($"Unknown command '{command}'.");
        Help();
        return ExitUsage;
    }

    private static int Help()
    {
        ConsoleOutput.Line($"{AppInfo.Name} {AppInfo.Version} - save and restore desktop icon positions");
        ConsoleOutput.Line();
        ConsoleOutput.Line("Usage: iconssaver-cli <command> [options]");
        ConsoleOutput.Line();
        ConsoleOutput.Line("Commands:");
        ConsoleOutput.Line("  save [name]            Capture the current icon layout");
        ConsoleOutput.Line("  list                   List saved layouts");
        ConsoleOutput.Line("  restore [id|latest]    Put a saved layout back");
        ConsoleOutput.Line("  show <id>              Print every icon in a layout");
        ConsoleOutput.Line("  delete <id>            Delete a layout");
        ConsoleOutput.Line("  export <id> <file>     Write a layout to a file");
        ConsoleOutput.Line("  import <file>          Read a layout from a file");
        ConsoleOutput.Line("  status                 Show the desktop and display state");
        ConsoleOutput.Line("  version                Print the version");
        ConsoleOutput.Line();
        ConsoleOutput.Line("Options:");
        ConsoleOutput.Line("  --name, -n <text>      Name to save the layout under");
        ConsoleOutput.Line("  --force, -f            Restore even onto a different display setup");
        ConsoleOutput.Line("  --dry-run              Report what a restore would do, without moving anything");
        ConsoleOutput.Line("  --monitor, -m <n>      Restore only the icons belonging to one screen");
        ConsoleOutput.Line("                         (the number comes from 'show')");
        ConsoleOutput.Line("  --region <x,y,w,h>     Restore only the icons saved inside a rectangle");
        return ExitOk;
    }
}
