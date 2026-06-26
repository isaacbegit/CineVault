using System.Windows;
using CineVault.Services;
using CineVault.ViewModels;

namespace CineVault.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private ProgressWindow? _progressWindow;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel();
        DataContext = _viewModel;
        _viewModel.SettingsRequested += OnSettingsRequested;
        _viewModel.ScanStarted  += (_, _) => { _progressWindow = new ProgressWindow(_viewModel) { Owner = this }; _progressWindow.Show(); };
        _viewModel.ScanFinished += (_, _) => { _progressWindow?.Close(); _progressWindow = null; };

        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var db = new DatabaseService();
        var settings = db.GetSettings();

        if (string.IsNullOrWhiteSpace(settings.MoviesRootFolder))
            OnSettingsRequested(this, EventArgs.Empty);
        else if (_viewModel.AllMovies.Count == 0)
            await _viewModel.ScanLibraryAsync();
    }

    private void OnSettingsRequested(object? sender, EventArgs e)
    {
        var settingsWindow = new SettingsWindow { Owner = this };
        settingsWindow.ShowDialog();
        _viewModel.ReloadSettings();
    }

    // ── Custom title-bar controls ─────────────────────────────────────────────
    private void Minimize_Click(object sender, RoutedEventArgs e)
        => WindowState = WindowState.Minimized;

    private void Maximize_Click(object sender, RoutedEventArgs e)
        => WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal : WindowState.Maximized;

    private void CloseWindow_Click(object sender, RoutedEventArgs e) => Close();

    protected override void OnStateChanged(EventArgs e)
    {
        base.OnStateChanged(e);
        if (MaximizeButton != null)
            MaximizeButton.Content = WindowState == WindowState.Maximized ? "❐" : "□";
    }
}
