using CineVault.Models;
using CineVault.Services;
using Microsoft.Win32;

namespace CineVault.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private readonly DatabaseService _db;

    private string _moviesRootFolder = string.Empty;
    public string MoviesRootFolder
    {
        get => _moviesRootFolder;
        set => SetProperty(ref _moviesRootFolder, value);
    }

    private string _externalPlayerPath = string.Empty;
    public string ExternalPlayerPath
    {
        get => _externalPlayerPath;
        set => SetProperty(ref _externalPlayerPath, value);
    }

    private string _tmdbApiKey = string.Empty;
    public string TmdbApiKey
    {
        get => _tmdbApiKey;
        set => SetProperty(ref _tmdbApiKey, value);
    }

    public List<string> AiProviders { get; } = new() { "None", "Google Gemini" };

    private string _aiProvider = "None";
    public string AiProvider
    {
        get => _aiProvider;
        set => SetProperty(ref _aiProvider, value);
    }

    private string _aiApiKey = string.Empty;
    public string AiApiKey
    {
        get => _aiApiKey;
        set => SetProperty(ref _aiApiKey, value);
    }

    private string _aiModel = "gemini-2.0-flash";
    public string AiModel
    {
        get => _aiModel;
        set => SetProperty(ref _aiModel, value);
    }

    private bool _preferAiSummary;
    public bool PreferAiSummary
    {
        get => _preferAiSummary;
        set => SetProperty(ref _preferAiSummary, value);
    }

    public RelayCommand BrowseFolderCommand { get; }
    public RelayCommand BrowsePlayerCommand { get; }
    public RelayCommand SaveCommand { get; }

    public event EventHandler? SaveCompleted;

    public SettingsViewModel(DatabaseService db)
    {
        _db = db;
        BrowseFolderCommand = new RelayCommand(BrowseFolder);
        BrowsePlayerCommand = new RelayCommand(BrowsePlayer);
        SaveCommand = new RelayCommand(Save);

        Load();
    }

    private void Load()
    {
        var settings = _db.GetSettings();
        MoviesRootFolder = settings.MoviesRootFolder;
        ExternalPlayerPath = settings.ExternalPlayerPath;
        TmdbApiKey = settings.TmdbApiKey;
        AiProvider = settings.AiProvider;
        AiApiKey = settings.AiApiKey;
        AiModel = settings.AiModel;
        PreferAiSummary = settings.PreferAiSummary;
    }

    private void BrowseFolder(object? _)
    {
        var dialog = new OpenFolderDialog { Title = "Select Movies Root Folder" };
        if (dialog.ShowDialog() == true)
            MoviesRootFolder = dialog.FolderName;
    }

    private void BrowsePlayer(object? _)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select External Player Executable",
            Filter = "Executable Files (*.exe)|*.exe|All Files (*.*)|*.*"
        };
        if (dialog.ShowDialog() == true)
            ExternalPlayerPath = dialog.FileName;
    }

    private void Save(object? _)
    {
        var settings = new AppSettings
        {
            MoviesRootFolder = MoviesRootFolder,
            ExternalPlayerPath = ExternalPlayerPath,
            TmdbApiKey = TmdbApiKey,
            AiProvider = AiProvider,
            AiApiKey = AiApiKey,
            AiModel = AiModel,
            PreferAiSummary = PreferAiSummary
        };
        _db.SaveSettings(settings);
        SaveCompleted?.Invoke(this, EventArgs.Empty);
    }
}
