namespace NINA.PINS.AllSky.Models;

public sealed class SessionInfo
{
    public string Id { get; set; } = string.Empty;
    public string? Label { get; set; }
    public string StartReason { get; set; } = "manual";
    public string? StopReason { get; set; }
    public bool StartedBySequence { get; set; }
    public bool GenerateRequestedOnStop { get; set; } = true;
    public DateTimeOffset StartedAtUtc { get; set; }
    public DateTimeOffset? EndedAtUtc { get; set; }
    public int CaptureCount { get; set; }
    public int StoredFrameCount { get; set; }
    public DateTimeOffset? LastCaptureAtUtc { get; set; }
    public string? LastFrameRelativePath { get; set; }
    public string? LastError { get; set; }
    public long TotalSizeBytes { get; set; }
    public GeneratedProducts Products { get; set; } = new();
}

public sealed class GeneratedProducts
{
    public ArtifactInfo? Timelapse { get; set; }
    public ArtifactInfo? Keogram { get; set; }
    public ArtifactInfo? Startrails { get; set; }
}

public sealed class ArtifactInfo
{
    public string Name { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public DateTimeOffset GeneratedAtUtc { get; set; }
    public long SizeBytes { get; set; }
}
