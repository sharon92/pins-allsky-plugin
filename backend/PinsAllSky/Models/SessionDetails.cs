namespace NINA.PINS.AllSky.Models;

public sealed class SessionDetailsInfo
{
    public SessionInfo Session { get; set; } = new();
    public List<ArtifactInfo> Artifacts { get; set; } = [];
    public List<FrameInfo> Frames { get; set; } = [];
}

public sealed class FrameInfo
{
    public string Name { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public DateTimeOffset? CapturedAtUtc { get; set; }
    public long SizeBytes { get; set; }
}

public sealed class EstimateBaselineInfo
{
    public string SessionId { get; set; } = string.Empty;
    public string? Label { get; set; }
    public int StoredFrameCount { get; set; }
    public long SourceSessionBytes { get; set; }
    public long AverageFrameBytes { get; set; }
    public long TimelapseBytes { get; set; }
    public long KeogramBytes { get; set; }
    public long StartrailsBytes { get; set; }
}
