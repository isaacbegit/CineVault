using System.Collections.ObjectModel;
using System.Windows;
using CineVault.Models;
using CineVault.Services;

namespace CineVault.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly DatabaseService _db;
    private readonly LibraryService _library;
    private readonly ExternalPlayerService _player;

    private AppSettings _settings;

    private MovieItemViewModel? _selectedMovie;
    public MovieItemViewModel? SelectedMovie
    {
        get => _selectedMovie;
        set
        {
            if (SetProperty(ref _selectedMovie, value))
            {
                OnPropertyChanged(nameof(HasSelection));
                _ = LoadMovieDetailsAsync(value);
            }
        }
    }

    public bool HasSelection => SelectedMovie != null;

    private bool _isLoadingDetails;
    public bool IsLoadingDetails
    {
        get => _isLoadingDetails;
        set => SetProperty(ref _isLoadingDetails, value);
    }



    public ObservableCollection<MovieItemViewModel> AllMovies { get; } = new();
    public ObservableCollection<MovieItemViewModel> FilteredMovies { get; } = new();
    public ObservableCollection<CastMemberViewModel> Cast { get; } = new();

  

  
    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
                ApplyFilter();
        }
    }

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    private string _statusText = "Ready";
    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public RelayCommand ScanLibraryCommand { get; }
    public RelayCommand PlayMovieCommand { get; }
    public RelayCommand OpenTrailerCommand { get; }
    public RelayCommand OpenSettingsCommand { get; }

    /// <summary>Raised when the UI should show the Settings window (e.g. on first run, or gear button).</summary>
    public event EventHandler? SettingsRequested;

    public MainViewModel()
    {
        _db = new DatabaseService();
        _library = new LibraryService(_db);
        _player = new ExternalPlayerService();
        _settings = _db.GetSettings();

        ScanLibraryCommand = new RelayCommand(async () => await ScanLibraryAsync(), () => !IsBusy);
        PlayMovieCommand = new RelayCommand(PlayMovie, _ => SelectedMovie != null);
        OpenTrailerCommand = new RelayCommand(OpenTrailer, _ => !string.IsNullOrEmpty(SelectedMovie?.Movie.TrailerUrl));
        OpenSettingsCommand = new RelayCommand(() => SettingsRequested?.Invoke(this, EventArgs.Empty));

        LoadFromDatabase();
    }

    public void ReloadSettings() => _settings = _db.GetSettings();

    private void LoadFromDatabase()
    {
        AllMovies.Clear();
        foreach (var movie in _library.LoadFromDatabase())
            AllMovies.Add(new MovieItemViewModel(movie));

        ApplyFilter();
    }

    private void ApplyFilter()
    {
        FilteredMovies.Clear();
        var query = string.IsNullOrWhiteSpace(SearchText)
            ? AllMovies
            : AllMovies.Where(m => m.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

        foreach (var movie in query)
            FilteredMovies.Add(movie);
    }

    public async Task ScanLibraryAsync()
    {
        ReloadSettings();

        if (string.IsNullOrWhiteSpace(_settings.MoviesRootFolder))
        {
            MessageBox.Show("Please set the movies root folder in Settings first.", "CineVault",
                MessageBoxButton.OK, MessageBoxImage.Information);
            SettingsRequested?.Invoke(this, EventArgs.Empty);
            return;
        }

        IsBusy = true;
        StatusText = "Scanning library...";

        var progress = new Progress<string>(msg => StatusText = msg);

        try
        {
            var movies = await _library.RescanAndUpdateAsync(_settings, progress);

            AllMovies.Clear();
            foreach (var movie in movies)
                AllMovies.Add(new MovieItemViewModel(movie));

            ApplyFilter();
            StatusText = $"Library updated — {movies.Count} movie(s) found.";
        }
        catch (Exception ex)
        {
            StatusText = "Scan failed.";
            MessageBox.Show($"Error scanning library: {ex.Message}", "CineVault",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadMovieDetailsAsync(MovieItemViewModel? selected)
    {
        Cast.Clear();
        if (selected == null) return;

        foreach (var member in _library.LoadCast(selected.Movie.Id))
            Cast.Add(new CastMemberViewModel(member));

        ReloadSettings();
        IsLoadingDetails = true;
        try
        {
            await _library.EnrichMovieAsync(selected.Movie, _settings);

            Cast.Clear();
            foreach (var member in _library.LoadCast(selected.Movie.Id))
                Cast.Add(new CastMemberViewModel(member));

            OnPropertyChanged(nameof(SelectedMovie));
        }
        catch (Exception ex)
        {
            StatusText = $"Could not load AI/TMDb details: {ex.Message}";
        }
        finally
        {
            IsLoadingDetails = false;
        }
    }

    private void PlayMovie(object? _)
    {
        if (SelectedMovie?.Movie.VideoPath == null) return;
        ReloadSettings();
        _player.Play(_settings.ExternalPlayerPath, SelectedMovie.Movie.VideoPath);
    }

    private void OpenTrailer(object? _)
    {
        if (SelectedMovie?.Movie.TrailerUrl == null) return;

        var trailerWindow = new Views.TrailerWindow(SelectedMovie.Movie.Title, SelectedMovie.Movie.TrailerUrl)
        {
            Owner = Application.Current.MainWindow
        };
        trailerWindow.Show();
    }
}
