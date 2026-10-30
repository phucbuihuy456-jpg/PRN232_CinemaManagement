namespace CinemaWeb.Models
{
    public class GenreViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
    }

    public class MovieGenreItemViewModel
    {
        public int MovieId { get; set; }
        public int GenreId { get; set; }
        public GenreViewModel? Genre { get; set; }
    }

    public class MovieItemViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public string? Director { get; set; }
        public string? Cast { get; set; }
        public int DurationMinutes { get; set; }
        public DateOnly ReleaseDate { get; set; }
        public DateOnly EndDate { get; set; }
        public string? PosterUrl { get; set; }
        public string? TrailerUrl { get; set; }
        public string? AgeRating { get; set; }
        public string? Language { get; set; }
        public List<MovieGenreItemViewModel> MovieGenres { get; set; } = new();

        public bool IsNowShowing
        {
            get
            {
                var today = DateOnly.FromDateTime(DateTime.Today);
                return ReleaseDate <= today && today <= EndDate;
            }
        }

        public bool IsComingSoon
        {
            get
            {
                var today = DateOnly.FromDateTime(DateTime.Today);
                return ReleaseDate > today;
            }
        }
    }

    public class HomeIndexViewModel
    {
        public List<MovieItemViewModel> BannerMovies { get; set; } = new();
        public List<MovieItemViewModel> NowShowingMovies { get; set; } = new();
        public List<MovieItemViewModel> ComingSoonMovies { get; set; } = new();
        public List<GenreViewModel> Genres { get; set; } = new();
        public int? SelectedGenreId { get; set; }
    }
}
