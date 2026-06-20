using System.Windows;
using CineVault.Services;
using CineVault.ViewModels;

namespace CineVault.Views;

public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _viewModel;

    public SettingsWindow()
    {
        InitializeComponent();
        _viewModel = new SettingsViewModel(new DatabaseService());
        DataContext = _viewModel;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.SaveCommand.Execute(null);
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
