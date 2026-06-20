namespace CineVault.Models;

/// <summary>
/// A single cast member shown in the movie details panel (top 5 per movie).
/// </summary>
public class CastMember
{
    public int Id { get; set; }
    public int MovieId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Character { get; set; }
    public string? ProfileImageUrl { get; set; }
    public string? ProfileLocalPath { get; set; }
    public int SortOrder { get; set; }
}
