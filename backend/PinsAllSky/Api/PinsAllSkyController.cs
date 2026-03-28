using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using NINA.PINS.AllSky.Models;
using NINA.PINS.AllSky.Services;
using Swan.Formatters;
using System.Text.Json;

namespace NINA.PINS.AllSky.Api;

public sealed class PinsAllSkyController : WebApiController
{
    private static readonly JsonSerializerOptions RequestOptions = new(JsonStorage.DefaultOptions)
    {
        PropertyNameCaseInsensitive = true
    };

    private static async Task<T> ReadCamelCaseRequestAsync<T>(IHttpContext context)
    {
        var body = await context.GetRequestBodyAsStringAsync().ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(body))
        {
            return Activator.CreateInstance<T>();
        }

        return JsonSerializer.Deserialize<T>(body, RequestOptions) ?? Activator.CreateInstance<T>();
    }

    [Route(HttpVerbs.Get, "/status")]
    public ApiResponse<PinsAllSkyStatus> GetStatus()
    {
        return ApiResponse<PinsAllSkyStatus>.Ok(PinsAllSkyPlugin.Host.GetStatus());
    }

    [Route(HttpVerbs.Get, "/config")]
    public ApiResponse<PinsAllSkyConfig> GetConfig()
    {
        return ApiResponse<PinsAllSkyConfig>.Ok(PinsAllSkyPlugin.Host.GetConfig());
    }

    [Route(HttpVerbs.Put, "/config")]
    public async Task<ApiResponse<PinsAllSkyConfig>> UpdateConfig()
    {
        var request = await ReadCamelCaseRequestAsync<PinsAllSkyConfig>(HttpContext).ConfigureAwait(false);
        var updated = await PinsAllSkyPlugin.Host.UpdateConfigAsync(request, HttpContext.CancellationToken).ConfigureAwait(false);
        return ApiResponse<PinsAllSkyConfig>.Ok(updated);
    }

    [Route(HttpVerbs.Get, "/sessions")]
    public ApiResponse<List<SessionInfo>> GetSessions()
    {
        return ApiResponse<List<SessionInfo>>.Ok(PinsAllSkyPlugin.Host.GetRecentSessions());
    }

    [Route(HttpVerbs.Post, "/session/delete")]
    public async Task<ApiResponse<SessionCleanupResult>> DeleteSession()
    {
        var request = await ReadCamelCaseRequestAsync<DeleteSessionRequest>(HttpContext).ConfigureAwait(false);
        var result = await PinsAllSkyPlugin.Host.DeleteSessionAsync(request.SessionId, HttpContext.CancellationToken).ConfigureAwait(false);
        return result is null
            ? ApiResponse<SessionCleanupResult>.Fail("The requested session does not exist.")
            : ApiResponse<SessionCleanupResult>.Ok(result);
    }

    [Route(HttpVerbs.Post, "/sessions/delete-all")]
    public async Task<ApiResponse<SessionCleanupResult>> DeleteAllSessions()
    {
        var result = await PinsAllSkyPlugin.Host.DeleteAllSessionsAsync(HttpContext.CancellationToken).ConfigureAwait(false);
        return ApiResponse<SessionCleanupResult>.Ok(result);
    }

    [Route(HttpVerbs.Post, "/session/start")]
    public async Task<ApiResponse<SessionInfo>> StartSession()
    {
        var request = await ReadCamelCaseRequestAsync<StartSessionRequest>(HttpContext).ConfigureAwait(false);
        var session = await PinsAllSkyPlugin.Host.StartSessionAsync(request.Label, "manual-start", false, HttpContext.CancellationToken).ConfigureAwait(false);
        return ApiResponse<SessionInfo>.Ok(session);
    }

    [Route(HttpVerbs.Post, "/session/stop")]
    public async Task<ApiResponse<SessionInfo>> StopSession()
    {
        var request = await ReadCamelCaseRequestAsync<StopSessionRequest>(HttpContext).ConfigureAwait(false);
        var session = await PinsAllSkyPlugin.Host.StopSessionAsync(request.GenerateArtifacts, "manual-stop", HttpContext.CancellationToken).ConfigureAwait(false);
        return session is null
            ? ApiResponse<SessionInfo>.Fail("No active session is running.")
            : ApiResponse<SessionInfo>.Ok(session);
    }

    [Route(HttpVerbs.Post, "/session/generate")]
    public async Task<ApiResponse<SessionInfo>> GenerateArtifacts()
    {
        var request = await ReadCamelCaseRequestAsync<GenerateArtifactsRequest>(HttpContext).ConfigureAwait(false);
        var session = await PinsAllSkyPlugin.Host.GenerateArtifactsAsync(request.SessionId, HttpContext.CancellationToken).ConfigureAwait(false);
        return session is null
            ? ApiResponse<SessionInfo>.Fail("No session is available for artifact generation.")
            : ApiResponse<SessionInfo>.Ok(session);
    }
}
