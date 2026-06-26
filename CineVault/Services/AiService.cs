using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CineVault.Services;

/// <summary>
/// Abstraction for an optional AI text-summary provider. TMDb already supplies an
/// overview for most movies; this is only used as a fallback or when the user
/// explicitly prefers AI-written summaries in Settings.
/// </summary>



/// <summary>
/// Google Gemini implementation. Gemini's free tier is generous and is the
/// recommended option in Settings. Get a key at https://aistudio.google.com/apikey
/// </summary>
public class GeminiAiService
{
    private readonly HttpClient _http = new();
    private readonly string _apiKey;
    private readonly string _model;

    public GeminiAiService(string apiKey, string model)
    {
        _apiKey = apiKey;
        _model = string.IsNullOrWhiteSpace(model) ? "gemini-2.0-flash" : model;
    }

    public async Task<MovieInfo> GetMovieInfoAsync(string title)
    {
        if (string.IsNullOrWhiteSpace(_apiKey)) return null;

        var apiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";


        // Build request payload with stricter instruction
        var requestBody = new
        {
            contents = new[]
            {
                new {
                    parts = new[]
                    {
                        new { text =
                            $"Provide JSON with fields: OfficialMovieName, SummaryEnglish, SummaryArabic for the movie '{title}'. " +
                            "Respond with the summary text only, no preamble or markdown." }
                    }
                }
            }
        };

        try
        {
            string jsonRequest = JsonSerializer.Serialize(requestBody);
            var response = await _http.PostAsync(apiUrl, new StringContent(jsonRequest, Encoding.UTF8, "application/json"));
            response.EnsureSuccessStatusCode();

            string responseContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<MovieInfo>(responseContent);
        }
        catch (Exception ex)
        {
            // Network/parse issues should never crash a library scan - just skip the AI summary.
            return null;
        }
    }




    public class MovieInfo
    {
        [JsonPropertyName("OfficialMovieName")]
        public string OfficialMovieName { get; set; }

        [JsonPropertyName("SummaryEnglish")]
        public string SummaryEnglish { get; set; }

        [JsonPropertyName("SummaryArabic")]
        public string SummaryArabic { get; set; }
    }

}