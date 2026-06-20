using System.Text.Json.Serialization;

namespace CineVault.Models;

// Minimal DTOs matching the subset of TMDb's JSON response we need.

public class TmdbSearchResult
{
    [JsonPropertyName("results")]
    public List<TmdbSearchItem> Results { get; set; } = new();
}

public class TmdbSearchItem
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("release_date")]
    public string? ReleaseDate { get; set; }
}

public class TmdbMovieDetails
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("overview")]
    public string? Overview { get; set; }

    [JsonPropertyName("backdrop_path")]
    public string? BackdropPath { get; set; }

    [JsonPropertyName("poster_path")]
    public string? PosterPath { get; set; }

    [JsonPropertyName("vote_average")]
    public double VoteAverage { get; set; }

    [JsonPropertyName("release_date")]
    public string? ReleaseDate { get; set; }

    [JsonPropertyName("credits")]
    public TmdbCredits? Credits { get; set; }

    [JsonPropertyName("videos")]
    public TmdbVideos? Videos { get; set; }
}

public class TmdbCredits
{
    [JsonPropertyName("cast")]
    public List<TmdbCastDto> Cast { get; set; } = new();
}

public class TmdbCastDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("character")]
    public string? Character { get; set; }

    [JsonPropertyName("profile_path")]
    public string? ProfilePath { get; set; }

    [JsonPropertyName("order")]
    public int Order { get; set; }
}

public class TmdbVideos
{
    [JsonPropertyName("results")]
    public List<TmdbVideoDto> Results { get; set; } = new();
}

public class TmdbVideoDto
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("site")]
    public string Site { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("official")]
    public bool Official { get; set; }
}
