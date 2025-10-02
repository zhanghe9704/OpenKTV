namespace Karaoke.Library.Services;

public interface ILoudnessAnalysisService
{
    /// <summary>
    /// Analyzes the loudness of an audio file using ffmpeg with EBU R128.
    /// </summary>
    /// <param name="filePath">Path to the audio/video file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple containing the integrated loudness in LUFS and the calculated gain in dB to reach -14 LUFS.</returns>
    Task<(double loudnessLufs, double gainDb)?> AnalyzeLoudnessAsync(string filePath, CancellationToken cancellationToken);
}
