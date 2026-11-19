# Contributing

1. Create a focused branch from `main`.
2. Keep changes compatible with .NET 8 and WPF on Windows.
3. Preserve the native working ComboBox implementation in the Alert Rule Editor.
4. Keep traffic histories and charts bounded to prevent long-running memory growth.
5. Do not add forced `GC.Collect()` calls as a memory optimization.
6. Build in Release mode and test dashboard hiding, taskbar integration, Explorer restart recovery, alerts, history persistence, and clean exit.
7. Submit one logical change per pull request with screenshots for UI changes.

Never commit `%LocalAppData%` history, local logs, build output, or executable files.
