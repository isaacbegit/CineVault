using System.Windows;
using CineVault.Services;
using CineVault.ViewModels;

namespace CineVault.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel();
        DataContext = _viewModel;
        _viewModel.SettingsRequested += OnSettingsRequested;

        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var db = new DatabaseService();
        var settings = db.GetSettings();

        if (string.IsNullOrWhiteSpace(settings.MoviesRootFolder))
        {
            // First run: ask the user to choose their movies folder via Settings.
            OnSettingsRequested(this, EventArgs.Empty);
        }
        else if (_viewModel.AllMovies.Count == 0)
        {
            // We have a folder configured but an empty database - scan automatically.
            await _viewModel.ScanLibraryAsync();
        }
    }

    private void OnSettingsRequested(object? sender, EventArgs e)
    {
        var settingsWindow = new SettingsWindow { Owner = this };
        settingsWindow.ShowDialog();
        _viewModel.ReloadSettings();
    }
}
