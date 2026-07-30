# Security policy

## Supported versions

The latest release is supported. Fixes are not backported.

## What this program does that is worth knowing about

Icons Saver reaches into `explorer.exe` to read and write desktop icon positions. That is
inherent to the job — the icons belong to explorer — but it is worth being explicit about
the shape of it:

- It calls `OpenProcess` on the explorer process **belonging to the current user**, with
  `PROCESS_VM_OPERATION | PROCESS_VM_READ | PROCESS_VM_WRITE`.
- It allocates a scratch page in that process with `VirtualAllocEx`, exchanges structures
  through it, and frees it afterwards. Nothing is executed there and no code is injected.
- It runs `asInvoker`. It never asks for elevation and does not need it, because both
  processes run as the same user at the same integrity level.
- It writes only to `%APPDATA%\IconsSaver` and to one per-user registry value under
  `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`, and only when "start with Windows"
  is switched on.
- It makes no network connections.

Some endpoint protection products flag cross-process memory access on principle. The code
doing it is confined to
[`src/IconsSaver.Core/Native/RemoteProcessMemory.cs`](src/IconsSaver.Core/Native/RemoteProcessMemory.cs)
and is short enough to read in full.

## Reporting a vulnerability

Please report security issues privately through
[GitHub's private vulnerability reporting](https://github.com/KilluaZoldyck0099/Icons-Saver/security/advisories/new)
rather than in a public issue.

Include what you did, what happened, and the Windows version you saw it on. You can expect
an acknowledgement within a week.
