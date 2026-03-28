namespace NINA.PINS.AllSky.Models;

public sealed class StartSessionRequest
{
    public string? Label { get; set; }
}

public sealed class StopSessionRequest
{
    public bool GenerateArtifacts { get; set; } = true;
}

public sealed class GenerateArtifactsRequest
{
    public string? SessionId { get; set; }
}

public sealed class DeleteSessionRequest
{
    public string? SessionId { get; set; }
}

public sealed class ApiResponse<T>
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public T? Data { get; init; }

    public static ApiResponse<T> Ok(T? data) => new() { Success = true, Data = data };

    public static ApiResponse<T> Fail(string error) => new() { Success = false, Error = error };
}
