namespace NINA.PINS.AllSky.Models;

public sealed class PinsAllSkyStatus
{
    public int BackendPort { get; set; }
    public bool CaptureRunning { get; set; }
    public bool GenerateInProgress { get; set; }
    public bool AdvancedApiReachable { get; set; }
    public bool SequenceRunning { get; set; }
    public string? LastError { get; set; }
    public string? CurrentImageUrl { get; set; }
    public SessionInfo? CurrentSession { get; set; }
    public List<SessionInfo> RecentSessions { get; set; } = [];
    public DependencyStatus Dependencies { get; set; } = new();
    public StorageStatusInfo Storage { get; set; } = new();
    public EstimateBaselineInfo? EstimateBaseline { get; set; }
}

public sealed class DependencyStatus
{
    public bool RpiCamStillAvailable { get; set; }
    public bool FfmpegAvailable { get; set; }
    public bool KeogramAvailable { get; set; }
    public bool StartrailsAvailable { get; set; }
}
