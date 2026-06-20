using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace CineVault.Services;

/// <summary>
/// Abstraction for an optional AI text-summary provider. TMDb already supplies an
/// overview for most movies; this is only used as a fallback or when the user
/// explicitly prefers AI-written summaries in Settings.
/// </summary>
public interface IAiService
{
    Task<string?> GetMovieSummaryAsync(string title);
}

public class NullAiService : IAiService
{
    public Task<string?> GetMovieSummaryAsync(string title) => Task.FromResult<string?>(null);
}

/// <summary>
/// Google Gemini implementation. Gemini's free tier is generous and is the
/// recommended option in Settings. Get a key at https://aistudio.google.com/apikey
/// </summary>
public class GeminiAiService : IAiService
{
    private readonly HttpClient _http = new();
    private readonly string _apiKey;
    private readonly string _model;

    public GeminiAiService(string apiKey, string model)
    {
        _apiKey = apiKey;
        _model = string.IsNullOrWhiteSpace(model) ? "gemini-2.0-flash" : model;
    }

    public async Task<string?> GetMovieSummaryAsync(string title)
    {
        if (string.IsNullOrWhiteSpace(_apiKey)) return null;

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";
        var prompt = $"Write a short, spoiler-free 3 sentence summary for the movie titled '{title}'. " +
                     "Respond with the summary text only, no preamble or markdown.";

        var body = new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = prompt } } }
            }
        };

        try
        {
            var response = await _http.PostAsJsonAsync(url, body);
            if (!response.IsSuccessStatusCode) return null;

            using var stream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);

            var text = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            return text?.Trim();
        }
        catch
        {
            // Network/parse issues should never crash a library scan - just skip the AI summary.
            return null;
        }
    }
}

public static class AiServiceFactory
{
    public static IAiService Create(string provider, string apiKey, string model) => provider switch
    {
        "Google Gemini" => new GeminiAiService(apiKey, model),
        _ => new NullAiService()
    };
}
