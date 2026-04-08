using NINA.PINS.AllSky.Models;

namespace NINA.PINS.AllSky.Services;

internal enum AllSkyAutoMode
{
    Off = 0,
    ExposureAndGain,
    ExposureOnly,
    GainOnly
}

internal sealed class AllSkyAutoExposureController
{
    private const int HistorySize = 3;
    private const double ShutterSteps = 6.0;
    private const int MaxExposureLevelChange = 50;
    private static readonly double ShutterStepSquared = Math.Pow(ShutterSteps, 2.0);

    private readonly double[] meanHistory = new double[HistorySize];
    private readonly int[] exposureLevelHistory = new int[HistorySize];

    private int meanCount;
    private int lastExposureChange;
    private bool fastForward;
    private int exposureLevel;

    public bool IsInitialized { get; private set; }
    public int CurrentExposureMicroseconds { get; private set; }
    public double CurrentAnalogGain { get; private set; } = 1.0;

    public static AllSkyAutoMode ResolveMode(CameraCaptureSettings settings)
    {
        if (settings.UseManualExposure && settings.UseManualGain)
        {
            return AllSkyAutoMode.Off;
        }

        if (!settings.UseManualExposure && !settings.UseManualGain)
        {
            return AllSkyAutoMode.ExposureAndGain;
        }

        return settings.UseManualExposure
            ? AllSkyAutoMode.GainOnly
            : AllSkyAutoMode.ExposureOnly;
    }

    public static bool IsEnabled(CameraCaptureSettings settings)
    {
        return ResolveMode(settings) != AllSkyAutoMode.Off && settings.AutoMeanTarget > 0;
    }

    public void Reset()
    {
        Array.Fill(meanHistory, 0);
        Array.Fill(exposureLevelHistory, 0);
        meanCount = 0;
        lastExposureChange = 0;
        fastForward = false;
        exposureLevel = 0;
        IsInitialized = false;
        CurrentExposureMicroseconds = 0;
        CurrentAnalogGain = 1.0;
    }

