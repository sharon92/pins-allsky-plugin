using System.Net.Http.Json;
using System.Text.Json;
using NINA.Core.Utility;
using NINA.PINS.AllSky.Models;

namespace NINA.PINS.AllSky.Services;

public sealed class PinsAllSkyHost : IDisposable
{
    private static readonly HttpClient HttpClient = new();

    private readonly object sync = new();
    private readonly PinsAllSkyPaths paths;
    private readonly ProcessRunner processRunner;
    private readonly PinsAllSkyServer server;

    private PinsAllSkyConfig config = new();
    private SessionInfo? currentSession;
    private Task? captureTask;
    private CancellationTokenSource? captureCts;
    private Task? sequenceMonitorTask;
    private CancellationTokenSource shutdownCts = new();
    private bool disposed;
    private bool generateInProgress;
    private bool sequenceRunning;
    private bool advancedApiReachable;
    private bool suppressAutoStartUntilSequenceStops;
    private string? lastError;
    private StorageStatusInfo? cachedStorageStatus;
    private DateTimeOffset storageStatusCalculatedAtUtc = DateTimeOffset.MinValue;

    public PinsAllSkyHost()
    {
        paths = new PinsAllSkyPaths();
        processRunner = new ProcessRunner();
        server = new PinsAllSkyServer();
    }

    public void Start()
    {
        paths.EnsureBaseDirectories();
        EnsureBundledToolsExecutable();
        RecoverInterruptedSessions();
        config = JsonStorage.LoadOrDefault(paths.ConfigFile, () => new PinsAllSkyConfig());
        NormalizeConfig(config);
        SyncCurrentImageToLatestPersistedFrame();

        try
        {
            _ = EnforceStorageLimitAsync("startup", CancellationToken.None).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Logger.Warning($"PINS AllSky could not enforce the storage limit during startup: {ex.Message}");
        }

        server.Start(paths);
        sequenceMonitorTask = Task.Run(() => SequenceMonitorLoopAsync(shutdownCts.Token));
        Logger.Info("PINS AllSky backend started");
    }

    public PinsAllSkyConfig GetConfig()
    {
        lock (sync)
        {
            return Clone(config);
        }
    }

    public async Task<PinsAllSkyConfig> UpdateConfigAsync(PinsAllSkyConfig updatedConfig, CancellationToken cancellationToken)
    {
        NormalizeConfig(updatedConfig);

        lock (sync)
        {
            config = Clone(updatedConfig);
        }

        await JsonStorage.SaveAsync(paths.ConfigFile, updatedConfig, cancellationToken).ConfigureAwait(false);
        InvalidateStorageStatus();
        _ = await EnforceStorageLimitAsync("config-update", cancellationToken).ConfigureAwait(false);
        Logger.Info("PINS AllSky configuration updated");
        return GetConfig();
    }

    public PinsAllSkyStatus GetStatus()
    {
        SessionInfo? sessionSnapshot;
        bool captureRunning;
        bool generateSnapshot;
        bool sequenceSnapshot;
        bool advancedSnapshot;
        string? errorSnapshot;

        lock (sync)
        {
            sessionSnapshot = currentSession is null ? null : Clone(currentSession);
            if (sessionSnapshot is not null)
            {
                sessionSnapshot.TotalSizeBytes = GetPathSize(paths.GetSessionRoot(sessionSnapshot.Id));
            }

            captureRunning = captureTask is { IsCompleted: false };
            generateSnapshot = generateInProgress;
            sequenceSnapshot = sequenceRunning;
            advancedSnapshot = advancedApiReachable;
            errorSnapshot = lastError;
        }

        return new PinsAllSkyStatus
        {
            BackendPort = PinsAllSkyServer.DefaultPort,
            CaptureRunning = captureRunning,
            GenerateInProgress = generateSnapshot,
            SequenceRunning = sequenceSnapshot,
            AdvancedApiReachable = advancedSnapshot,
            LastError = errorSnapshot,
            CurrentImageUrl = File.Exists(paths.GetCurrentImagePath()) ? "/media/current/latest.jpg" : null,
            CurrentSession = sessionSnapshot,
            RecentSessions = GetRecentSessions(),
            Dependencies = GetDependencyStatus(),
            Storage = GetStorageStatus()
        };
    }

    public async Task<SessionInfo> StartSessionAsync(string? label, string reason, bool startedBySequence, CancellationToken cancellationToken)
    {
        SessionInfo? existing;
        lock (sync)
        {
            existing = captureTask is { IsCompleted: false } && currentSession is not null ? Clone(currentSession) : null;
        }

        if (existing is not null)
        {
            return existing;
        }

        _ = await EnforceStorageLimitAsync("pre-start", cancellationToken).ConfigureAwait(false);

        var session = new SessionInfo
        {
            Id = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss"),
            Label = string.IsNullOrWhiteSpace(label) ? null : label.Trim(),
            StartedAtUtc = DateTimeOffset.UtcNow,
            StartReason = reason,
            StartedBySequence = startedBySequence
        };

        Directory.CreateDirectory(paths.GetSessionRoot(session.Id));
        Directory.CreateDirectory(paths.GetFramesRoot(session.Id));
        Directory.CreateDirectory(paths.GetProductsRoot(session.Id));

        await SaveSessionAsync(session, cancellationToken).ConfigureAwait(false);

        lock (sync)
        {
            currentSession = session;
            captureCts?.Dispose();
            captureCts = CancellationTokenSource.CreateLinkedTokenSource(shutdownCts.Token);
            captureTask = Task.Run(() => CaptureLoopAsync(captureCts.Token));
            lastError = null;
        }

        Logger.Info($"PINS AllSky session '{session.Id}' started ({reason})");
        return Clone(session);
    }

