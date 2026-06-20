using CineVault.Models;
using System.IO;

namespace CineVault.Services;

/// <summary>
/// Walks the movies root folder on disk and figures out, for each movie sub-folder,
/// which nested folder is the "real" one: the most recently modified folder that
/// contains a "folder.*" poster image, alongside its video and subtitle files.
/// </summary>
public class MovieScannerService
{
    private static readonly string[] VideoExtensions =
        { ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".flv", ".m4v", ".ts", ".webm" };

    private static readonly string[] SubtitleExtensions =
        { ".srt", ".sub", ".ass", ".vtt", ".ssa" };

    private static readonly string[] ImageExtensions =
        { ".jpg", ".jpeg", ".png", ".webp" };

    public List<Movie> ScanLibrary(string rootFolder)
    {
        var results = new List<Movie>();
        if (string.IsNullOrWhiteSpace(rootFolder) || !Directory.Exists(rootFolder))
            return results;

        foreach (var movieFolder in Directory.GetDirectories(rootFolder))
        {
            var movie = ScanMovieFolder(movieFolder);
            if (movie != null)
                results.Add(movie);
        }

        return results;
    }

    private Movie? ScanMovieFolder(string movieFolder)
    {
        var allDirs = new List<string> { movieFolder };
        try
        {
            allDirs.AddRange(Directory.GetDirectories(movieFolder, "*", SearchOption.AllDirectories));
        }
        catch (UnauthorizedAccessException)
        {
            // Skip folders we don't have permission to read.
        }

        string? bestDir = null;
        string? bestPoster = null;
        var bestTime = DateTime.MinValue;

        // Find the most recently modified folder that contains a "folder.*" poster image.
        foreach (var dir in allDirs)
        {
            string[] files;
            try { files = Directory.GetFiles(dir); }
            catch (UnauthorizedAccessException) { continue; }

            var posterFile = files.FirstOrDefault(f =>
                Path.GetFileNameWithoutExtension(f).Equals("folder", StringComparison.OrdinalIgnoreCase) &&
                ImageExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()));

            if (posterFile == null) continue;

            var dirTime = Directory.GetLastWriteTime(dir);
            if (dirTime >= bestTime)
            {
                bestTime = dirTime;
                bestDir = dir;
                bestPoster = posterFile;
            }
        }

        if (bestDir == null) return null; // no folder.jpg anywhere -> skip this movie

        var dirFiles = Directory.GetFiles(bestDir);
        var videoFile = dirFiles.FirstOrDefault(f => VideoExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()));
        var subtitleFile = dirFiles.FirstOrDefault(f => SubtitleExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()));

        return new Movie
        {
            Title = CleanTitle(Path.GetFileName(movieFolder)),
            FolderPath = movieFolder,
            PosterPath = bestPoster,
            VideoPath = videoFile,
            SubtitlePath = subtitleFile,
            LastModified = bestTime,
            LastScanned = DateTime.Now
        };
    }

    /// <summary>Turns a raw folder name like "The.Matrix.1999.1080p.BluRay.x264" into "The Matrix 1999".</summary>
    public static string CleanTitle(string rawName)
    {
        var name = rawName.Replace('.', ' ').Replace('_', ' ');

        name = System.Text.RegularExpressions.Regex.Replace(name, @"[\[\(].*?[\]\)]", " ");
        name = System.Text.RegularExpressions.Regex.Replace(name,
            @"\b(1080p|720p|2160p|4K|BluRay|BRRip|WEBRip|WEB-DL|HDRip|DVDRip|x264|x265|HEVC|AAC|YIFY)\b",
            " ", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        name = System.Text.RegularExpressions.Regex.Replace(name, @"\s+", " ").Trim();
        return name;
    }
}
