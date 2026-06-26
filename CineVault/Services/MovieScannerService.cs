using CineVault.Models;
using System.IO;
using static System.Net.WebRequestMethods;
using File = System.Net.WebRequestMethods.File;

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
        {".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".webp" };

    public List<Movie> ScanLibrary(string rootFolder)
    {
        var results = new List<Movie>();
        if (string.IsNullOrWhiteSpace(rootFolder) || !Directory.Exists(rootFolder))
            return results;

        var movieFiles = Directory
    .EnumerateFiles(rootFolder, "*.*", SearchOption.AllDirectories)
    .Where(file => VideoExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase));

        foreach (var movieFile in movieFiles)
        {

            var movie = ScanMovieFolder(movieFile);
            if (movie != null)
                results.Add(movie);
        }

        return results;
    }

    private Movie? ScanMovieFolder(string movieFile)
    {
        if (string.IsNullOrWhiteSpace(movieFile) || !System.IO.File.Exists(movieFile))
            return null;
        var movieFolder = Path.GetDirectoryName(movieFile);

        var bestPoster = Directory
            .EnumerateFiles(movieFolder, "*.*", SearchOption.AllDirectories)
            .Where(file => ImageExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase)).FirstOrDefault();


        var subtitleFile = Directory
          .EnumerateFiles(movieFolder, "*.*", SearchOption.AllDirectories)
          .Where(file => SubtitleExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase)).FirstOrDefault();


        return new Movie
        {
            Title = CleanTitle(Path.GetFileNameWithoutExtension(movieFile)),
            FolderPath = Path.GetDirectoryName(movieFile),
            PosterPath = bestPoster,
            VideoPath = movieFile,
            SubtitlePath = subtitleFile,
            LastModified = DateTime.Now,
            LastScanned = DateTime.Now
        };
    }

    /// <summary>Turns a raw folder name like "The.Matrix.1999.1080p.BluRay.x264" into "The Matrix 1999".</summary>
    public static string CleanTitle(string rawName)
    {
        if (string.IsNullOrEmpty(rawName)) return string.Empty;
        var name = rawName.Replace('.', ' ').Replace('_', ' ');

        name = System.Text.RegularExpressions.Regex.Replace(name, @"[\[\(].*?[\]\)]", " ");
        name = System.Text.RegularExpressions.Regex.Replace(name,
            @"\b(1080p|720p|2160p|4K|BluRay|BRRip|WEBRip|WEB-DL|HDRip|DVDRip|x264|x265|HEVC|AAC|YIFY)\b",
            " ", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        name = System.Text.RegularExpressions.Regex.Replace(name, @"\s+", " ").Trim();
        return name;
    }
}
