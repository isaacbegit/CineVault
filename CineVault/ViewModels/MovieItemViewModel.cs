using CineVault.Models;

namespace CineVault.ViewModels;

public class MovieItemViewModel : ViewModelBase
{
    public Movie Movie { get; }

    public MovieItemViewModel(Movie movie)
    {
        Movie = movie;
    }

    public string Title => Movie.Title;
    public string? PosterPath => Movie.PosterPath;
}
