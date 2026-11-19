# Siam Network Monitor

A Windows network-usage monitor built with .NET 8 and WPF. It provides a taskbar-integrated live bandwidth meter, per-application traffic, persistent history, application identity, configurable alerts, and bounded live charts.

## Highlights

- Live download and upload rates
- Movable taskbar-integrated meter with saved position
- Per-application bandwidth and 120-second live charts
- Persistent Today, 7-day, and 30-day usage history
- Application description, publisher, signature state, and executable actions
- Configurable usage and speed alerts with Windows notifications
- Search, active-only filter, pause/resume, and CSV export
- Start with Windows and start-minimized options
- Memory-bounded collections and chart history

## Requirements

- Windows 10 or Windows 11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) to build
- Administrator approval at launch for ETW per-process traffic capture

## Build and run

1. Clone or download the repository.
2. Exit any running `SiamNetworkMonitor.exe` instance.
3. Double-click `BUILD-AND-RUN.cmd`.
4. Accept the Windows UAC prompt.

Or use PowerShell:

```powershell
dotnet restore
dotnet build -c Release --no-restore
Start-Process ".\bin\Release\net8.0-windows\SiamNetworkMonitor.exe" -Verb RunAs
```

## Usage

- Close the dashboard to hide it while monitoring continues.
- Right-click the meter for settings and Exit.
- Double-click an application row to open its bandwidth chart.
- Use **Alerts > Edit rules** to configure thresholds.
- Use **Settings** to control startup and notifications.

## Local data

Runtime settings and history are stored under:

```text
%LocalAppData%\SiamNetworkMonitor\
```

This includes usage history, alert history, alert rules, and application settings. These files are not committed to Git.

## Important implementation note

Taskbar integration uses Windows Explorer's taskbar window as the meter's native owner. This is intentionally isolated and reattached after Explorer restarts, but Windows updates may affect this experimental integration.

## Privacy

Traffic accounting is processed locally. The application does not upload usage history or application identity data. See [PRIVACY.md](PRIVACY.md).

## Documentation

- [Architecture](docs/ARCHITECTURE.md)
- [Release checklist](docs/RELEASE.md)
- [Contributing](CONTRIBUTING.md)
- [Security policy](SECURITY.md)

## License

No license has been selected yet. Until a license file is added, copyright remains with the repository owner and others do not automatically receive permission to copy, modify, or redistribute the code. See `LICENSE-CHOICE.md` before publishing publicly.
