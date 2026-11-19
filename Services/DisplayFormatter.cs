namespace SiamNetworkMonitor.Services;

public static class DisplayFormatter
{
    public static string Rate(double kilobytesPerSecond)
    {
        if (kilobytesPerSecond >= 1024)
            return $"{kilobytesPerSecond / 1024:F2} MB/s";

        return $"{kilobytesPerSecond:F2} KB/s";
    }

    public static string Data(double kilobytes)
    {
        if (kilobytes >= 1024 * 1024)
            return $"{kilobytes / 1024 / 1024:F2} GB";

        if (kilobytes >= 1024)
            return $"{kilobytes / 1024:F2} MB";

        return $"{kilobytes:F2} KB";
    }

    public static string LinkSpeed(double megabitsPerSecond)
    {
        if (megabitsPerSecond >= 1000)
            return $"{megabitsPerSecond / 1000:F2} Gbps";

        return $"{megabitsPerSecond:F0} Mbps";
    }
}
