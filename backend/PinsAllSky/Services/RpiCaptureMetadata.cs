using System.Globalization;

namespace NINA.PINS.AllSky.Services;

internal sealed class RpiCaptureMetadata
{
    public int? ExposureTimeMicroseconds { get; init; }
    public double? AnalogueGain { get; init; }

    public static RpiCaptureMetadata ParseTxtFile(string path)
    {
        if (!File.Exists(path))
        {
            return new RpiCaptureMetadata();
        }

        int? exposureTimeMicroseconds = null;
        double? analogueGain = null;

        foreach (var rawLine in File.ReadLines(path))
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0 || separatorIndex == line.Length - 1)
            {
                continue;
            }

            var key = line[..separatorIndex].Trim();
            var value = line[(separatorIndex + 1)..].Trim();

            if (string.Equals(key, "ExposureTime", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var exposureUs))
            {
                exposureTimeMicroseconds = exposureUs;
            }
            else if (string.Equals(key, "AnalogueGain", StringComparison.OrdinalIgnoreCase) &&
                double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var gain))
            {
                analogueGain = gain;
            }
        }

        return new RpiCaptureMetadata
        {
            ExposureTimeMicroseconds = exposureTimeMicroseconds,
            AnalogueGain = analogueGain
        };
    }
}
