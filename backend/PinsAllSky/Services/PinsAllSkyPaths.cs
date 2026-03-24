using System.Reflection;

namespace NINA.PINS.AllSky.Services;

public sealed class PinsAllSkyPaths
{
    public PinsAllSkyPaths()
    {
        var assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
            ?? throw new InvalidOperationException("Unable to determine plugin directory.");

        PluginRoot = assemblyDirectory;

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        DataRoot = Path.Combine(home, ".local", "share", "NINA", "PluginData", "PinsAllSky");
        SessionsRoot = Path.Combine(DataRoot, "sessions");
        CurrentRoot = Path.Combine(DataRoot, "current");
        ConfigFile = Path.Combine(DataRoot, "config.json");
        ToolsRoot = Path.Combine(PluginRoot, "tools");
        KeogramTool = Path.Combine(ToolsRoot, "keogram");
        StartrailsTool = Path.Combine(ToolsRoot, "startrails");
    }

    public string PluginRoot { get; }
    public string DataRoot { get; }
    public string SessionsRoot { get; }
    public string CurrentRoot { get; }
    public string ConfigFile { get; }
    public string ToolsRoot { get; }
    public string KeogramTool { get; }
    public string StartrailsTool { get; }

    public string GetSessionRoot(string sessionId) => Path.Combine(SessionsRoot, sessionId);

    public string GetFramesRoot(string sessionId) => Path.Combine(GetSessionRoot(sessionId), "frames");

    public string GetProductsRoot(string sessionId) => Path.Combine(GetSessionRoot(sessionId), "products");

    public string GetSessionFile(string sessionId) => Path.Combine(GetSessionRoot(sessionId), "session.json");

    public string GetCurrentImagePath() => Path.Combine(CurrentRoot, "latest.jpg");

    public string GetRelativePath(string path) => Path.GetRelativePath(DataRoot, path).Replace('\\', '/');

    public void EnsureBaseDirectories()
    {
        Directory.CreateDirectory(DataRoot);
        Directory.CreateDirectory(SessionsRoot);
        Directory.CreateDirectory(CurrentRoot);
    }
}
