using Microsoft.Data.Sqlite;
using CineVault.Models;
using System.IO;

namespace CineVault.Services;

/// <summary>
/// All SQLite access lives here: schema creation, movies, cast and key/value settings.
/// Plain ADO.NET + raw SQL on purpose, to keep the project easy to read and modify.
/// </summary>
public class DatabaseService
{
    private readonly string _connectionString;

    public DatabaseService()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CineVault");
        Directory.CreateDirectory(folder);

        var dbPath = Path.Combine(folder, "cinevault.db");
        _connectionString = $"Data Source={dbPath}";
        Initialize();
    }

    private SqliteConnection GetConnection() => new(_connectionString);

    private void Initialize()
    {
        using var conn = GetConnection();
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS Movies (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Title TEXT NOT NULL,
                FolderPath TEXT NOT NULL UNIQUE,
                VideoPath TEXT,
                SubtitlePath TEXT,
                PosterPath TEXT,
                BackdropLocalPath TEXT,
                BackdropUrl TEXT,
                Overview TEXT,
                TmdbId TEXT,
                TrailerUrl TEXT,
                Rating REAL,
                ReleaseDate TEXT,
                LastModified TEXT,
                LastScanned TEXT
            );

            CREATE TABLE IF NOT EXISTS CastMembers (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                MovieId INTEGER NOT NULL,
                Name TEXT NOT NULL,
                Character TEXT,
                ProfileImageUrl TEXT,
                ProfileLocalPath TEXT,
                SortOrder INTEGER,
                FOREIGN KEY (MovieId) REFERENCES Movies(Id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS Settings (
                Key TEXT PRIMARY KEY,
                Value TEXT
            );
            """;
        cmd.ExecuteNonQuery();
    }

    // ===================== Movies =====================

    public List<Movie> GetAllMovies()
    {
        var list = new List<Movie>();
        using var conn = GetConnection();
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Movies ORDER BY Title;";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            list.Add(ReadMovie(reader));
        return list;
    }

    public Movie? GetMovieByFolder(string folderPath)
    {
        using var conn = GetConnection();
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Movies WHERE FolderPath = $folder;";
        cmd.Parameters.AddWithValue("$folder", folderPath);
        using var reader = cmd.ExecuteReader();
        return reader.Read() ? ReadMovie(reader) : null;
    }

    /// <summary>Inserts a new movie or updates the existing row matched by FolderPath. Returns the row Id.</summary>
    public int UpsertMovie(Movie movie)
    {
        using var conn = GetConnection();
        conn.Open();
        var existing = GetMovieByFolder(movie.FolderPath);

        var cmd = conn.CreateCommand();
        cmd.CommandText = existing == null
            ? """
              INSERT INTO Movies
              (Title, FolderPath, VideoPath, SubtitlePath, PosterPath, BackdropLocalPath, BackdropUrl,
               Overview, TmdbId, TrailerUrl, Rating, ReleaseDate, LastModified, LastScanned)
              VALUES
              ($title, $folder, $video, $sub, $poster, $backdropLocal, $backdropUrl,
               $overview, $tmdbId, $trailer, $rating, $release, $modified, $scanned);
              SELECT last_insert_rowid();
              """
            : """
              UPDATE Movies SET
                  Title=$title, VideoPath=$video, SubtitlePath=$sub, PosterPath=$poster,
                  BackdropLocalPath=$backdropLocal, BackdropUrl=$backdropUrl, Overview=$overview,
                  TmdbId=$tmdbId, TrailerUrl=$trailer, Rating=$rating, ReleaseDate=$release,
                  LastModified=$modified, LastScanned=$scanned
              WHERE FolderPath=$folder;
              SELECT Id FROM Movies WHERE FolderPath=$folder;
              """;

        cmd.Parameters.AddWithValue("$title", movie.Title);
        cmd.Parameters.AddWithValue("$folder", movie.FolderPath);
        cmd.Parameters.AddWithValue("$video", (object?)movie.VideoPath ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$sub", (object?)movie.SubtitlePath ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$poster", (object?)movie.PosterPath ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$backdropLocal", (object?)movie.BackdropLocalPath ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$backdropUrl", (object?)movie.BackdropUrl ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$overview", (object?)movie.Overview ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$tmdbId", (object?)movie.TmdbId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$trailer", (object?)movie.TrailerUrl ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$rating", movie.Rating);
        cmd.Parameters.AddWithValue("$release", (object?)movie.ReleaseDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$modified", movie.LastModified.ToString("o"));
        cmd.Parameters.AddWithValue("$scanned", movie.LastScanned.ToString("o"));

        var result = cmd.ExecuteScalar();
        return Convert.ToInt32(result);
    }

    /// <summary>Removes movies whose folder no longer exists on disk (e.g. deleted/renamed since last scan).</summary>
    public void DeleteMoviesNotIn(IEnumerable<string> folderPaths)
    {
        using var conn = GetConnection();
        conn.Open();

        var keep = new HashSet<string>(folderPaths, StringComparer.OrdinalIgnoreCase);
        foreach (var movie in GetAllMovies())
        {
            if (keep.Contains(movie.FolderPath)) continue;

            var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Movies WHERE Id = $id;";
            cmd.Parameters.AddWithValue("$id", movie.Id);
            cmd.ExecuteNonQuery();
        }
    }

    private static Movie ReadMovie(SqliteDataReader reader) => new()
    {
        Id = reader.GetInt32(reader.GetOrdinal("Id")),
        Title = reader.GetString(reader.GetOrdinal("Title")),
        FolderPath = reader.GetString(reader.GetOrdinal("FolderPath")),
        VideoPath = GetNullableString(reader, "VideoPath"),
        SubtitlePath = GetNullableString(reader, "SubtitlePath"),
        PosterPath = GetNullableString(reader, "PosterPath"),
        BackdropLocalPath = GetNullableString(reader, "BackdropLocalPath"),
        BackdropUrl = GetNullableString(reader, "BackdropUrl"),
        Overview = GetNullableString(reader, "Overview"),
        TmdbId = GetNullableString(reader, "TmdbId"),
        TrailerUrl = GetNullableString(reader, "TrailerUrl"),
        Rating = reader.IsDBNull(reader.GetOrdinal("Rating")) ? 0 : reader.GetDouble(reader.GetOrdinal("Rating")),
        ReleaseDate = GetNullableString(reader, "ReleaseDate"),
        LastModified = DateTime.TryParse(GetNullableString(reader, "LastModified"), out var lm) ? lm : DateTime.MinValue,
        LastScanned = DateTime.TryParse(GetNullableString(reader, "LastScanned"), out var ls) ? ls : DateTime.MinValue
    };

    private static string? GetNullableString(SqliteDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    // ===================== Cast =====================

    public List<CastMember> GetCastForMovie(int movieId)
    {
        var list = new List<CastMember>();
        using var conn = GetConnection();
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM CastMembers WHERE MovieId = $id ORDER BY SortOrder;";
        cmd.Parameters.AddWithValue("$id", movieId);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new CastMember
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                MovieId = reader.GetInt32(reader.GetOrdinal("MovieId")),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                Character = GetNullableString(reader, "Character"),
                ProfileImageUrl = GetNullableString(reader, "ProfileImageUrl"),
                ProfileLocalPath = GetNullableString(reader, "ProfileLocalPath"),
                SortOrder = reader.IsDBNull(reader.GetOrdinal("SortOrder")) ? 0 : reader.GetInt32(reader.GetOrdinal("SortOrder"))
            });
        }
        return list;
    }

    public void ReplaceCastForMovie(int movieId, List<CastMember> cast)
    {
        using var conn = GetConnection();
        conn.Open();

        var delCmd = conn.CreateCommand();
        delCmd.CommandText = "DELETE FROM CastMembers WHERE MovieId = $id;";
        delCmd.Parameters.AddWithValue("$id", movieId);
        delCmd.ExecuteNonQuery();

        foreach (var member in cast)
        {
            var cmd = conn.CreateCommand();
            cmd.CommandText = """
                INSERT INTO CastMembers (MovieId, Name, Character, ProfileImageUrl, ProfileLocalPath, SortOrder)
                VALUES ($movieId, $name, $character, $url, $local, $order);
                """;
            cmd.Parameters.AddWithValue("$movieId", movieId);
            cmd.Parameters.AddWithValue("$name", member.Name);
            cmd.Parameters.AddWithValue("$character", (object?)member.Character ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$url", (object?)member.ProfileImageUrl ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$local", (object?)member.ProfileLocalPath ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$order", member.SortOrder);
            cmd.ExecuteNonQuery();
        }
    }

    // ===================== Settings (simple key/value) =====================

    public AppSettings GetSettings()
    {
        var settings = new AppSettings();
        using var conn = GetConnection();
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Key, Value FROM Settings;";
        using var reader = cmd.ExecuteReader();

        var dict = new Dictionary<string, string>();
        while (reader.Read())
            dict[reader.GetString(0)] = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);

        if (dict.TryGetValue(nameof(AppSettings.MoviesRootFolder), out var v1)) settings.MoviesRootFolder = v1;
        if (dict.TryGetValue(nameof(AppSettings.ExternalPlayerPath), out var v2)) settings.ExternalPlayerPath = v2;
        if (dict.TryGetValue(nameof(AppSettings.TmdbApiKey), out var v3)) settings.TmdbApiKey = v3;
        if (dict.TryGetValue(nameof(AppSettings.AiProvider), out var v4)) settings.AiProvider = v4;
        if (dict.TryGetValue(nameof(AppSettings.AiApiKey), out var v5)) settings.AiApiKey = v5;
        if (dict.TryGetValue(nameof(AppSettings.AiModel), out var v6)) settings.AiModel = v6;
        if (dict.TryGetValue(nameof(AppSettings.PreferAiSummary), out var v7)) settings.PreferAiSummary = v7 == "true";

        return settings;
    }

    public void SaveSettings(AppSettings settings)
    {
        using var conn = GetConnection();
        conn.Open();

        void SaveKey(string key, string value)
        {
            var cmd = conn.CreateCommand();
            cmd.CommandText = """
                INSERT INTO Settings (Key, Value) VALUES ($key, $value)
                ON CONFLICT(Key) DO UPDATE SET Value = $value;
                """;
            cmd.Parameters.AddWithValue("$key", key);
            cmd.Parameters.AddWithValue("$value", value);
            cmd.ExecuteNonQuery();
        }

        SaveKey(nameof(AppSettings.MoviesRootFolder), settings.MoviesRootFolder);
        SaveKey(nameof(AppSettings.ExternalPlayerPath), settings.ExternalPlayerPath);
        SaveKey(nameof(AppSettings.TmdbApiKey), settings.TmdbApiKey);
        SaveKey(nameof(AppSettings.AiProvider), settings.AiProvider);
        SaveKey(nameof(AppSettings.AiApiKey), settings.AiApiKey);
        SaveKey(nameof(AppSettings.AiModel), settings.AiModel);
        SaveKey(nameof(AppSettings.PreferAiSummary), settings.PreferAiSummary ? "true" : "false");
    }
}
