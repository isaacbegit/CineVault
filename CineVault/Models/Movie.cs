namespace CineVault.Models;

/// <summary>
/// A single movie discovered on disk, enriched with metadata from TMDb / AI.
/// </summary>
public class Movie
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;

    // File system info
    public string FolderPath { get; set; } = string.Empty;
    public string? VideoPath { get; set; }
    public string? SubtitlePath { get; set; }
    public string? PosterPath { get; set; }       // local "folder.jpg" image
    public DateTime LastModified { get; set; }
    public DateTime LastScanned { get; set; } = DateTime.Now;

    // Online metadata
    public string? TmdbId { get; set; }
    public string? Overview { get; set; }
    public double Rating { get; set; }
    public string? ReleaseDate { get; set; }
    public string? TrailerUrl { get; set; }
    public string? BackdropUrl { get; set; }
    public string? BackdropLocalPath { get; set; }
}
