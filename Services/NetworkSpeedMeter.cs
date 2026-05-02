using System.Diagnostics;
using System.Net.NetworkInformation;

namespace PetWindow.Services;

public sealed class NetworkSpeedMeter
{
    private long _lastBytes;
    private long _lastTicks;

    public string Sample()
    {
        long total = 0;
        try
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up)
                    continue;
                if (ni.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel)
                    continue;
                var stat = ni.GetIPStatistics();
                total += stat.BytesReceived + stat.BytesSent;
            }
        }
        catch
        {
            return "—";
        }

        var now = Stopwatch.GetTimestamp();
        if (_lastTicks == 0)
        {
            _lastBytes = total;
            _lastTicks = now;
            return "—";
        }

        var elapsed = (now - _lastTicks) / (double)Stopwatch.Frequency;
        if (elapsed < 0.2)
            return "—";

        var delta = total - _lastBytes;
        _lastBytes = total;
        _lastTicks = now;
        var bps = delta / elapsed;
        return FormatRate(bps);
    }

    private static string FormatRate(double bytesPerSec)
    {
        if (bytesPerSec < 1024)
            return $"{bytesPerSec:F0} B/s";
        if (bytesPerSec < 1024 * 1024)
            return $"{bytesPerSec / 1024:F1} KB/s";
        return $"{bytesPerSec / (1024 * 1024):F2} MB/s";
    }
}
