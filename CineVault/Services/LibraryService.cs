using CineVault.Models;

namespace CineVault.Services;

/// <summary>
/// Coordinates a full library scan: walk the disk, enrich each new/changed movie
/// with TMDb (and optionally AI) metadata, cache images locally, and persist everything.
/// This is the single entry point the UI talks to for "Update Library".
/// </summary>
public class LibraryService
{
    private readonly DatabaseService _db;
    private readonly MovieScannerService _scanner;
    private readonly ImageCacheService _imageCache;
    private GeminiAiService geminiAiService;
    private AppSettings appSettings;

    public LibraryService(DatabaseService db)
    {
        _db = db;
        _scanner = new MovieScannerService();
        _imageCache = new ImageCacheService();
        appSettings = _db.GetSettings();

        geminiAiService = new GeminiAiService(appSettings.AiApiKey, appSettings.AiModel);
    }

    public List<Movie> LoadFromDatabase() => _db.GetAllMovies();

    public List<CastMember> LoadCast(int movieId) => _db.GetCastForMovie(movieId);



    private async Task EnrichFromTmdbAsync(Movie movie, TmdbMovieDetails details, AppSettings settings)
    {
        movie.TmdbId = details.Id.ToString();
        movie.Overview = details.Overview;
        movie.Rating = details.VoteAverage;
        movie.ReleaseDate = details.ReleaseDate;
        movie.BackdropUrl = TmdbService.BuildImageUrl(details.BackdropPath, "w1280");
        movie.TrailerUrl = TmdbService.FindTrailerUrl(details);

        if (settings.PreferAiSummary || string.IsNullOrWhiteSpace(movie.Overview))
        {
            var movieInfo = await geminiAiService.GetMovieInfoAsync(movie.Title);
            if (movieInfo != null)
            {
                movie.Overview = movieInfo.SummaryEnglish;
                movie.Title = movieInfo.OfficialMovieName;
            }

        }

        movie.BackdropLocalPath = await _imageCache.DownloadAndCacheAsync(movie.BackdropUrl, $"backdrop_{movie.TmdbId}");

        var movieId = _db.UpsertMovie(movie);

        var castList = new List<CastMember>();
        if (details.Credits?.Cast != null)
        {
            foreach (var c in details.Credits.Cast.OrderBy(c => c.Order).Take(5))
            {
                var profileUrl = TmdbService.BuildImageUrl(c.ProfilePath, "w185");
                var localPath = await _imageCache.DownloadAndCacheAsync(profileUrl, $"cast_{c.Name}_{movieId}");

                castList.Add(new CastMember
                {
                    MovieId = movieId,
                    Name = c.Name,
                    Character = c.Character,
                    ProfileImageUrl = profileUrl,
                    ProfileLocalPath = localPath,
                    SortOrder = c.Order
                });
            }
        }

        _db.ReplaceCastForMovie(movieId, castList);
    }

    private static void CopyOnlineMetadata(Movie from, Movie to)
    {
        to.TmdbId = from.TmdbId;
        to.Overview = from.Overview;
        to.Rating = from.Rating;
        to.ReleaseDate = from.ReleaseDate;
        to.BackdropUrl = from.BackdropUrl;
        to.BackdropLocalPath = from.BackdropLocalPath;
        to.TrailerUrl = from.TrailerUrl;
    }


    /// <summary>
    /// Fast, local-only scan: just discovers movies on disk and upserts them.
    /// No TMDb/AI calls here - that now happens lazily in EnrichMovieAsync
    /// when the user clicks a thumbnail, so "Update Library" stays quick even for large folders.
    /// </summary>
    public async Task<List<Movie>> RescanAndUpdateAsync(AppSettings settings, IProgress<string>? progress = null)
    {
        var scanned = _scanner.ScanLibrary(settings.MoviesRootFolder);
        _db.DeleteMoviesNotIn(scanned.Select(m => m.FolderPath));

        foreach (var movie in scanned)
        {
            progress?.Report($"Found {movie.Title}");

            var existing = _db.GetMovieByFolder(movie.FolderPath);
            if (existing != null)
                CopyOnlineMetadata(from: existing, to: movie);

            _db.UpsertMovie(movie);
        }

        await Task.CompletedTask;
        return _db.GetAllMovies();
    }

    /// <summary>
    /// Fetches/refreshes TMDb (+ optional AI) metadata for a single movie.
    /// Called when the user clicks a thumbnail. Skips the network call entirely if the
    /// movie was already enriched before, unless forceRefresh is true.
    /// </summary>
    public async Task<Movie> EnrichMovieAsync(Movie movie, AppSettings settings, bool forceRefresh = false)
    {
        if (!forceRefresh && !string.IsNullOrWhiteSpace(movie.Overview))
            return movie;

        var tmdb = new TmdbService(settings.TmdbApiKey);


        if (tmdb.IsConfigured)
        {
            var details = await tmdb.FindMovieDetailsAsync(movie.Title);
            if (details != null)
            {
                await EnrichFromTmdbAsync(movie, details, settings);
                return movie;
            }
        }

        if (settings.AiProvider != "None")
        {
            var movieInfo = await geminiAiService.GetMovieInfoAsync(movie.Title);
            if (movieInfo != null)
            {
                movie.Overview = movieInfo.SummaryEnglish;
                movie.Title = movieInfo.OfficialMovieName;


                _db.UpsertMovie(movie);
                return movie;
            }
            else
                return null;



        }
        return null;


    }
}
