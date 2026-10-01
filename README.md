<div align="center">

<img src="assets/logo.png" width="112" alt="FocusTracker logo" />

# FocusTracker

**Offline Pomodoro timer + automatic app-usage tracker for Windows.**
See where your day actually goes — every byte of data stays on your PC.

<a href="https://github.com/Satyam1616/FocusTracker/releases/latest/download/FocusTracker-Setup.exe">
  <img src="https://img.shields.io/badge/⬇%20Download-Setup.exe-FF6B5E?style=for-the-badge&logo=windows&logoColor=white" alt="Download FocusTracker" />
</a>

<br/><br/>

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)
![Platform](https://img.shields.io/badge/Windows-10%20%2F%2011-0078D6?logo=windows&logoColor=white)
![License](https://img.shields.io/badge/License-MIT-3fb950)
![Offline](https://img.shields.io/badge/100%25-Offline-success)

</div>

---

## Overview

FocusTracker pairs a classic **Pomodoro timer** with a lightweight **active-window tracker**. While you work, it quietly records how long each application stays in the foreground, then shows you a clean daily breakdown — so your focus sessions and your real time usage live in one place. No account, no cloud, no telemetry.

## Features

- 🍅 **Pomodoro timer** — configurable focus / short-break / long-break lengths with automatic cycling and a completed-sessions counter.
- 🔊 **Two distinct chimes** — a calm descending tone when a focus session ends, a brighter rising tone when a break ends (synthesized in-app, no sound files).
- 📊 **Usage dashboard** — per-day active time, focus time, idle time, a ranked "where your time went" bar list, and a 7-day activity trend.
- 🪟 **Mini / picture-in-picture mode** — a draggable, always-on-top timer widget that stays visible over any window; its position is remembered.
- 💤 **Idle detection** — time with no keyboard/mouse input is tracked separately so your active totals stay honest.
- 🔒 **Fully offline** — all data is stored locally in `%APPDATA%\FocusTracker`. Nothing is ever uploaded.

<!-- PLACEHOLDER_APPEND -->
## Download & Install

1. Click **[⬇ Download Setup.exe](https://github.com/Satyam1616/FocusTracker/releases/latest/download/FocusTracker-Setup.exe)**.
2. Run the installer. It installs per-user (no admin rights needed) and adds a Start-menu shortcut.
3. Launch **FocusTracker** and start a session.

> **Note:** the installer is not code-signed yet, so Windows SmartScreen may show a warning. Click **More info → Run anyway** to continue.

## Build from source

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download).

```bash
git clone https://github.com/Satyam1616/FocusTracker.git
cd FocusTracker
dotnet run
```

To produce the installer yourself (uses the [Velopack](https://velopack.io) `vpk` tool):

```bash
dotnet tool install -g vpk
dotnet publish -c Release -r win-x64 --self-contained -o publish
vpk pack --packId FocusTracker --packVersion 1.0.0 --packDir publish --mainExe FocusTracker.exe --icon app.ico
# -> Setup.exe is written to ./Releases
```

## How it works

| Layer | Role |
|-------|------|
| `Native/NativeMethods.cs` | Win32 interop — foreground window + `GetLastInputInfo` idle detection |
| `Services/ActiveWindowTracker.cs` | Polls once a second, records per-app time segments, flushes every 30s |
| `Services/PomodoroService.cs` | Timer state machine, phase transitions, session persistence |
| `Services/ChimePlayer.cs` | Synthesizes the two completion chimes as in-memory PCM |
| `Services/Database.cs` | Local SQLite store + per-day aggregates |
| `Views/` | Timer, Dashboard and Settings screens + the floating mini timer |

**Tech:** C# · WPF · .NET 8 · SQLite (`Microsoft.Data.Sqlite`) · Velopack.

## Privacy

FocusTracker makes **no network connections**. Your activity log, pomodoro history and settings never leave your machine — they live in `%APPDATA%\FocusTracker` and can be deleted at any time.

## Roadmap

- [ ] System-tray icon + keep tracking in the background
- [ ] Start automatically on Windows login
- [ ] App categorization (Productive / Neutral / Distracting) + focus score
- [ ] CSV / JSON export and weekly/monthly views
- [ ] Global hotkey to start/pause

## License

Released under the [MIT License](LICENSE).

