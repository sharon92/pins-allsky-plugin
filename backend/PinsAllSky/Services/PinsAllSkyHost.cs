using System.Net.Http.Json;
using System.Text.Json;
using System.Text;
using System.Globalization;
using System.Diagnostics;
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
    private AllSkyAutoExposureController autoExposureController = new();
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
        List<SessionInfo> persistedSessions;

        lock (sync)
        {
            sessionSnapshot = currentSession is null ? null : Clone(currentSession);
            captureRunning = captureTask is { IsCompleted: false };
            generateSnapshot = generateInProgress;
            sequenceSnapshot = sequenceRunning;
            advancedSnapshot = advancedApiReachable;
            errorSnapshot = lastError;
        }

        if (sessionSnapshot is not null)
        {
            PopulateSessionDerivedFields(sessionSnapshot, paths.GetSessionRoot(sessionSnapshot.Id));
        }

        persistedSessions = GetPersistedSessions();

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
            RecentSessions = persistedSessions.Take(10).Select(Clone).ToList(),
            Dependencies = GetDependencyStatus(),
            Storage = GetStorageStatus(),
            EstimateBaseline = BuildEstimateBaselineInfo(persistedSessions)
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
            autoExposureController.Reset();
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
            autoExposureController.Reset();
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

    public BackendUpdateResult TriggerBackendUpdate()
    {
        bool captureRunning;
        bool renderRunning;

        lock (sync)
        {
            captureRunning = captureTask is { IsCompleted: false };
            renderRunning = generateInProgress;
        }

        if (captureRunning || renderRunning)
        {
            throw new InvalidOperationException("Stop capture and rendering before updating the backend.");
        }

        var scriptPath = Path.Combine(paths.PluginRoot, "scripts", "update-backend-plugin.sh");
        if (!File.Exists(scriptPath))
        {
            throw new InvalidOperationException("The packaged backend updater script is not available in this plugin install.");
        }

        Directory.CreateDirectory(paths.UpdatesRoot);
        var logPath = Path.Combine(paths.UpdatesRoot, $"backend-update-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.log");
        var repoDirectory = ResolvePreferredRepoDirectory();
        var command = $"nohup {ShellQuote(scriptPath)} > {ShellQuote(logPath)} 2>&1 &";

        using var process = new Process();
        process.StartInfo.FileName = "/bin/bash";
        process.StartInfo.WorkingDirectory = paths.PluginRoot;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.RedirectStandardOutput = false;
        process.StartInfo.RedirectStandardError = false;
        process.StartInfo.ArgumentList.Add("-lc");
        process.StartInfo.ArgumentList.Add(command);

        if (!string.IsNullOrWhiteSpace(repoDirectory))
        {
            process.StartInfo.Environment["PINS_ALLSKY_REPO_DIR"] = repoDirectory;
        }

        if (!process.Start())
        {
            throw new InvalidOperationException("Unable to launch the backend updater.");
        }

        Logger.Info($"PINS AllSky backend update triggered. Log: {logPath}");

        return new BackendUpdateResult
        {
            Message = $"Backend update started. PINS will restart when installation finishes. Log: {logPath}",
            LogPath = logPath,
            RepoDirectory = repoDirectory
        };
    }

    public List<SessionInfo> GetRecentSessions()
    {
        return GetPersistedSessions()
            .Take(10)
            .Select(Clone)
            .ToList();
    }

    public SessionDetailsInfo? GetSessionDetails(string? sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return null;
        }

        var session = LoadSession(sessionId.Trim());
        if (session is null)
        {
            return null;
        }

        PopulateSessionDerivedFields(session, paths.GetSessionRoot(session.Id));

        return new SessionDetailsInfo
        {
            Session = Clone(session),
            Artifacts = GetSessionArtifacts(session),
            Frames = GetFrameEntries(session.Id)
        };
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

    public async Task<FileCleanupResult?> DeleteArtifactAsync(string? sessionId, string? relativePath, CancellationToken cancellationToken)
    {
        return await DeleteSessionFileAsync(sessionId, relativePath, "products", true, cancellationToken).ConfigureAwait(false);
    }

    public async Task<FileCleanupResult?> DeleteFrameAsync(string? sessionId, string? relativePath, CancellationToken cancellationToken)
    {
        return await DeleteSessionFileAsync(sessionId, relativePath, "frames", false, cancellationToken).ConfigureAwait(false);
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
        var tempMetadataPath = Path.Combine(paths.GetFramesRoot(session.Id), $"{frameName}.metadata.txt");
        var finalFramePath = Path.Combine(paths.GetFramesRoot(session.Id), frameName);

        Directory.CreateDirectory(paths.GetFramesRoot(session.Id));

        if (File.Exists(tempFramePath))
        {
            File.Delete(tempFramePath);
        }

        if (File.Exists(tempMetadataPath))
        {
            File.Delete(tempMetadataPath);
        }

        var captureSettings = BuildCaptureSettings(settings.Camera);
        var arguments = BuildCaptureArguments(settings.Camera, captureSettings, tempFramePath, tempMetadataPath);
        var timeout = ResolveCaptureProcessTimeout(settings.Camera, captureSettings);
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

        var metadata = RpiCaptureMetadata.ParseTxtFile(tempMetadataPath);
        var observedExposureMicroseconds = ResolveObservedExposureMicroseconds(settings.Camera, captureSettings, metadata);
        var observedAnalogGain = ResolveObservedAnalogGain(settings.Camera, captureSettings, metadata);
        var observedMean = ImageMeanAnalyzer.CalculateNormalizedMean(finalFramePath);
        TryDeleteFile(tempMetadataPath);

        await SaveFrameMetadataAsync(
            finalFramePath,
            new FrameCaptureMetadata
            {
                CapturedAtUtc = timestamp,
                ExposureMicroseconds = observedExposureMicroseconds,
                AnalogGain = observedAnalogGain,
                MeanBrightness = observedMean
            },
            cancellationToken).ConfigureAwait(false);

        if (AllSkyAutoExposureController.IsEnabled(settings.Camera))
        {
            autoExposureController.Observe(settings.Camera, observedExposureMicroseconds, observedAnalogGain, observedMean);
        }
        else
        {
            autoExposureController.Reset();
        }

        var updated = Clone(session);
        updated.CaptureCount += 1;
        updated.LastCaptureAtUtc = timestamp;
        updated.LastFrameRelativePath = paths.GetRelativePath(finalFramePath);
        updated.LastExposureMicroseconds = observedExposureMicroseconds;
        updated.LastAnalogGain = observedAnalogGain;
        updated.LastMeanBrightness = observedMean;
        updated.LastError = null;
        SetLastError(null);

        Logger.Info($"PINS AllSky captured frame '{frameName}' for session '{session.Id}'");
        return updated;
    }

    private async Task<ArtifactInfo> GenerateTimelapseAsync(string sessionId, PinsAllSkyConfig settings, CancellationToken cancellationToken)
    {
        var outputPath = Path.Combine(paths.GetProductsRoot(sessionId), "timelapse.mp4");
        var framesPattern = Path.Combine(paths.GetFramesRoot(sessionId), "*.jpg");
        var products = settings.Products;
        var overlayScriptPath = await CreateTimelapseOverlayScriptAsync(sessionId, settings, cancellationToken).ConfigureAwait(false);

        try
        {
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

            var filter = BuildTimelapseVideoFilter(settings, overlayScriptPath);
            if (!string.IsNullOrWhiteSpace(filter))
            {
                arguments.AddRange(["-vf", filter]);
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
        finally
        {
            if (!string.IsNullOrWhiteSpace(overlayScriptPath))
            {
                TryDeleteFile(overlayScriptPath);
            }
        }
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

    private CaptureSettingsPlan BuildCaptureSettings(CameraCaptureSettings settings)
    {
        var managedAutoEnabled = AllSkyAutoExposureController.IsEnabled(settings);
        if (!managedAutoEnabled)
        {
            return new CaptureSettingsPlan(
                UseManualExposure: settings.UseManualExposure,
                ExposureMicroseconds: settings.UseManualExposure ? Math.Max(1, settings.ShutterMicroseconds) : null,
                UseManualGain: settings.UseManualGain,
                AnalogGain: settings.UseManualGain ? Math.Max(1.0, settings.AnalogGain) : null,
                WriteMetadata: true);
        }

        if (!autoExposureController.IsInitialized)
        {
            return new CaptureSettingsPlan(
                UseManualExposure: settings.UseManualExposure,
                ExposureMicroseconds: settings.UseManualExposure ? Math.Max(1, settings.ShutterMicroseconds) : null,
                UseManualGain: settings.UseManualGain,
                AnalogGain: settings.UseManualGain ? Math.Max(1.0, settings.AnalogGain) : null,
                WriteMetadata: true);
        }

        var mode = AllSkyAutoExposureController.ResolveMode(settings);
        var exposureMicroseconds = mode == AllSkyAutoMode.GainOnly
            ? Math.Max(1, settings.ShutterMicroseconds)
            : autoExposureController.CurrentExposureMicroseconds;
        var analogGain = mode == AllSkyAutoMode.ExposureOnly
            ? Math.Max(1.0, settings.AnalogGain)
            : autoExposureController.CurrentAnalogGain;

        return new CaptureSettingsPlan(
            UseManualExposure: mode != AllSkyAutoMode.Off,
            ExposureMicroseconds: exposureMicroseconds,
            UseManualGain: mode != AllSkyAutoMode.Off,
            AnalogGain: analogGain,
            WriteMetadata: true);
    }

    private static TimeSpan ResolveCaptureProcessTimeout(CameraCaptureSettings settings, CaptureSettingsPlan captureSettings)
    {
        var configuredSeconds = Math.Max(15, settings.CaptureTimeoutSeconds);
        var warmupSeconds = Math.Ceiling(Math.Max(1, settings.WarmupMilliseconds) / 1000d);
        var plannedExposureSeconds = captureSettings.UseManualExposure && captureSettings.ExposureMicroseconds.HasValue
            ? Math.Ceiling(Math.Max(1, captureSettings.ExposureMicroseconds.Value) / 1_000_000d)
            : 0d;

        if (plannedExposureSeconds <= 0 && AllSkyAutoExposureController.IsEnabled(settings))
        {
            plannedExposureSeconds = Math.Ceiling(Math.Max(1, settings.AutoMaxExposureMicroseconds) / 1_000_000d);
        }

        // Allow enough headroom for long night exposures, camera startup, and metadata/file I/O.
        var requiredSeconds = warmupSeconds + plannedExposureSeconds + 15d;
        return TimeSpan.FromSeconds(Math.Clamp(Math.Max(configuredSeconds, requiredSeconds), 15d, 900d));
    }

    private string? BuildTimelapseVideoFilter(PinsAllSkyConfig settings, string? overlayScriptPath)
    {
        var products = settings.Products;
        var filters = new List<string>();

        if (products.TimelapseWidth > 0 && products.TimelapseHeight > 0)
        {
            filters.Add(
                $"scale={products.TimelapseWidth}:{products.TimelapseHeight}:force_original_aspect_ratio=decrease");
            filters.Add(
                $"pad={products.TimelapseWidth}:{products.TimelapseHeight}:(ow-iw)/2:(oh-ih)/2:black");
        }

        if (!string.IsNullOrWhiteSpace(overlayScriptPath))
        {
            filters.Add(
                $"subtitles={EscapeFfmpegSubtitlePath(overlayScriptPath)}:force_style='Fontname=DejaVu Sans,FontSize=18,PrimaryColour=&H00FFFFFF,OutlineColour=&H80000000,BorderStyle=1,Outline=2,Shadow=0,Alignment=2,MarginL=24,MarginR=24,MarginV=24'");
        }

        return filters.Count == 0 ? null : string.Join(",", filters);
    }

    private async Task<string?> CreateTimelapseOverlayScriptAsync(string sessionId, PinsAllSkyConfig settings, CancellationToken cancellationToken)
    {
        var products = settings.Products;
        if (!products.TimelapseOverlayTimestamp && !products.TimelapseOverlayExposureGain)
        {
            return null;
        }

        var frames = GetFrameEntries(sessionId)
            .OrderBy(frame => frame.CapturedAtUtc ?? DateTimeOffset.MinValue)
            .ThenBy(frame => frame.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (frames.Count == 0)
        {
            return null;
        }

        var frameDurationSeconds = 1d / Math.Max(1, products.TimelapseFps);
        var renderWidth = products.TimelapseWidth > 0 ? products.TimelapseWidth : Math.Max(640, settings.Camera.Width);
        var renderHeight = products.TimelapseHeight > 0 ? products.TimelapseHeight : Math.Max(480, settings.Camera.Height);
        var lines = new List<string>
        {
            "[Script Info]",
            "ScriptType: v4.00+",
            $"PlayResX: {renderWidth}",
            $"PlayResY: {renderHeight}",
            string.Empty,
            "[V4+ Styles]",
            "Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding",
            "Style: Default,DejaVu Sans,20,&H00FFFFFF,&H00FFFFFF,&H80000000,&H40000000,-1,0,0,0,100,100,0,0,1,2,0,2,24,24,24,1",
            string.Empty,
            "[Events]",
            "Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text"
        };

        for (var index = 0; index < frames.Count; index += 1)
        {
            var overlayText = BuildTimelapseOverlayText(frames[index], products);
            if (string.IsNullOrWhiteSpace(overlayText))
            {
                continue;
            }

            var start = FormatAssTimestamp(TimeSpan.FromSeconds(index * frameDurationSeconds));
            var end = FormatAssTimestamp(TimeSpan.FromSeconds((index + 1) * frameDurationSeconds));
            lines.Add($"Dialogue: 0,{start},{end},Default,,0,0,0,,{EscapeAssText(overlayText)}");
        }

        if (lines.Count == 11)
        {
            return null;
        }

        var overlayScriptPath = Path.Combine(paths.GetFramesRoot(sessionId), "timelapse-overlay.ass");
        await File.WriteAllLinesAsync(overlayScriptPath, lines, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
        return overlayScriptPath;
    }

    private static string BuildTimelapseOverlayText(FrameInfo frame, ProductGenerationSettings products)
    {
        var lines = new List<string>();

        if (products.TimelapseOverlayTimestamp && frame.CapturedAtUtc is DateTimeOffset capturedAtUtc)
        {
            lines.Add(capturedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
        }

        if (products.TimelapseOverlayExposureGain)
        {
            var exposureLabel = frame.ExposureMicroseconds is int exposureMicroseconds && exposureMicroseconds > 0
                ? $"Exp {FormatExposureValue(exposureMicroseconds)}"
                : null;
            var gainLabel = frame.AnalogGain is double analogGain && analogGain > 0
                ? $"Gain {FormatGainValue(analogGain)}"
                : null;

            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(exposureLabel))
            {
                parts.Add(exposureLabel);
            }

            if (!string.IsNullOrWhiteSpace(gainLabel))
            {
                parts.Add(gainLabel);
            }

            if (parts.Count > 0)
            {
                lines.Add(string.Join("  ", parts));
            }
        }

        return string.Join("\\N", lines);
    }

    private static string FormatExposureValue(int shutterMicroseconds)
    {
        if (shutterMicroseconds <= 0)
        {
            return "n/a";
        }

        var exposureSeconds = shutterMicroseconds / 1_000_000d;
        if (exposureSeconds >= 1)
        {
            return exposureSeconds.ToString(exposureSeconds >= 10 ? "0" : "0.0", CultureInfo.InvariantCulture) + "s";
        }

        return exposureSeconds.ToString(exposureSeconds >= 0.1 ? "0.00" : "0.000", CultureInfo.InvariantCulture) + "s";
    }

    private static string FormatGainValue(double gain)
    {
        if (gain <= 0)
        {
            return "n/a";
        }

        return gain >= 10
            ? gain.ToString("0", CultureInfo.InvariantCulture)
            : gain.ToString("0.0", CultureInfo.InvariantCulture).TrimEnd('0').TrimEnd('.');
    }

    private static string FormatAssTimestamp(TimeSpan value)
    {
        if (value < TimeSpan.Zero)
        {
            value = TimeSpan.Zero;
        }

        var totalHours = (int)value.TotalHours;
        return $"{totalHours}:{value.Minutes:00}:{value.Seconds:00}.{value.Milliseconds / 10:00}";
    }

    private static string EscapeAssText(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("{", "\\{", StringComparison.Ordinal)
            .Replace("}", "\\}", StringComparison.Ordinal);
    }

    private static string EscapeFfmpegSubtitlePath(string path)
    {
        return path
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace(":", "\\:", StringComparison.Ordinal)
            .Replace("'", "\\'", StringComparison.Ordinal);
    }

    private static string GetFrameMetadataPath(string framePath)
    {
        return Path.ChangeExtension(framePath, ".json");
    }

    private async Task SaveFrameMetadataAsync(string framePath, FrameCaptureMetadata metadata, CancellationToken cancellationToken)
    {
        await JsonStorage.SaveAsync(GetFrameMetadataPath(framePath), metadata, cancellationToken).ConfigureAwait(false);
    }

    private static FrameCaptureMetadata? LoadFrameMetadata(string framePath)
    {
        var metadataPath = GetFrameMetadataPath(framePath);
        if (!File.Exists(metadataPath))
        {
            return null;
        }

        try
        {
            return JsonStorage.LoadOrDefault<FrameCaptureMetadata?>(metadataPath, static () => null);
        }
        catch
        {
            return null;
        }
    }

    private IEnumerable<string> BuildCaptureArguments(CameraCaptureSettings settings, CaptureSettingsPlan captureSettings, string outputPath, string metadataPath)
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

        if (captureSettings.WriteMetadata)
        {
            arguments.AddRange(["--metadata", metadataPath, "--metadata-format", "txt"]);
        }

        if (captureSettings.UseManualExposure && captureSettings.ExposureMicroseconds.HasValue)
        {
            arguments.AddRange(["--shutter", Math.Max(1, captureSettings.ExposureMicroseconds.Value).ToString()]);
        }

        if (captureSettings.UseManualGain && captureSettings.AnalogGain.HasValue)
        {
            arguments.AddRange(["--gain", captureSettings.AnalogGain.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)]);
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

    private static int ResolveObservedExposureMicroseconds(CameraCaptureSettings settings, CaptureSettingsPlan captureSettings, RpiCaptureMetadata metadata)
    {
        if (metadata.ExposureTimeMicroseconds is int exposureFromMetadata && exposureFromMetadata > 0)
        {
            return exposureFromMetadata;
        }

        if (captureSettings.ExposureMicroseconds is int exposureFromPlan && exposureFromPlan > 0)
        {
            return exposureFromPlan;
        }

        return Math.Max(1, settings.ShutterMicroseconds);
    }

    private static double ResolveObservedAnalogGain(CameraCaptureSettings settings, CaptureSettingsPlan captureSettings, RpiCaptureMetadata metadata)
    {
        if (metadata.AnalogueGain is double gainFromMetadata && gainFromMetadata > 0)
        {
            return gainFromMetadata;
        }

        if (captureSettings.AnalogGain is double gainFromPlan && gainFromPlan > 0)
        {
            return gainFromPlan;
        }

        return Math.Max(1.0, settings.AnalogGain);
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
        updatedConfig.Camera.ShutterMicroseconds = Math.Clamp(updatedConfig.Camera.ShutterMicroseconds, 1, 600_000_000);
        updatedConfig.Camera.AnalogGain = Math.Clamp(updatedConfig.Camera.AnalogGain, 1.0, 64.0);
        updatedConfig.Camera.AutoMaxExposureMicroseconds = Math.Clamp(updatedConfig.Camera.AutoMaxExposureMicroseconds, 1, 600_000_000);
        updatedConfig.Camera.AutoMaxGain = Math.Clamp(updatedConfig.Camera.AutoMaxGain, 1.0, 64.0);
        updatedConfig.Camera.AutoMeanTarget = Math.Clamp(updatedConfig.Camera.AutoMeanTarget, 0.0, 1.0);
        updatedConfig.Camera.AutoMeanThreshold = Math.Clamp(updatedConfig.Camera.AutoMeanThreshold, 0.0, 1.0);
        updatedConfig.Camera.AutoMeanP0 = Math.Clamp(updatedConfig.Camera.AutoMeanP0, 0.0, 1000.0);
        updatedConfig.Camera.AutoMeanP1 = Math.Clamp(updatedConfig.Camera.AutoMeanP1, 0.0, 1000.0);
        updatedConfig.Camera.AutoMeanP2 = Math.Clamp(updatedConfig.Camera.AutoMeanP2, 0.0, 1000.0);
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

    private void SetLastError(string? message)
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
                TryDeleteFile(GetFrameMetadataPath(framePath));
            }
            catch (Exception ex)
            {
                Logger.Warning($"Unable to delete captured frame '{framePath}': {ex.Message}");
            }
        }

        InvalidateStorageStatus();
    }

    private async Task<FileCleanupResult?> DeleteSessionFileAsync(
        string? sessionId,
        string? relativePath,
        string folderName,
        bool isArtifact,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sessionId) || string.IsNullOrWhiteSpace(relativePath))
        {
            return null;
        }

        sessionId = sessionId.Trim();
        relativePath = relativePath.Trim();

        EnsureSessionFilesCanBeModified(sessionId);

        var resolvedPath = ResolveSessionFilePath(sessionId, relativePath, folderName);
        if (resolvedPath is null || !File.Exists(resolvedPath))
        {
            return null;
        }

        var before = GetStorageStatus(forceRefresh: true);
        var freedBytes = new FileInfo(resolvedPath).Length;
        File.Delete(resolvedPath);

        if (!isArtifact)
        {
            var metadataPath = GetFrameMetadataPath(resolvedPath);
            if (File.Exists(metadataPath))
            {
                freedBytes += new FileInfo(metadataPath).Length;
                File.Delete(metadataPath);
            }
        }

        var session = LoadSession(sessionId);
        if (session is not null)
        {
            if (isArtifact)
            {
                RemoveArtifactReference(session, relativePath);
            }
            else
            {
                RefreshSessionFrameMetadata(session);
            }

            PopulateSessionDerivedFields(session, paths.GetSessionRoot(session.Id));
            await SaveSessionAsync(session, cancellationToken).ConfigureAwait(false);
        }

        SyncCurrentImageToLatestPersistedFrame();
        InvalidateStorageStatus();

        return new FileCleanupResult
        {
            SessionId = sessionId,
            RelativePath = relativePath,
            FreedBytes = Math.Max(0, freedBytes),
            Storage = GetStorageStatus(forceRefresh: true)
        };
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

                PopulateSessionDerivedFields(session, sessionDirectory);
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
            DiskUsedBytes = Math.Max(0, diskTotalBytes - diskAvailableBytes),
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

    private string ResolvePreferredRepoDirectory()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var candidates = new[]
        {
            Path.Combine(home, "pins-allsky-plugin"),
            Path.Combine(home, "Projects", "piplugin")
        };

        return candidates.FirstOrDefault(candidate => Directory.Exists(Path.Combine(candidate, ".git")))
            ?? candidates[0];
    }

    private static string ShellQuote(string value)
    {
        return $"'{value.Replace("'", "'\"'\"'")}'";
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
        DiskUsedBytes = source.DiskUsedBytes,
        DiskTotalBytes = source.DiskTotalBytes,
        LimitEnabled = source.LimitEnabled,
        WithinLimit = source.WithinLimit
    };

    private EstimateBaselineInfo? BuildEstimateBaselineInfo(IEnumerable<SessionInfo> sessions)
    {
        var preferred = sessions.FirstOrDefault(session =>
            !string.IsNullOrWhiteSpace(session.Label)
            && string.Equals(session.Label, "test-maja", StringComparison.OrdinalIgnoreCase)
            && session.StoredFrameCount > 0);

        var baseline = preferred ?? sessions.FirstOrDefault(session => session.StoredFrameCount > 0);
        if (baseline is null)
        {
            return null;
        }

        var timelapseBytes = baseline.Products.Timelapse?.SizeBytes ?? 0;
        var keogramBytes = baseline.Products.Keogram?.SizeBytes ?? 0;
        var startrailsBytes = baseline.Products.Startrails?.SizeBytes ?? 0;
        var frameBytes = Math.Max(0, baseline.TotalSizeBytes - timelapseBytes - keogramBytes - startrailsBytes);

        return new EstimateBaselineInfo
        {
            SessionId = baseline.Id,
            Label = baseline.Label,
            StoredFrameCount = baseline.StoredFrameCount,
            SourceSessionBytes = baseline.TotalSizeBytes,
            AverageFrameBytes = baseline.StoredFrameCount > 0 ? frameBytes / baseline.StoredFrameCount : 0,
            TimelapseBytes = timelapseBytes,
            KeogramBytes = keogramBytes,
            StartrailsBytes = startrailsBytes
        };
    }

    private void PopulateSessionDerivedFields(SessionInfo session, string sessionDirectory)
    {
        if (!Directory.Exists(sessionDirectory))
        {
            session.TotalSizeBytes = 0;
            session.StoredFrameCount = 0;
            return;
        }

        var framesRoot = Path.GetFullPath(paths.GetFramesRoot(session.Id));
        long totalSizeBytes = 0;
        var storedFrameCount = 0;

        foreach (var filePath in Directory.EnumerateFiles(sessionDirectory, "*", SearchOption.AllDirectories))
        {
            try
            {
                var info = new FileInfo(filePath);
                totalSizeBytes += info.Length;

                if (string.Equals(info.Extension, ".jpg", StringComparison.OrdinalIgnoreCase)
                    && IsPathWithinDirectory(info.FullName, framesRoot))
                {
                    storedFrameCount += 1;
                }
            }
            catch
            {
            }
        }

        session.TotalSizeBytes = totalSizeBytes;
        session.StoredFrameCount = storedFrameCount;
    }

    private List<ArtifactInfo> GetSessionArtifacts(SessionInfo session)
    {
        var artifacts = new List<ArtifactInfo>();

        AddArtifactIfPresent(artifacts, session.Products.Timelapse);
        AddArtifactIfPresent(artifacts, session.Products.Keogram);
        AddArtifactIfPresent(artifacts, session.Products.Startrails);

        return artifacts
            .OrderBy(artifact => artifact.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private void AddArtifactIfPresent(List<ArtifactInfo> artifacts, ArtifactInfo? artifact)
    {
        if (artifact is null || string.IsNullOrWhiteSpace(artifact.RelativePath))
        {
            return;
        }

        var artifactPath = Path.Combine(paths.DataRoot, artifact.RelativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(artifactPath))
        {
            return;
        }

        artifacts.Add(new ArtifactInfo
        {
            Name = artifact.Name,
            RelativePath = artifact.RelativePath,
            GeneratedAtUtc = artifact.GeneratedAtUtc,
            SizeBytes = artifact.SizeBytes > 0 ? artifact.SizeBytes : new FileInfo(artifactPath).Length
        });
    }

    private List<FrameInfo> GetFrameEntries(string sessionId)
    {
        var framesRoot = paths.GetFramesRoot(sessionId);
        if (!Directory.Exists(framesRoot))
        {
            return [];
        }

        return Directory.EnumerateFiles(framesRoot, "*.jpg", SearchOption.TopDirectoryOnly)
            .Select(framePath =>
            {
                var info = new FileInfo(framePath);
                var metadata = LoadFrameMetadata(info.FullName);
                return new FrameInfo
                {
                    Name = info.Name,
                    RelativePath = paths.GetRelativePath(info.FullName),
                    CapturedAtUtc = metadata?.CapturedAtUtc
                        ?? ParseFrameTimestamp(info.Name)
                        ?? new DateTimeOffset(info.LastWriteTimeUtc, TimeSpan.Zero),
                    ExposureMicroseconds = metadata?.ExposureMicroseconds,
                    AnalogGain = metadata?.AnalogGain,
                    MeanBrightness = metadata?.MeanBrightness,
                    SizeBytes = info.Length
                };
            })
            .OrderByDescending(frame => frame.CapturedAtUtc ?? DateTimeOffset.MinValue)
            .ThenByDescending(frame => frame.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static DateTimeOffset? ParseFrameTimestamp(string fileName)
    {
        const string prefix = "frame-";
        const string suffix = ".jpg";

        if (!fileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            || !fileName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var timestamp = fileName[prefix.Length..^suffix.Length];
        if (!DateTime.TryParseExact(
                timestamp,
                "yyyyMMdd'T'HHmmssfff",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsed))
        {
            return null;
        }

        return new DateTimeOffset(parsed, TimeSpan.Zero);
    }

    private void RemoveArtifactReference(SessionInfo session, string relativePath)
    {
        if (session.Products.Timelapse is not null && string.Equals(session.Products.Timelapse.RelativePath, relativePath, StringComparison.Ordinal))
        {
            session.Products.Timelapse = null;
        }

        if (session.Products.Keogram is not null && string.Equals(session.Products.Keogram.RelativePath, relativePath, StringComparison.Ordinal))
        {
            session.Products.Keogram = null;
        }

        if (session.Products.Startrails is not null && string.Equals(session.Products.Startrails.RelativePath, relativePath, StringComparison.Ordinal))
        {
            session.Products.Startrails = null;
        }
    }

    private void RefreshSessionFrameMetadata(SessionInfo session)
    {
        var frames = GetFrameEntries(session.Id);
        session.StoredFrameCount = frames.Count;

        var latest = frames.FirstOrDefault();
        session.LastFrameRelativePath = latest?.RelativePath;
        session.LastCaptureAtUtc = latest?.CapturedAtUtc;
        session.LastExposureMicroseconds = latest?.ExposureMicroseconds;
        session.LastAnalogGain = latest?.AnalogGain;
        session.LastMeanBrightness = latest?.MeanBrightness;
    }

    private void EnsureSessionFilesCanBeModified(string sessionId)
    {
        lock (sync)
        {
            if (generateInProgress)
            {
                throw new InvalidOperationException("Files cannot be deleted while products are rendering.");
            }

            if (captureTask is { IsCompleted: false } && string.Equals(currentSession?.Id, sessionId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Files cannot be deleted from the active capture session.");
            }
        }
    }

    private string? ResolveSessionFilePath(string sessionId, string relativePath, string folderName)
    {
        var candidatePath = Path.GetFullPath(Path.Combine(paths.DataRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        var expectedRoot = Path.GetFullPath(Path.Combine(paths.GetSessionRoot(sessionId), folderName));

        if (!IsPathWithinDirectory(candidatePath, expectedRoot))
        {
            return null;
        }

        return candidatePath;
    }

    private static bool IsPathWithinDirectory(string candidatePath, string directoryPath)
    {
        var normalizedDirectory = Path.GetFullPath(directoryPath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        var normalizedCandidate = Path.GetFullPath(candidatePath);

        return normalizedCandidate.StartsWith(normalizedDirectory, StringComparison.Ordinal);
    }

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

    private readonly record struct CaptureSettingsPlan(
        bool UseManualExposure,
        int? ExposureMicroseconds,
        bool UseManualGain,
        double? AnalogGain,
        bool WriteMetadata);

    private sealed record SequenceProbeResult(bool Reachable, bool SequenceRunning);
}
