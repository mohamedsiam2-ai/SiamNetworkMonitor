# Architecture

- **WPF UI**: dashboard, settings, alert editor, and per-application charts.
- **NetworkMonitor**: adapter-level rates.
- **ProcessTrafficMonitor**: ETW-based per-process network capture.
- **UsageHistoryService**: hourly JSON aggregation with 90-day retention.
- **ApplicationIdentityService**: executable description, company, path, and signature metadata.
- **AlertEngine**: rule evaluation, cooldowns, sustained thresholds, and durable alert events.
- **TaskbarMeterWindow**: movable meter with Explorer taskbar ownership and automatic reattachment.

Runtime data is stored under `%LocalAppData%\SiamNetworkMonitor`. UI collections and chart samples are bounded to avoid unbounded memory growth.