    public async Task<SessionInfo?> StopSessionAsync(bool generateArtifacts, string reason, CancellationToken cancellationToken)
    {
        SessionInfo? session;
        Task? taskToAwait;
        CancellationTokenSource? ctsToCancel;

        lock (sync)
        {
            session = currentSession;
            taskToAwait = captureTask;
            ctsToCancel = captureCts;
        }

        if (session is null)
        {
            return null;
        }

        ctsToCancel?.Cancel();

        if (taskToAwait is not null)
        {
            try
            {
                await taskToAwait.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }

        lock (sync)
        {
            if (currentSession is not null)
            {
                currentSession.EndedAtUtc ??= DateTimeOffset.UtcNow;
                currentSession.StopReason = reason;
                currentSession.GenerateRequestedOnStop = generateArtifacts;
                session = Clone(currentSession);
            }

            currentSession = null;
            captureTask = null;
            captureCts?.Dispose();
            captureCts = null;
        }

        if (session is not null)
        {
            await SaveSessionAsync(session, cancellationToken).ConfigureAwait(false);
            Logger.Info($"PINS AllSky session '{session.Id}' stopped ({reason})");

            lock (sync)
            {
                if (!string.Equals(reason, "sequence-stop", StringComparison.OrdinalIgnoreCase) && sequenceRunning)
                {
                    suppressAutoStartUntilSequenceStops = true;
                }
            }

            if (generateArtifacts)
            {
                _ = Task.Run(() => GenerateArtifactsAsync(session.Id, shutdownCts.Token), shutdownCts.Token);
            }
            else
            {
                _ = await EnforceStorageLimitAsync("session-stop", cancellationToken).ConfigureAwait(false);
            }
        }

        return session;
    }

    public async Task<SessionInfo?> GenerateArtifactsAsync(string? sessionId, CancellationToken cancellationToken)
    {
        SessionInfo? session = null;

        lock (sync)
        {
            if (generateInProgress)
            {
                return currentSession is null ? null : Clone(currentSession);
            }

            generateInProgress = true;
        }

        try
        {
            session = string.IsNullOrWhiteSpace(sessionId)
                ? GetLatestPersistedSession()
                : LoadSession(sessionId);

            if (session is null)
            {
                throw new InvalidOperationException("No session is available for artifact generation.");
            }

            var framesRoot = paths.GetFramesRoot(session.Id);
            if (!Directory.Exists(framesRoot))
            {
                throw new InvalidOperationException($"Session '{session.Id}' does not contain a frames directory.");
            }

            var frameCount = Directory.EnumerateFiles(framesRoot, "*.jpg", SearchOption.TopDirectoryOnly).Count();
            if (frameCount == 0)
            {
                throw new InvalidOperationException($"Session '{session.Id}' does not contain any captured frames.");
            }

            var settings = GetConfig();
            var productsRoot = paths.GetProductsRoot(session.Id);
            Directory.CreateDirectory(productsRoot);

            if (settings.Products.TimelapseEnabled)
            {
                session.Products.Timelapse = await GenerateTimelapseAsync(session.Id, settings, cancellationToken).ConfigureAwait(false);
            }

            if (settings.Products.KeogramEnabled)
            {
                session.Products.Keogram = await GenerateKeogramAsync(session.Id, settings, cancellationToken).ConfigureAwait(false);
            }

            if (settings.Products.StartrailsEnabled)
            {
                session.Products.Startrails = await GenerateStartrailsAsync(session.Id, settings, cancellationToken).ConfigureAwait(false);
            }

            if (!settings.Products.KeepFrames)
            {
                DeleteCapturedFrames(session.Id);
            }

            await SaveSessionAsync(session, cancellationToken).ConfigureAwait(false);
            _ = await EnforceStorageLimitAsync("post-generate", cancellationToken).ConfigureAwait(false);
            Logger.Info($"PINS AllSky artifacts generated for session '{session.Id}'");
            return Clone(session);
        }
        catch (Exception ex)
        {
            SetLastError(ex.Message);
            Logger.Error($"PINS AllSky artifact generation failed: {ex}");
            throw;
        }
        finally
        {
            lock (sync)
            {
                generateInProgress = false;
            }
        }
    }

    public List<SessionInfo> GetRecentSessions()
    {
        return GetPersistedSessions()
            .Take(10)
            .Select(Clone)
            .ToList();
    }

    public async Task<SessionCleanupResult?> DeleteSessionAsync(string? sessionId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return null;
        }

        sessionId = sessionId.Trim();

        lock (sync)
        {
            if (string.Equals(currentSession?.Id, sessionId, StringComparison.Ordinal))
            {
                return new SessionCleanupResult
                {
                    DeletedSessionCount = 0,
                    FreedBytes = 0,
                    Storage = GetStorageStatus(forceRefresh: true)
                };
            }
        }

        var sessionRoot = paths.GetSessionRoot(sessionId);
        if (!Directory.Exists(sessionRoot))
        {
            return null;
        }

        var before = GetStorageStatus(forceRefresh: true);
        DeleteSessionDirectory(sessionId);
        SyncCurrentImageToLatestPersistedFrame();
        InvalidateStorageStatus();
        await Task.CompletedTask.ConfigureAwait(false);

        return new SessionCleanupResult
        {
            DeletedSessionCount = 1,
            DeletedSessionIds = [sessionId],
            FreedBytes = Math.Max(0, before.PluginUsedBytes - GetStorageStatus(forceRefresh: true).PluginUsedBytes),
            Storage = GetStorageStatus(forceRefresh: true)
        };
    }

