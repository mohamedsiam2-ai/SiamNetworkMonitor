# Privacy

Siam Network Monitor processes network counters, process identity, executable metadata, usage history, and alert history locally on the Windows device.

The application does not intentionally transmit telemetry, analytics, usage history, process names, executable paths, or alert data to an external service.

Local data is stored under `%LocalAppData%\SiamNetworkMonitor`. CSV exports are written only to the location selected by the user.

Per-process monitoring uses Windows ETW and requires elevated execution. Users should review the source and build the application themselves for sensitive environments.
