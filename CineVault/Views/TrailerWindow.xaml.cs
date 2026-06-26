using System.Diagnostics;
using System.Web;
using System.Windows;

namespace CineVault.Views;

/// <summary>
/// Plays a YouTube trailer in-app using WebView2 (the Edge/Chromium engine built into Windows
/// 11 and freely installable on Windows 10). Falls back to the default browser if the
/// WebView2 Runtime isn't available on the machine.
/// </summary>
public partial class TrailerWindow : Window
{
    private readonly string _trailerUrl;

    public TrailerWindow(string movieTitle, string trailerUrl)
    {
        InitializeComponent();
        _trailerUrl = trailerUrl;

        TitleText.Text = $"{movieTitle} — Trailer";
        Title = TitleText.Text;

        Loaded += async (_, _) => await LoadTrailerAsync();
    }

    private async Task LoadTrailerAsync()
    {
        try
        {
            await TrailerWebView.EnsureCoreWebView2Async();

            // Spoof a standard Chrome UA so YouTube treats WebView2 as a real browser.
            TrailerWebView.CoreWebView2.Settings.UserAgent =
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36";

            var videoId = ExtractYouTubeId(_trailerUrl);

            // Use the full watch page, NOT the /embed/ URL.
            // Error 153 means the content owner disabled embedding, so /embed/ always fails.
            // The full watch page works regardless of embed restrictions.
            var url = string.IsNullOrEmpty(videoId)
                ? _trailerUrl
                : $"https://www.youtube.com/watch?v={videoId}&autoplay=1";

            TrailerWebView.CoreWebView2.Navigate(url);
        }
        catch
        {
            try { Process.Start(new ProcessStartInfo { FileName = _trailerUrl, UseShellExecute = true }); }
            catch { /* nothing more we can do */ }
            Close();
        }
    }
    private static string BuildEmbedHtml(string videoId) => $$"""
        <!DOCTYPE html>
        <html>
        <head>
            <style>
                html, body { margin:0; padding:0; background:#000; height:100%; overflow:hidden; }
                iframe { width:100%; height:100%; border:0; }
            </style>
        </head>
        <body>
            <iframe src="https://www.youtube.com/embed/{{videoId}}?autoplay=1&rel=0&modestbranding=1"
                    allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture"
                    allowfullscreen></iframe>
        </body>
        </html>
        """;

    private static string? ExtractYouTubeId(string url)
    {
        try
        {
            var uri = new Uri(url);
            if (uri.Host.Contains("youtu.be", StringComparison.OrdinalIgnoreCase))
                return uri.AbsolutePath.Trim('/');

            return HttpUtility.ParseQueryString(uri.Query)["v"];
        }
        catch
        {
            return null;
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
