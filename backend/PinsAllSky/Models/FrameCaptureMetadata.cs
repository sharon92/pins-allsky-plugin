namespace NINA.PINS.AllSky.Models;

public sealed class FrameCaptureMetadata
{
    public DateTimeOffset CapturedAtUtc { get; set; }
    public int ExposureMicroseconds { get; set; }
    public double AnalogGain { get; set; }
    public double? MeanBrightness { get; set; }
}
