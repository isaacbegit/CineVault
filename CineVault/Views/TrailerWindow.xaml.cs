using System.Diagnostics;
using System.Web;
using System.Windows;

namespace CineVault.Views;

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

            var videoId = ExtractYouTubeId(_trailerUrl);
            if (string.IsNullOrEmpty(videoId))
            {
                TrailerWebView.CoreWebView2.Navigate(_trailerUrl);
                return;
            }

            TrailerWebView.CoreWebView2.NavigateToString(BuildEmbedHtml(videoId));
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

    private static string BuildEmbedUrl(string trailerUrl)
    {
        var videoId = ExtractYouTubeId(trailerUrl);
        return string.IsNullOrEmpty(videoId)
            ? trailerUrl
            : $"https://www.youtube.com/embed/{videoId}?autoplay=1";
    }

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