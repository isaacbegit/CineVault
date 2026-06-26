using System.Windows;
using CineVault.ViewModels;

namespace CineVault.Views;

public partial class ProgressWindow : Window
{
    public ProgressWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
