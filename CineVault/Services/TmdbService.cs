using System.Net.Http;
using System.Net.Http.Json;
using System.Web;
using CineVault.Models;

namespace CineVault.Services;

/// <summary>
/// Thin wrapper around the free TMDb (The Movie Database) API.
/// One call set gives us: overview/summary, rating, release date, backdrop
/// (used as the trending blurred background), top cast with photos, and trailer.
/// </summary>
public class TmdbService
{
    private const string BaseUrl = "https://api.themoviedb.org/3";
    private const string ImageBase = "https://image.tmdb.org/t/p";

    private readonly HttpClient _http;
    private readonly string _apiKey;

    public TmdbService(string apiKey)
    {
        _apiKey = apiKey;
        _http = new HttpClient();
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);

    public async Task<TmdbMovieDetails?> FindMovieDetailsAsync(string title)
    {
        if (!IsConfigured) return null;

        var query = HttpUtility.UrlEncode(title);
        var searchUrl = $"{BaseUrl}/search/movie?api_key={_apiKey}&query={query}";

        var searchResult = await _http.GetFromJsonAsync<TmdbSearchResult>(searchUrl);
        var first = searchResult?.Results.FirstOrDefault();
        if (first == null) return null;

        // append_to_response lets us fetch details + cast + trailer in a single request.
        var detailsUrl = $"{BaseUrl}/movie/{first.Id}?api_key={_apiKey}&append_to_response=credits,videos";
        return await _http.GetFromJsonAsync<TmdbMovieDetails>(detailsUrl);
    }

    public static string? BuildImageUrl(string? path, string size = "w500") =>
        string.IsNullOrEmpty(path) ? null : $"{ImageBase}/{size}{path}";

    public static string? FindTrailerUrl(TmdbMovieDetails details)
    {
        var trailer = details.Videos?.Results
            .Where(v => v.Site == "YouTube" && v.Type == "Trailer")
            .OrderByDescending(v => v.Official)
            .FirstOrDefault();

        return trailer == null ? null : $"https://www.youtube.com/watch?v={trailer.Key}";
    }
}
