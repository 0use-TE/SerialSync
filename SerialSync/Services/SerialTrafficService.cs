using CommunityToolkit.Mvvm.ComponentModel;

namespace SerialSync.Services;

public partial class SerialTrafficService : ObservableObject
{
    [ObservableProperty]
    private long _totalTxBytes;

    [ObservableProperty]
    private long _totalRxBytes;

    public void AddTx(int bytes)
    {
        if (bytes > 0)
            TotalTxBytes += bytes;
    }

    public void AddRx(int bytes)
    {
        if (bytes > 0)
            TotalRxBytes += bytes;
    }

    public void ResetRx() => TotalRxBytes = 0;
}
