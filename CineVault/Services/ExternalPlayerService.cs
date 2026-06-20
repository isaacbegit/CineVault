using System.Diagnostics;
using System.IO;
using System.Windows;

namespace CineVault.Services;

/// <summary>Launches the user's chosen external player (VLC, MPC-HC, PotPlayer, ...) with the movie file.</summary>
public class ExternalPlayerService
{
    public void Play(string playerPath, string videoFilePath)
    {
        if (string.IsNullOrWhiteSpace(videoFilePath) || !File.Exists(videoFilePath))
        {
            MessageBox.Show("Video file was not found.", "CineVault", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(playerPath) || !File.Exists(playerPath))
        {
            MessageBox.Show(
                "External player is not configured or not found.\nPlease set the player path in Settings.",
                "CineVault", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = playerPath,
                Arguments = $"\"{videoFilePath}\"",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not start player: {ex.Message}", "CineVault",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public void OpenUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;
        try
        {
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
        catch
        {
            // Best-effort: if no browser is registered, silently ignore.
        }
    }
}
