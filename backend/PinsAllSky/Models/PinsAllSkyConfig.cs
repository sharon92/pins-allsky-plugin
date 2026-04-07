namespace NINA.PINS.AllSky.Models;

public sealed class PinsAllSkyConfig
{
    public bool AutoStartWithSequence { get; set; } = true;
    public int SequencePollIntervalSeconds { get; set; } = 10;
    public CameraCaptureSettings Camera { get; set; } = new();
    public ProductGenerationSettings Products { get; set; } = new();
    public AdvancedApiSettings AdvancedApi { get; set; } = new();
    public StorageManagementSettings Storage { get; set; } = new();
}

public sealed class CameraCaptureSettings
{
    public int IntervalSeconds { get; set; } = 60;
    public int Width { get; set; } = 2028;
    public int Height { get; set; } = 1520;
    public int Quality { get; set; } = 93;
    public int WarmupMilliseconds { get; set; } = 3000;
    public int CaptureTimeoutSeconds { get; set; } = 45;
    public bool UseManualExposure { get; set; }
    public int ShutterMicroseconds { get; set; } = 10000;
    public bool UseManualGain { get; set; }
    public double AnalogGain { get; set; } = 1.0;
    public int AutoMaxExposureMicroseconds { get; set; } = 60_000_000;
    public double AutoMaxGain { get; set; } = 16.0;
    public double AutoMeanTarget { get; set; } = 0.2;
    public double AutoMeanThreshold { get; set; } = 0.1;
    public double AutoMeanP0 { get; set; } = 5.0;
    public double AutoMeanP1 { get; set; } = 20.0;
    public double AutoMeanP2 { get; set; } = 45.0;
    public string MeteringMode { get; set; } = "centre";
    public string AwbMode { get; set; } = "auto";
    public string DenoiseMode { get; set; } = "auto";
    public double EvCompensation { get; set; }
    public double Brightness { get; set; }
    public double Contrast { get; set; } = 1.0;
    public double Saturation { get; set; } = 1.0;
    public double Sharpness { get; set; } = 1.0;
    public int Rotation { get; set; }
    public bool HorizontalFlip { get; set; }
    public bool VerticalFlip { get; set; }
    public string ExtraArguments { get; set; } = string.Empty;
}

public sealed class ProductGenerationSettings
{
    public bool KeepFrames { get; set; } = true;

    public bool TimelapseEnabled { get; set; } = true;
    public bool TimelapseOverlayTimestamp { get; set; }
    public bool TimelapseOverlayExposureGain { get; set; }
    public int TimelapseFps { get; set; } = 20;
    public int TimelapseBitrateKbps { get; set; } = 4000;
    public int TimelapseWidth { get; set; } = 1920;
    public int TimelapseHeight { get; set; } = 1080;
    public string TimelapseCodec { get; set; } = "libx264";
    public string TimelapsePixelFormat { get; set; } = "yuv420p";
    public string TimelapseLogLevel { get; set; } = "warning";
    public string TimelapseExtraParameters { get; set; } = string.Empty;

    public bool KeogramEnabled { get; set; } = true;
    public bool KeogramExpand { get; set; } = true;
    public bool KeogramShowLabels { get; set; } = true;
    public bool KeogramShowDate { get; set; } = true;
    public string KeogramFontName { get; set; } = "simplex";
    public string KeogramFontColor { get; set; } = "#ffffff";
    public double KeogramFontSize { get; set; } = 2.0;
    public int KeogramLineThickness { get; set; } = 3;
    public double KeogramRotateDegrees { get; set; }
    public string KeogramExtraParameters { get; set; } = string.Empty;

    public bool StartrailsEnabled { get; set; } = true;
    public double StartrailsBrightnessThreshold { get; set; } = 0.35;
    public string StartrailsExtraParameters { get; set; } = string.Empty;
}

public sealed class AdvancedApiSettings
{
    public bool Enabled { get; set; } = true;
    public string Protocol { get; set; } = "http";
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 1888;
    public string BasePath { get; set; } = "/v2/api";
    public int RequestTimeoutSeconds { get; set; } = 5;
}

public sealed class StorageManagementSettings
{
    public double MaxUsageGb { get; set; }
}
