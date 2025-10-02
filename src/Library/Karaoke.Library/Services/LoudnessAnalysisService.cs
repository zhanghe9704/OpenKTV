using System;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Karaoke.Library.Services;

public sealed class LoudnessAnalysisService : ILoudnessAnalysisService
{
    private const double TargetLoudness = -14.0; // LUFS target (Spotify/YouTube standard)
    private static readonly Regex LoudnessRegex = new(@"I:\s*(-?\d+\.?\d*)\s*LUFS", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly ILogger<LoudnessAnalysisService> _logger;

    public LoudnessAnalysisService(ILogger<LoudnessAnalysisService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<(double loudnessLufs, double gainDb)?> AnalyzeLoudnessAsync(string filePath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            _logger.LogWarning("File path is null or empty");
            return null;
        }

        if (!File.Exists(filePath))
        {
            _logger.LogWarning("File does not exist: {FilePath}", filePath);
            return null;
        }

        try
        {
            _logger.LogInformation("[LoudnessAnalysis] Starting loudness analysis for: {FilePath}", filePath);

            var startInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-i \"{filePath}\" -af ebur128=framelog=verbose -f null -",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            _logger.LogInformation("[LoudnessAnalysis] Executing: ffmpeg {Arguments}", startInfo.Arguments);

            using var process = new Process { StartInfo = startInfo };
            var stderrBuilder = new System.Text.StringBuilder();

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    stderrBuilder.AppendLine(e.Data);
                }
            };

            process.Start();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

            if (cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to kill ffmpeg process during cancellation");
                }
                return null;
            }

            if (process.ExitCode != 0)
            {
                _logger.LogWarning("[LoudnessAnalysis] ffmpeg exited with code {ExitCode} for file: {FilePath}", process.ExitCode, filePath);
                return null;
            }

            var stderr = stderrBuilder.ToString();
            _logger.LogDebug("[LoudnessAnalysis] ffmpeg stderr output length: {Length} chars", stderr.Length);

            var match = LoudnessRegex.Match(stderr);

            if (!match.Success)
            {
                _logger.LogWarning("[LoudnessAnalysis] Could not parse loudness from ffmpeg output for: {FilePath}", filePath);
                _logger.LogDebug("[LoudnessAnalysis] ffmpeg stderr: {Stderr}", stderr);
                return null;
            }

            if (!double.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var loudnessLufs))
            {
                _logger.LogWarning("[LoudnessAnalysis] Failed to parse loudness value: {Value}", match.Groups[1].Value);
                return null;
            }

            // Calculate gain needed to reach target loudness
            var gainDb = TargetLoudness - loudnessLufs;

            _logger.LogInformation("[LoudnessAnalysis] SUCCESS: {FilePath}: {Loudness} LUFS, gain: {Gain} dB",
                filePath, loudnessLufs, gainDb);

            return (loudnessLufs, gainDb);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LoudnessAnalysis] ERROR analyzing loudness for file: {FilePath}", filePath);
            return null;
        }
    }
}
