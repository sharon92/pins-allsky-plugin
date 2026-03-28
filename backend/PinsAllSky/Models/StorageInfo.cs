namespace NINA.PINS.AllSky.Models;

public sealed class StorageStatusInfo
{
    public long PluginUsedBytes { get; set; }
    public long? PluginAvailableBytes { get; set; }
    public long? MaxPluginUsageBytes { get; set; }
    public long DiskAvailableBytes { get; set; }
    public long DiskTotalBytes { get; set; }
    public bool LimitEnabled { get; set; }
    public bool WithinLimit { get; set; } = true;
}

public sealed class SessionCleanupResult
{
    public int DeletedSessionCount { get; set; }
    public List<string> DeletedSessionIds { get; set; } = [];
    public long FreedBytes { get; set; }
    public StorageStatusInfo Storage { get; set; } = new();
}
