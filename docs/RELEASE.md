# Release checklist

1. Update the version in the project and release notes.
2. Exit every running `SiamNetworkMonitor.exe` process.
3. Run `dotnet restore` and `dotnet build -c Release --no-restore`.
4. Test on Windows 11:
   - UAC launch and ETW capture
   - Taskbar meter placement and Explorer restart
   - Dashboard hide/show and Alt+Tab behavior
   - Application identity and context menu
   - History persistence after restart
   - Alert creation, notification, acknowledge, resolve, and clear
   - Alert Rule Editor ComboBoxes and black text
   - Application charts and 120-sample bound
   - Start with Windows and start minimized
5. Confirm no `bin`, `obj`, logs, local JSON history, or secrets are included.
6. Create a ZIP from the Release output or attach the executable bundle to a GitHub Release.
7. Publish SHA-256 checksums with the release assets.
