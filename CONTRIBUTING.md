# Contributing

Thanks for taking an interest.

## Building

```powershell
dotnet build IconsSaver.sln -c Release
dotnet test IconsSaver.sln -c Release
```

You need the .NET 8 SDK and 64-bit Windows. The build is pinned to x64 on purpose; see
[docs/how-it-works.md](docs/how-it-works.md) for why a 32-bit build fails in a way that
looks like it is working.

## Project layout

| Project | What it is |
| --- | --- |
| `src/IconsSaver.Core` | Everything that touches the shell, the models, and storage |
| `src/IconsSaver.App` | The WinForms tray application |
| `src/IconsSaver.Cli` | The command line tool |
| `tests/IconsSaver.Tests` | xUnit tests for the parts that do not need a desktop |

The interop layer is confined to `IconsSaver.Core/Native` and `IconsSaver.Core/Desktop`.
Everything above that is ordinary C# and is unit tested.

## Testing changes to the interop layer

The tests deliberately do not drive a real desktop, because a test that moves 200 icons on
the machine running it is not a test anyone will enjoy. If you change how positions are
read or written, verify by hand:

```powershell
iconssaver save "before my change"
# move a couple of icons around
iconssaver restore --dry-run
iconssaver restore
```

A restore that reports `Moved N of N matched icons` and visibly puts the icons back is the
bar. Please say in the pull request what you tested it on: Windows version, number of
monitors, and whether any of them are scaled.

## Style

- `.editorconfig` is authoritative. Run `dotnet format` if in doubt.
- File-scoped namespaces, explicit types over `var` unless the type is obvious.
- Comments explain *why*, not *what*. The interop layer especially: if a magic number or an
  ordering constraint is not obvious, write down what happens when it is wrong.

## Commits

One logical change per commit, present tense, with a scope:

```
feat(desktop): move icons with LVM_SETITEMPOSITION32 for exact coordinates
fix(core): refuse to capture while desktop icons are hidden
docs: explain the 16-bit coordinate trap
```

## Reporting a bug

Include the output of `iconssaver status`, and the relevant part of
`%APPDATA%\IconsSaver\logs\iconssaver.log` if there is one. Both say more about a failed
restore than a description can.
