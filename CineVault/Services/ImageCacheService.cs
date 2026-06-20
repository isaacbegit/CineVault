using System.IO;
using System.Net.Http;

namespace CineVault.Services;

/// <summary>
/// Downloads remote images (backdrops, cast photos) once and caches them on disk,
/// so the app works offline after the first scan and doesn't re-download on every launch.
/// </summary>
public class ImageCacheService
{
    private readonly string _cacheFolder;
    private readonly HttpClient _http = new();

    public ImageCacheService()
    {
        _cacheFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CineVault", "Cache");
        Directory.CreateDirectory(_cacheFolder);
    }

    public async Task<string?> DownloadAndCacheAsync(string? url, string fileNameHint)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;

        var safeName = string.Concat(fileNameHint.Where(c => !Path.GetInvalidFileNameChars().Contains(c)));
        var extension = Path.GetExtension(url);
        if (string.IsNullOrWhiteSpace(extension) || extension.Length > 5) extension = ".jpg";

        var localPath = Path.Combine(_cacheFolder, safeName + extension);
        if (File.Exists(localPath)) return localPath;

        try
        {
            var bytes = await _http.GetByteArrayAsync(url);
            await File.WriteAllBytesAsync(localPath, bytes);
            return localPath;
        }
        catch
        {
            return null;
        }
    }
}
