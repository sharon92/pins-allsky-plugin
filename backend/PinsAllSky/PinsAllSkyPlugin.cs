using System.ComponentModel.Composition;
using NINA.Core.Utility;
using NINA.Plugin;
using NINA.Plugin.Interfaces;
using NINA.PINS.AllSky.Services;

namespace NINA.PINS.AllSky;

[Export(typeof(IPluginManifest))]
public sealed class PinsAllSkyPlugin : PluginBase
{
    private static readonly Lazy<PinsAllSkyHost> HostFactory = new(() => new PinsAllSkyHost());
    private static bool startAttempted;

    [ImportingConstructor]
    public PinsAllSkyPlugin()
    {
        if (startAttempted)
        {
            return;
        }

        startAttempted = true;

        try
        {
            HostFactory.Value.Start();
        }
        catch (Exception ex)
        {
            Logger.Error($"PINS AllSky failed to start: {ex}");
        }
    }

    internal static PinsAllSkyHost Host => HostFactory.Value;

    public override Task Teardown()
    {
        if (HostFactory.IsValueCreated)
        {
            HostFactory.Value.Dispose();
        }

        return base.Teardown();
    }
}
