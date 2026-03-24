using System.Text.Json;
using System.Text.Json.Serialization;

namespace NINA.PINS.AllSky.Services;

public static class JsonStorage
{
    public static readonly JsonSerializerOptions DefaultOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    public static T LoadOrDefault<T>(string path, Func<T> factory)
    {
        if (!File.Exists(path))
        {
            return factory();
        }

        using var stream = File.OpenRead(path);
        return JsonSerializer.Deserialize<T>(stream, DefaultOptions) ?? factory();
    }

    public static async Task SaveAsync<T>(string path, T value, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var tempPath = $"{path}.tmp";
        await using (var stream = File.Create(tempPath))
        {
            await JsonSerializer.SerializeAsync(stream, value, DefaultOptions, cancellationToken).ConfigureAwait(false);
        }

        File.Move(tempPath, path, overwrite: true);
    }
}
