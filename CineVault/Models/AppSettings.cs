namespace CineVault.Models;

/// <summary>
/// All user-configurable application settings, persisted in the Settings table.
/// </summary>
public class AppSettings
{
    public string MoviesRootFolder { get; set; } = string.Empty;
    public string ExternalPlayerPath { get; set; } = string.Empty;

    public string TmdbApiKey { get; set; } = string.Empty;

    public string AiProvider { get; set; } = "None";   // "None" or "Google Gemini"
    public string AiApiKey { get; set; } = string.Empty;
    public string AiModel { get; set; } = "gemini-2.0-flash";
    public bool PreferAiSummary { get; set; } = false;
}