    public async Task<SessionCleanupResult> DeleteAllSessionsAsync(CancellationToken cancellationToken)
    {
        var activeSessionId = GetActiveSessionId();
        var before = GetStorageStatus(forceRefresh: true);
        var deletedSessionIds = new List<string>();

        foreach (var session in GetPersistedSessions().OrderBy(session => session.StartedAtUtc))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!string.IsNullOrWhiteSpace(activeSessionId) && string.Equals(session.Id, activeSessionId, StringComparison.Ordinal))
            {
                continue;
            }

            DeleteSessionDirectory(session.Id);
            deletedSessionIds.Add(session.Id);
        }

        SyncCurrentImageToLatestPersistedFrame();
        InvalidateStorageStatus();

        var after = GetStorageStatus(forceRefresh: true);

        await Task.CompletedTask.ConfigureAwait(false);
        return new SessionCleanupResult
        {
            DeletedSessionCount = deletedSessionIds.Count,
            DeletedSessionIds = deletedSessionIds,
            FreedBytes = Math.Max(0, before.PluginUsedBytes - after.PluginUsedBytes),
            Storage = after
        };
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        shutdownCts.Cancel();

        try
        {
            captureTask?.GetAwaiter().GetResult();
        }
        catch
        {
        }

        try
        {
            sequenceMonitorTask?.GetAwaiter().GetResult();
        }
        catch
        {
        }

        captureCts?.Dispose();
        shutdownCts.Dispose();
        server.Dispose();
    }

    private async Task CaptureLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var startedAt = DateTimeOffset.UtcNow;
            SessionInfo? session;
            PinsAllSkyConfig settings;

            lock (sync)
            {
                session = currentSession;
                settings = Clone(config);
            }

            if (session is null)
            {
                return;
            }

            try
            {
                var updatedSession = await CaptureFrameAsync(session, settings, cancellationToken).ConfigureAwait(false);

                lock (sync)
                {
                    currentSession = updatedSession;
                }

                await SaveSessionAsync(updatedSession, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                SetLastError(ex.Message);
                Logger.Error($"PINS AllSky capture failed: {ex}");

                lock (sync)
                {
                    if (currentSession is not null)
                    {
                        currentSession.LastError = ex.Message;
                    }
                }
            }

            var interval = TimeSpan.FromSeconds(Math.Max(5, settings.Camera.IntervalSeconds));
            var wait = interval - (DateTimeOffset.UtcNow - startedAt);
            if (wait > TimeSpan.Zero)
            {
                await Task.Delay(wait, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async Task<SessionInfo> CaptureFrameAsync(SessionInfo session, PinsAllSkyConfig settings, CancellationToken cancellationToken)
    {
        var timestamp = DateTimeOffset.UtcNow;
        var frameName = $"frame-{timestamp:yyyyMMddTHHmmssfff}.jpg";
        var tempFramePath = Path.Combine(paths.GetFramesRoot(session.Id), $"{frameName}.tmp");
        var finalFramePath = Path.Combine(paths.GetFramesRoot(session.Id), frameName);

        Directory.CreateDirectory(paths.GetFramesRoot(session.Id));

        if (File.Exists(tempFramePath))
        {
            File.Delete(tempFramePath);
        }

        var arguments = BuildCaptureArguments(settings.Camera, tempFramePath);
        var timeout = TimeSpan.FromSeconds(Math.Max(15, settings.Camera.CaptureTimeoutSeconds));
        var result = await processRunner.RunAsync("/usr/bin/rpicam-still", arguments, paths.GetFramesRoot(session.Id), timeout, cancellationToken).ConfigureAwait(false);

        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(result.StdErr)
                ? "rpicam-still returned a non-zero exit code."
                : result.StdErr);
        }

        if (!File.Exists(tempFramePath))
        {
            throw new FileNotFoundException("rpicam-still completed without creating an output image.", tempFramePath);
        }

        File.Move(tempFramePath, finalFramePath, overwrite: true);
        File.Copy(finalFramePath, paths.GetCurrentImagePath(), overwrite: true);

        var updated = Clone(session);
        updated.CaptureCount += 1;
        updated.LastCaptureAtUtc = timestamp;
        updated.LastFrameRelativePath = paths.GetRelativePath(finalFramePath);
        updated.LastError = null;

        Logger.Info($"PINS AllSky captured frame '{frameName}' for session '{session.Id}'");
        return updated;
    }

    private async Task<ArtifactInfo> GenerateTimelapseAsync(string sessionId, PinsAllSkyConfig settings, CancellationToken cancellationToken)
    {
        var outputPath = Path.Combine(paths.GetProductsRoot(sessionId), "timelapse.mp4");
        var framesPattern = Path.Combine(paths.GetFramesRoot(sessionId), "*.jpg");
        var products = settings.Products;

        var arguments = new List<string>
        {
            "-y",
            "-loglevel",
            products.TimelapseLogLevel,
            "-framerate",
            products.TimelapseFps.ToString(),
            "-pattern_type",
            "glob",
            "-i",
            framesPattern
        };

        if (products.TimelapseWidth > 0 && products.TimelapseHeight > 0)
        {
            arguments.AddRange(
            [
                "-vf",
                $"scale={products.TimelapseWidth}:{products.TimelapseHeight}:force_original_aspect_ratio=decrease,pad={products.TimelapseWidth}:{products.TimelapseHeight}:(ow-iw)/2:(oh-ih)/2:black"
            ]);
        }

        arguments.AddRange(
        [
            "-c:v",
            products.TimelapseCodec,
            "-b:v",
            $"{Math.Max(1000, products.TimelapseBitrateKbps)}k",
            "-pix_fmt",
            products.TimelapsePixelFormat
        ]);

        arguments.AddRange(ArgumentSplitter.Split(products.TimelapseExtraParameters));
        arguments.Add(outputPath);

        var result = await processRunner.RunAsync("/usr/bin/ffmpeg", arguments, paths.GetFramesRoot(sessionId), TimeSpan.FromMinutes(10), cancellationToken).ConfigureAwait(false);
        if (!result.Succeeded || !File.Exists(outputPath))
        {
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(result.StdErr)
                ? "ffmpeg did not produce a timelapse video."
                : result.StdErr);
        }

        return CreateArtifact("Timelapse", outputPath);
    }

    private async Task<ArtifactInfo> GenerateKeogramAsync(string sessionId, PinsAllSkyConfig settings, CancellationToken cancellationToken)
    {
        EnsureToolAvailable(paths.KeogramTool, "keogram");

        var outputPath = Path.Combine(paths.GetProductsRoot(sessionId), "keogram.jpg");
        var products = settings.Products;
        var arguments = new List<string>
        {
            "-d",
            paths.GetFramesRoot(sessionId),
            "-e",
            "jpg",
            "-o",
            outputPath,
            "-N",
            products.KeogramFontName,
            "-C",
            products.KeogramFontColor,
            "-S",
            products.KeogramFontSize.ToString(System.Globalization.CultureInfo.InvariantCulture),
            "-L",
            products.KeogramLineThickness.ToString(),
            "-r",
            products.KeogramRotateDegrees.ToString(System.Globalization.CultureInfo.InvariantCulture)
        };

        if (products.KeogramExpand)
        {
            arguments.Add("-x");
        }

        if (!products.KeogramShowLabels)
        {
            arguments.Add("-n");
        }
        else if (!products.KeogramShowDate)
        {
            arguments.Add("-D");
        }

        arguments.AddRange(ArgumentSplitter.Split(products.KeogramExtraParameters));

        var result = await processRunner.RunAsync(paths.KeogramTool, arguments, paths.GetFramesRoot(sessionId), TimeSpan.FromMinutes(5), cancellationToken).ConfigureAwait(false);
        if (!result.Succeeded || !File.Exists(outputPath))
        {
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(result.StdErr)
                ? "The AllSky keogram tool did not produce an output image."
                : result.StdErr);
        }

        return CreateArtifact("Keogram", outputPath);
    }

    private async Task<ArtifactInfo> GenerateStartrailsAsync(string sessionId, PinsAllSkyConfig settings, CancellationToken cancellationToken)
    {
        EnsureToolAvailable(paths.StartrailsTool, "startrails");

        var outputPath = Path.Combine(paths.GetProductsRoot(sessionId), "startrails.jpg");
        var products = settings.Products;
        var arguments = new List<string>
        {
            "-d",
            paths.GetFramesRoot(sessionId),
            "-e",
            "jpg",
            "-o",
            outputPath,
            "-b",
            products.StartrailsBrightnessThreshold.ToString(System.Globalization.CultureInfo.InvariantCulture)
        };

        arguments.AddRange(ArgumentSplitter.Split(products.StartrailsExtraParameters));

        var result = await processRunner.RunAsync(paths.StartrailsTool, arguments, paths.GetFramesRoot(sessionId), TimeSpan.FromMinutes(5), cancellationToken).ConfigureAwait(false);
        if (!result.Succeeded || !File.Exists(outputPath))
        {
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(result.StdErr)
                ? "The AllSky startrails tool did not produce an output image."
                : result.StdErr);
        }

        return CreateArtifact("Startrails", outputPath);
    }

    private async Task SequenceMonitorLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var settings = GetConfig();

            if (settings.AdvancedApi.Enabled)
            {
                try
                {
                    var probe = await ProbeSequenceStateAsync(settings.AdvancedApi, cancellationToken).ConfigureAwait(false);

                    lock (sync)
                    {
                        advancedApiReachable = probe.Reachable;
                        sequenceRunning = probe.SequenceRunning;
                        if (!probe.SequenceRunning)
                        {
                            suppressAutoStartUntilSequenceStops = false;
                        }
                    }

                    if (settings.AutoStartWithSequence)
                    {
                        if (probe.SequenceRunning && !suppressAutoStartUntilSequenceStops)
                        {
                            _ = StartSessionAsync(null, "sequence-start", true, cancellationToken);
                        }
                        else
                        {
                            SessionInfo? sessionToStop;
                            lock (sync)
                            {
                                sessionToStop = currentSession is { StartedBySequence: true } ? Clone(currentSession) : null;
                            }

                            if (sessionToStop is not null)
                            {
                                _ = StopSessionAsync(true, "sequence-stop", cancellationToken);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    lock (sync)
                    {
                        advancedApiReachable = false;
                        sequenceRunning = false;
                    }

                    Logger.Warning($"PINS AllSky could not query the Advanced API: {ex.Message}");
                }
            }

            var delay = TimeSpan.FromSeconds(Math.Max(5, settings.SequencePollIntervalSeconds));
            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<SequenceProbeResult> ProbeSequenceStateAsync(AdvancedApiSettings settings, CancellationToken cancellationToken)
    {
        var baseUrl = $"{settings.Protocol}://{settings.Host}:{settings.Port}{settings.BasePath.TrimEnd('/')}";
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, settings.RequestTimeoutSeconds)));

        using var response = await HttpClient.GetAsync($"{baseUrl}/sequence/state", timeoutCts.Token).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        await using var content = await response.Content.ReadAsStreamAsync(timeoutCts.Token).ConfigureAwait(false);
        using var document = await JsonDocument.ParseAsync(content, cancellationToken: timeoutCts.Token).ConfigureAwait(false);

        var root = document.RootElement;
        if (!TryGetProperty(root, "success", out var successElement) || !successElement.GetBoolean())
        {
            return new SequenceProbeResult(true, false);
        }

        if (!TryGetProperty(root, "response", out var responseElement) || responseElement.ValueKind != JsonValueKind.Array)
        {
            return new SequenceProbeResult(true, false);
        }

        foreach (var container in responseElement.EnumerateArray())
        {
            if (!TryGetProperty(container, "items", out var items) || items.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var item in items.EnumerateArray())
            {
                if (TryGetProperty(item, "status", out var status) && string.Equals(status.GetString(), "RUNNING", StringComparison.OrdinalIgnoreCase))
                {
                    return new SequenceProbeResult(true, true);
                }
            }
        }

        return new SequenceProbeResult(true, false);
    }

    private IEnumerable<string> BuildCaptureArguments(CameraCaptureSettings settings, string outputPath)
    {
        var arguments = new List<string>
        {
            "-n",
            "--timeout",
            Math.Max(1, settings.WarmupMilliseconds).ToString(),
            "--output",
            outputPath,
            "--encoding",
            "jpg",
            "--quality",
            settings.Quality.ToString(),
            "--width",
            Math.Max(640, settings.Width).ToString(),
            "--height",
            Math.Max(480, settings.Height).ToString(),
            "--metering",
            settings.MeteringMode,
            "--awb",
            settings.AwbMode,
            "--denoise",
            settings.DenoiseMode,
            "--ev",
            settings.EvCompensation.ToString(System.Globalization.CultureInfo.InvariantCulture),
            "--brightness",
            settings.Brightness.ToString(System.Globalization.CultureInfo.InvariantCulture),
            "--contrast",
            settings.Contrast.ToString(System.Globalization.CultureInfo.InvariantCulture),
            "--saturation",
            settings.Saturation.ToString(System.Globalization.CultureInfo.InvariantCulture),
            "--sharpness",
            settings.Sharpness.ToString(System.Globalization.CultureInfo.InvariantCulture)
        };

        if (settings.UseManualExposure)
        {
            arguments.AddRange(["--shutter", Math.Max(1, settings.ShutterMicroseconds).ToString()]);
        }

        if (settings.UseManualGain)
        {
            arguments.AddRange(["--gain", settings.AnalogGain.ToString(System.Globalization.CultureInfo.InvariantCulture)]);
        }

        if (settings.HorizontalFlip)
        {
            arguments.Add("--hflip");
        }

        if (settings.VerticalFlip)
        {
            arguments.Add("--vflip");
        }

        if (settings.Rotation is 0 or 180)
        {
            arguments.AddRange(["--rotation", settings.Rotation.ToString()]);
        }

        arguments.AddRange(ArgumentSplitter.Split(settings.ExtraArguments));
        return arguments;
    }

    private void RecoverInterruptedSessions()
    {
        if (!Directory.Exists(paths.SessionsRoot))
        {
            return;
        }

        foreach (var sessionFile in Directory.EnumerateFiles(paths.SessionsRoot, "session.json", SearchOption.AllDirectories))
        {
            try
            {
                var session = JsonStorage.LoadOrDefault(sessionFile, () => new SessionInfo());
                if (string.IsNullOrWhiteSpace(session.Id) || session.EndedAtUtc is not null)
                {
                    continue;
                }

                session.EndedAtUtc = DateTimeOffset.UtcNow;
                session.StopReason = "recovered-after-restart";
                JsonStorage.SaveAsync(sessionFile, session).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Logger.Warning($"Unable to recover session from '{sessionFile}': {ex.Message}");
            }
        }
    }

    private void EnsureBundledToolsExecutable()
    {
        if (!OperatingSystem.IsLinux())
        {
            return;
        }

        foreach (var tool in new[] { paths.KeogramTool, paths.StartrailsTool })
        {
            if (!File.Exists(tool))
            {
                continue;
            }

            try
            {
                File.SetUnixFileMode(tool, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute | UnixFileMode.GroupRead | UnixFileMode.GroupExecute);
            }
            catch (Exception ex)
            {
                Logger.Warning($"Unable to mark '{tool}' executable: {ex.Message}");
            }
        }
    }

    private DependencyStatus GetDependencyStatus() => new()
    {
        RpiCamStillAvailable = File.Exists("/usr/bin/rpicam-still"),
        FfmpegAvailable = File.Exists("/usr/bin/ffmpeg"),
        KeogramAvailable = File.Exists(paths.KeogramTool),
        StartrailsAvailable = File.Exists(paths.StartrailsTool)
    };

    private SessionInfo? GetLatestPersistedSession() => GetRecentSessions().FirstOrDefault();

    private SessionInfo? LoadSession(string sessionId)
    {
        var sessionFile = paths.GetSessionFile(sessionId);
        return File.Exists(sessionFile) ? JsonStorage.LoadOrDefault(sessionFile, () => new SessionInfo()) : null;
    }

    private static SessionInfo Clone(SessionInfo session)
    {
        var json = JsonSerializer.Serialize(session, JsonStorage.DefaultOptions);
        return JsonSerializer.Deserialize<SessionInfo>(json, JsonStorage.DefaultOptions) ?? new SessionInfo();
    }

    private static PinsAllSkyConfig Clone(PinsAllSkyConfig source)
    {
        var json = JsonSerializer.Serialize(source, JsonStorage.DefaultOptions);
        return JsonSerializer.Deserialize<PinsAllSkyConfig>(json, JsonStorage.DefaultOptions) ?? new PinsAllSkyConfig();
    }

    private static void NormalizeConfig(PinsAllSkyConfig updatedConfig)
    {
        updatedConfig.Camera ??= new CameraCaptureSettings();
        updatedConfig.Products ??= new ProductGenerationSettings();
        updatedConfig.AdvancedApi ??= new AdvancedApiSettings();
        updatedConfig.Storage ??= new StorageManagementSettings();

        updatedConfig.SequencePollIntervalSeconds = Math.Clamp(updatedConfig.SequencePollIntervalSeconds, 5, 300);
        updatedConfig.Camera.IntervalSeconds = Math.Clamp(updatedConfig.Camera.IntervalSeconds, 5, 3600);
        updatedConfig.Camera.CaptureTimeoutSeconds = Math.Clamp(updatedConfig.Camera.CaptureTimeoutSeconds, 15, 300);
        updatedConfig.Camera.Width = Math.Clamp(updatedConfig.Camera.Width, 640, 4056);
        updatedConfig.Camera.Height = Math.Clamp(updatedConfig.Camera.Height, 480, 3040);
        updatedConfig.Camera.Quality = Math.Clamp(updatedConfig.Camera.Quality, 1, 100);
        updatedConfig.Camera.WarmupMilliseconds = Math.Clamp(updatedConfig.Camera.WarmupMilliseconds, 1, 10000);
        updatedConfig.Products.TimelapseFps = Math.Clamp(updatedConfig.Products.TimelapseFps, 1, 60);
        updatedConfig.Products.TimelapseBitrateKbps = Math.Clamp(updatedConfig.Products.TimelapseBitrateKbps, 1000, 50000);
        updatedConfig.Products.TimelapseWidth = Math.Clamp(updatedConfig.Products.TimelapseWidth, 320, 4056);
        updatedConfig.Products.TimelapseHeight = Math.Clamp(updatedConfig.Products.TimelapseHeight, 240, 3040);
        updatedConfig.Products.StartrailsBrightnessThreshold = Math.Clamp(updatedConfig.Products.StartrailsBrightnessThreshold, 0.0, 1.0);
        updatedConfig.AdvancedApi.RequestTimeoutSeconds = Math.Clamp(updatedConfig.AdvancedApi.RequestTimeoutSeconds, 1, 30);
        updatedConfig.AdvancedApi.Port = Math.Clamp(updatedConfig.AdvancedApi.Port, 1, 65535);
        updatedConfig.Storage.MaxUsageGb = Math.Clamp(updatedConfig.Storage.MaxUsageGb, 0.0, 100000.0);
    }

    private static bool TryGetProperty(JsonElement element, string camelCaseName, out JsonElement value)
    {
        if (element.TryGetProperty(camelCaseName, out value))
        {
            return true;
        }

        if (string.IsNullOrEmpty(camelCaseName))
        {
            value = default;
            return false;
        }

        var pascalCase = char.ToUpperInvariant(camelCaseName[0]) + camelCaseName[1..];
        return element.TryGetProperty(pascalCase, out value);
    }

    private void SetLastError(string message)
    {
        lock (sync)
        {
            lastError = message;
        }
    }

    private async Task SaveSessionAsync(SessionInfo session, CancellationToken cancellationToken)
    {
        await JsonStorage.SaveAsync(paths.GetSessionFile(session.Id), session, cancellationToken).ConfigureAwait(false);
        InvalidateStorageStatus();
    }

    private ArtifactInfo CreateArtifact(string name, string outputPath)
    {
        var info = new FileInfo(outputPath);
        return new ArtifactInfo
        {
            Name = name,
            RelativePath = paths.GetRelativePath(outputPath),
            GeneratedAtUtc = DateTimeOffset.UtcNow,
            SizeBytes = info.Exists ? info.Length : 0
        };
    }

    private void EnsureToolAvailable(string path, string name)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Bundled {name} tool is missing at '{path}'.", path);
        }
    }

    private void DeleteCapturedFrames(string sessionId)
    {
        var framesRoot = paths.GetFramesRoot(sessionId);
        if (!Directory.Exists(framesRoot))
        {
            return;
        }

        foreach (var framePath in Directory.EnumerateFiles(framesRoot, "*.jpg", SearchOption.TopDirectoryOnly))
        {
            try
            {
                File.Delete(framePath);
            }
            catch (Exception ex)
            {
                Logger.Warning($"Unable to delete captured frame '{framePath}': {ex.Message}");
            }
        }

        InvalidateStorageStatus();
    }

    private List<SessionInfo> GetPersistedSessions()
    {
        if (!Directory.Exists(paths.SessionsRoot))
        {
            return [];
        }

        var sessions = new List<SessionInfo>();
        foreach (var sessionDirectory in Directory.EnumerateDirectories(paths.SessionsRoot))
        {
            var sessionFile = Path.Combine(sessionDirectory, "session.json");
            if (!File.Exists(sessionFile))
            {
                continue;
            }

            try
            {
                var session = JsonStorage.LoadOrDefault(sessionFile, () => new SessionInfo());
                if (string.IsNullOrWhiteSpace(session.Id))
                {
                    continue;
                }

                session.TotalSizeBytes = GetPathSize(sessionDirectory);
                sessions.Add(session);
            }
            catch (Exception ex)
            {
                Logger.Warning($"Unable to read session file '{sessionFile}': {ex.Message}");
            }
        }

        return sessions
            .OrderByDescending(session => session.StartedAtUtc)
            .ToList();
    }

    private StorageStatusInfo GetStorageStatus(bool forceRefresh = false)
    {
        StorageStatusInfo? cached;

        lock (sync)
        {
            cached = cachedStorageStatus;
            if (!forceRefresh
                && cached is not null
                && DateTimeOffset.UtcNow - storageStatusCalculatedAtUtc < TimeSpan.FromSeconds(15))
            {
                return Clone(cached);
            }
        }

        var snapshot = BuildStorageStatus();

        lock (sync)
        {
            cachedStorageStatus = snapshot;
            storageStatusCalculatedAtUtc = DateTimeOffset.UtcNow;
        }

        return Clone(snapshot);
    }

    private StorageStatusInfo BuildStorageStatus()
    {
        var pluginUsedBytes = GetPathSize(paths.DataRoot);
        var maxPluginUsageBytes = GetConfiguredMaxUsageBytes();
        var driveRoot = Path.GetPathRoot(Path.GetFullPath(paths.DataRoot));
        long diskAvailableBytes = 0;
        long diskTotalBytes = 0;

        try
        {
            var drive = new DriveInfo(string.IsNullOrWhiteSpace(driveRoot) ? paths.DataRoot : driveRoot);
            diskAvailableBytes = drive.AvailableFreeSpace;
            diskTotalBytes = drive.TotalSize;
        }
        catch (Exception ex)
        {
            Logger.Warning($"Unable to read storage information for '{paths.DataRoot}': {ex.Message}");
        }

        return new StorageStatusInfo
        {
            PluginUsedBytes = pluginUsedBytes,
            PluginAvailableBytes = maxPluginUsageBytes is > 0 ? Math.Max(0, maxPluginUsageBytes.Value - pluginUsedBytes) : null,
            MaxPluginUsageBytes = maxPluginUsageBytes,
            DiskAvailableBytes = diskAvailableBytes,
            DiskTotalBytes = diskTotalBytes,
            LimitEnabled = maxPluginUsageBytes is > 0,
            WithinLimit = maxPluginUsageBytes is not > 0 || pluginUsedBytes <= maxPluginUsageBytes.Value
        };
    }

    private long? GetConfiguredMaxUsageBytes()
    {
        double maxUsageGb;

        lock (sync)
        {
            maxUsageGb = config.Storage.MaxUsageGb;
        }

        if (maxUsageGb <= 0)
        {
            return null;
        }

        return (long)Math.Round(maxUsageGb * 1024d * 1024d * 1024d, MidpointRounding.AwayFromZero);
    }

    private async Task<SessionCleanupResult?> EnforceStorageLimitAsync(string reason, CancellationToken cancellationToken)
    {
        var maxUsageBytes = GetConfiguredMaxUsageBytes();
        if (maxUsageBytes is not > 0)
        {
            return null;
        }

        var before = GetStorageStatus(forceRefresh: true);
        if (before.PluginUsedBytes <= maxUsageBytes.Value)
        {
            return null;
        }

        var activeSessionId = GetActiveSessionId();
        var deletedSessionIds = new List<string>();
        var estimatedUsedBytes = before.PluginUsedBytes;

        foreach (var session in GetPersistedSessions().OrderBy(session => session.StartedAtUtc))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!string.IsNullOrWhiteSpace(activeSessionId) && string.Equals(session.Id, activeSessionId, StringComparison.Ordinal))
            {
                continue;
            }

            DeleteSessionDirectory(session.Id);
            deletedSessionIds.Add(session.Id);
            estimatedUsedBytes = Math.Max(0, estimatedUsedBytes - session.TotalSizeBytes);

            if (estimatedUsedBytes <= maxUsageBytes.Value)
            {
                break;
            }
        }

        if (deletedSessionIds.Count == 0)
        {
            return null;
        }

        SyncCurrentImageToLatestPersistedFrame();
        InvalidateStorageStatus();

        var after = GetStorageStatus(forceRefresh: true);
        var result = new SessionCleanupResult
        {
            DeletedSessionCount = deletedSessionIds.Count,
            DeletedSessionIds = deletedSessionIds,
            FreedBytes = Math.Max(0, before.PluginUsedBytes - after.PluginUsedBytes),
            Storage = after
        };

        Logger.Info($"PINS AllSky removed {result.DeletedSessionCount} session(s) during '{reason}' storage cleanup and freed {result.FreedBytes} bytes.");
        await Task.CompletedTask.ConfigureAwait(false);
        return result;
    }

    private string? GetActiveSessionId()
    {
        lock (sync)
        {
            return captureTask is { IsCompleted: false } ? currentSession?.Id : null;
        }
    }

    private void SyncCurrentImageToLatestPersistedFrame()
    {
        var activeSessionId = GetActiveSessionId();
        if (!string.IsNullOrWhiteSpace(activeSessionId))
        {
            return;
        }

        var currentImagePath = paths.GetCurrentImagePath();
        var latestSession = GetPersistedSessions().FirstOrDefault(session => !string.IsNullOrWhiteSpace(session.LastFrameRelativePath));
        if (latestSession is null || string.IsNullOrWhiteSpace(latestSession.LastFrameRelativePath))
        {
            TryDeleteFile(currentImagePath);
            return;
        }

        var relativePath = latestSession.LastFrameRelativePath.Replace('/', Path.DirectorySeparatorChar);
        var sourcePath = Path.Combine(paths.DataRoot, relativePath);
        if (!File.Exists(sourcePath))
        {
            TryDeleteFile(currentImagePath);
            return;
        }

        Directory.CreateDirectory(paths.CurrentRoot);
        File.Copy(sourcePath, currentImagePath, overwrite: true);
    }

    private void DeleteSessionDirectory(string sessionId)
    {
        var sessionRoot = paths.GetSessionRoot(sessionId);
        if (!Directory.Exists(sessionRoot))
        {
            return;
        }

        Directory.Delete(sessionRoot, recursive: true);
        Logger.Info($"PINS AllSky deleted session '{sessionId}'.");
    }

    private void InvalidateStorageStatus()
    {
        lock (sync)
        {
            cachedStorageStatus = null;
            storageStatusCalculatedAtUtc = DateTimeOffset.MinValue;
        }
    }

    private static StorageStatusInfo Clone(StorageStatusInfo source) => new()
    {
        PluginUsedBytes = source.PluginUsedBytes,
        PluginAvailableBytes = source.PluginAvailableBytes,
        MaxPluginUsageBytes = source.MaxPluginUsageBytes,
        DiskAvailableBytes = source.DiskAvailableBytes,
        DiskTotalBytes = source.DiskTotalBytes,
        LimitEnabled = source.LimitEnabled,
        WithinLimit = source.WithinLimit
    };

    private static long GetPathSize(string path)
    {
        if (File.Exists(path))
        {
            return new FileInfo(path).Length;
        }

        if (!Directory.Exists(path))
        {
            return 0;
        }

        long total = 0;
        foreach (var filePath in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
        {
            try
            {
                total += new FileInfo(filePath).Length;
            }
            catch
            {
            }
        }

        return total;
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
        }
    }

    private sealed record SequenceProbeResult(bool Reachable, bool SequenceRunning);
}
