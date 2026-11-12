namespace CinemaWeb.Models
{
    public class AdminMovieRequestModel
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Director { get; set; }
        public string? Cast { get; set; }
        public int DurationMinutes { get; set; }
        public DateOnly ReleaseDate { get; set; }
        public DateOnly EndDate { get; set; }
        public string? PosterUrl { get; set; }
        public string? TrailerUrl { get; set; }
        public string? AgeRating { get; set; } = "P";
        public string? Language { get; set; } = "Tiếng Việt";
        public List<int> GenreIds { get; set; } = new();
    }

    public class AdminMoviesListViewModel
    {
        public List<MovieItemViewModel> Movies { get; set; } = new();
        public List<GenreViewModel> Genres { get; set; } = new();
        public string? SearchKeyword { get; set; }
        public int? SelectedGenreId { get; set; }
    }
}
