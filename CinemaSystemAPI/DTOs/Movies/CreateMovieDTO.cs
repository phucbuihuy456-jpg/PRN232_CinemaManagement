using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaSystemAPI.DTOs.Movies
{
    public class CreateMovieDTO
    {
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public string? Director { get; set; }
        public string? Cast { get; set; }
        public int DurationMinutes { get; set; }
        public DateOnly ReleaseDate { get; set; }
        public DateOnly EndDate { get; set; }
        public string? PosterUrl { get; set; }
        public string? TrailerUrl { get; set; }

        // P, C13, C16, C18
        [Column(TypeName = "varchar(10)")]
        public string? AgeRating { get; set; } = null!;
        public string? Language { get; set; }
        public List<int> GenreIds { get; set; } = new List<int>();
    }
}