    // Adapted from AllSky's Raspberry Pi capture path:
    // - src/mode_mean.cpp
    // - src/capture_RPi.cpp
    public AutoExposureObservation Observe(CameraCaptureSettings settings, int observedExposureMicroseconds, double observedAnalogGain, double observedMean)
    {
        var mode = ResolveMode(settings);
        if (mode == AllSkyAutoMode.Off || settings.AutoMeanTarget <= 0)
        {
            Reset();
            return new AutoExposureObservation(true, false);
        }

        var context = AutoExposureContext.Create(settings, mode);
        var clampedObservedExposure = mode == AllSkyAutoMode.GainOnly
            ? context.FixedExposureMicroseconds
            : Math.Clamp(observedExposureMicroseconds, context.MinExposureMicroseconds, context.MaxExposureMicroseconds);
        var clampedObservedGain = mode == AllSkyAutoMode.ExposureOnly
            ? context.FixedGain
            : Math.Clamp(observedAnalogGain, context.MinGain, context.MaxGain);

        if (!IsInitialized)
        {
            exposureLevel = CalculateExposureLevel(clampedObservedExposure, clampedObservedGain);
            Array.Fill(meanHistory, context.TargetMean);
            Array.Fill(exposureLevelHistory, exposureLevel);

            CurrentExposureMicroseconds = clampedObservedExposure;
            CurrentAnalogGain = clampedObservedGain;
            IsInitialized = true;
        }
        else
        {
            CurrentExposureMicroseconds = clampedObservedExposure;
            CurrentAnalogGain = clampedObservedGain;
        }

        meanHistory[meanCount % HistorySize] = Math.Clamp(observedMean, 0.0, 1.0);

        var currentIndex = (meanCount + HistorySize) % HistorySize;
        var previousIndex = (meanCount + HistorySize - 1) % HistorySize;

        var meanForecast = Math.Clamp((2.0 * meanHistory[currentIndex]) - meanHistory[previousIndex], 0.0, 1.0);
        double weightedMean = 0.0;
        var divisor = HistorySize;
        for (var i = 1; i <= HistorySize; i++)
        {
            var historyIndex = (meanCount + i) % HistorySize;
            weightedMean += meanHistory[historyIndex] * i;
            divisor += i;
        }

        weightedMean += meanForecast * HistorySize;
        weightedMean /= divisor;

        var forecastMeanDifference = Math.Abs(weightedMean - context.TargetMean);
        var measuredMeanDifference = Math.Abs(meanHistory[currentIndex] - context.TargetMean);
        var exposureChange = CalculateExposureChange(context, forecastMeanDifference, measuredMeanDifference);
        var withinThreshold = false;
        var canImprove = false;

        if (meanHistory[currentIndex] < (context.TargetMean - context.TargetThreshold))
        {
            if (CurrentAnalogGain < context.MaxGain || CurrentExposureMicroseconds < context.MaxExposureMicroseconds)
            {
                canImprove = true;
                exposureLevel += exposureChange;
            }
        }
        else if (meanHistory[currentIndex] > (context.TargetMean + context.TargetThreshold))
        {
            if (CurrentAnalogGain > context.MinGain || CurrentExposureMicroseconds > context.MinExposureMicroseconds)
            {
                canImprove = true;
                exposureLevel -= exposureChange;
            }
        }
        else
        {
            withinThreshold = true;
        }

        exposureLevel = Math.Clamp(exposureLevel, context.MinExposureLevel, context.MaxExposureLevel);
        var effectiveExposureMicroseconds = CalculateEffectiveExposureTime(exposureLevel);

        if (exposureLevel == context.MaxExposureLevel || exposureLevel == context.MinExposureLevel)
        {
            fastForward = true;
        }

        if (fastForward &&
            Math.Abs(meanHistory[currentIndex] - context.TargetMean) < context.TargetThreshold &&
            Math.Abs(meanHistory[previousIndex] - context.TargetMean) < context.TargetThreshold)
        {
            fastForward = false;
        }

        var nextExposureMicroseconds = CurrentExposureMicroseconds;
        var nextAnalogGain = CurrentAnalogGain;

        switch (mode)
        {
            case AllSkyAutoMode.ExposureAndGain:
            {
                var gainCandidate = Math.Max(context.MinGain, effectiveExposureMicroseconds / (double)context.MaxExposureMicroseconds);
                nextAnalogGain = Math.Min(context.MaxGain, gainCandidate);
                var exposureCandidate = Math.Max(context.MinExposureMicroseconds, effectiveExposureMicroseconds / nextAnalogGain);
                nextExposureMicroseconds = (int)Math.Round(Math.Min(context.MaxExposureMicroseconds, exposureCandidate));
                break;
            }
            case AllSkyAutoMode.GainOnly:
            {
                nextExposureMicroseconds = context.FixedExposureMicroseconds;
                var gainCandidate = Math.Max(context.MinGain, effectiveExposureMicroseconds / (double)context.FixedExposureMicroseconds);
                nextAnalogGain = Math.Min(context.MaxGain, gainCandidate);
                break;
            }
            case AllSkyAutoMode.ExposureOnly:
            {
                nextAnalogGain = context.FixedGain;
                var exposureCandidate = Math.Max(context.MinExposureMicroseconds, effectiveExposureMicroseconds / context.FixedGain);
                nextExposureMicroseconds = (int)Math.Round(Math.Min(context.MaxExposureMicroseconds, exposureCandidate));
                break;
            }
        }

        meanCount++;
        exposureLevelHistory[meanCount % HistorySize] = exposureLevel;
        CurrentExposureMicroseconds = Math.Clamp(nextExposureMicroseconds, context.MinExposureMicroseconds, context.MaxExposureMicroseconds);
        CurrentAnalogGain = Math.Clamp(nextAnalogGain, context.MinGain, context.MaxGain);
        return new AutoExposureObservation(withinThreshold, canImprove);
    }

    private int CalculateExposureChange(AutoExposureContext context, double forecastMeanDifference, double measuredMeanDifference)
    {
        const double multiplier1 = 1.75;
        const double multiplier2 = 1.25;

        double rawChange;

        if (fastForward || measuredMeanDifference > (context.TargetThreshold * multiplier1))
        {
            rawChange = Math.Max(
                1.0,
                context.P0 + (context.P1 * forecastMeanDifference) + Math.Pow(context.P2 * forecastMeanDifference, 2.0));
        }
        else if (measuredMeanDifference > (context.TargetThreshold * multiplier2))
        {
            rawChange = Math.Max(
                1.0,
                context.P0 + (context.P1 * forecastMeanDifference) + (Math.Pow(context.P2 * forecastMeanDifference, 2.0) / 2.0));
        }
        else if (measuredMeanDifference > context.TargetThreshold)
        {
            rawChange = Math.Max(1.0, context.P0 + (context.P1 * forecastMeanDifference));
        }
        else
        {
            rawChange = ShutterSteps / 2.0;
        }

        var clipped = Math.Min(MaxExposureLevelChange, (int)Math.Round(rawChange));
        lastExposureChange = clipped;
        return clipped;
    }

