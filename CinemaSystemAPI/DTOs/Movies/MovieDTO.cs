using CinemaSystemAPI.Models;

namespace CinemaSystemAPI.DTOs.Movies
{
    public class MovieDTO
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
        public virtual ICollection<MovieGenre> MovieGenres { get; set; } = new List<MovieGenre>();
        public List<string> Genres { get; set; } = new();

        public bool IsNowShowing
        {
            get
            {
                var today = DateOnly.FromDateTime(DateTime.Today);
                return ReleaseDate <= today && today <= EndDate;
            }
        }
    }
}
