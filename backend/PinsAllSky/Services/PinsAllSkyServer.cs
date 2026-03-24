using EmbedIO;
using EmbedIO.Actions;
using EmbedIO.WebApi;
using System.Text;
using NINA.PINS.AllSky.Api;
using Swan.Formatters;

namespace NINA.PINS.AllSky.Services;

public sealed class PinsAllSkyServer : IDisposable
{
    public const int DefaultPort = 19091;

    private WebServer? server;

    public void Start(PinsAllSkyPaths paths)
    {
        Stop();

        server = new WebServer(options => options
                .WithUrlPrefix($"http://*:{DefaultPort}")
                .WithMode(HttpListenerMode.EmbedIO))
            .WithModule(new CorsModule())
            .WithWebApi("/api", ResponseSerializer.Json(JsonSerializerCase.CamelCase), module => module.WithController<PinsAllSkyController>())
            .WithStaticFolder("/media", paths.DataRoot, false);

        server.RunAsync();
    }

    public void Stop()
    {
        server?.Dispose();
        server = null;
    }

    public void Dispose()
    {
        Stop();
    }

    private sealed class CorsModule : WebModuleBase
    {
        public CorsModule() : base("/")
        {
        }

        public override bool IsFinalHandler => false;

        protected override async Task OnRequestAsync(IHttpContext context)
        {
            context.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, OPTIONS");
            context.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization");
            context.Response.Headers.Add("Access-Control-Allow-Origin", "*");

            if (context.Request.HttpVerb == HttpVerbs.Options)
            {
                context.Response.StatusCode = 200;
                await context.SendStringAsync(string.Empty, "text/plain", Encoding.UTF8).ConfigureAwait(false);
            }
        }
    }
}