    private static int CalculateExposureLevel(int exposureMicroseconds, double analogGain)
    {
        // AllSky allows negative exposure levels for effective exposures below 1 second.
        // Clamping to 1.0 breaks day/night convergence because the controller can no longer
        // step down into short daytime shutters.
        var normalizedExposure = Math.Max(1e-12, analogGain * exposureMicroseconds / 1_000_000.0);
        return (int)Math.Round(Math.Log(normalizedExposure, 2.0) * ShutterStepSquared);
    }

    private static long CalculateEffectiveExposureTime(int exposureLevel)
    {
        return (long)Math.Round(Math.Pow(2.0, exposureLevel / ShutterStepSquared) * 1_000_000.0);
    }

    private sealed record AutoExposureContext(
        AllSkyAutoMode Mode,
        double TargetMean,
        double TargetThreshold,
        double P0,
        double P1,
        double P2,
        int MinExposureMicroseconds,
        int MaxExposureMicroseconds,
        double MinGain,
        double MaxGain,
        int FixedExposureMicroseconds,
        double FixedGain,
        int MinExposureLevel,
        int MaxExposureLevel)
    {
        public static AutoExposureContext Create(CameraCaptureSettings settings, AllSkyAutoMode mode)
        {
            var minExposureMicroseconds = 1;
            var minGain = 1.0;
            var maxExposureMicroseconds = Math.Clamp(settings.AutoMaxExposureMicroseconds, minExposureMicroseconds, 600_000_000);
            var maxGain = Math.Clamp(settings.AutoMaxGain, minGain, 64.0);
            var fixedExposureMicroseconds = Math.Clamp(settings.ShutterMicroseconds, minExposureMicroseconds, maxExposureMicroseconds);
            var fixedGain = Math.Clamp(settings.AnalogGain, minGain, maxGain);

            var minExposureLevel = mode switch
            {
                AllSkyAutoMode.ExposureAndGain => CalculateExposureLevel(minExposureMicroseconds, minGain) - 1,
                AllSkyAutoMode.GainOnly => CalculateExposureLevel(fixedExposureMicroseconds, minGain) - 1,
                AllSkyAutoMode.ExposureOnly => CalculateExposureLevel(minExposureMicroseconds, fixedGain) - 1,
                _ => CalculateExposureLevel(fixedExposureMicroseconds, fixedGain) - 1
            };

            var maxExposureLevel = mode switch
            {
                AllSkyAutoMode.ExposureAndGain => CalculateExposureLevel(maxExposureMicroseconds, maxGain) + 1,
                AllSkyAutoMode.GainOnly => CalculateExposureLevel(fixedExposureMicroseconds, maxGain) + 1,
                AllSkyAutoMode.ExposureOnly => CalculateExposureLevel(maxExposureMicroseconds, fixedGain) + 1,
                _ => CalculateExposureLevel(fixedExposureMicroseconds, fixedGain) + 1
            };

            return new AutoExposureContext(
                mode,
                Math.Clamp(settings.AutoMeanTarget, 0.0, 1.0),
                Math.Clamp(settings.AutoMeanThreshold, 0.001, 1.0),
                Math.Clamp(settings.AutoMeanP0, 0.0, 1000.0),
                Math.Clamp(settings.AutoMeanP1, 0.0, 1000.0),
                Math.Clamp(settings.AutoMeanP2, 0.0, 1000.0),
                minExposureMicroseconds,
                maxExposureMicroseconds,
                minGain,
                maxGain,
                fixedExposureMicroseconds,
                fixedGain,
                minExposureLevel,
                maxExposureLevel);
        }
    }
}

internal readonly record struct AutoExposureObservation(bool WithinThreshold, bool CanImprove);
