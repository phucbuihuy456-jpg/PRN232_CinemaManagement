using CinemaSystemAPI.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaSystemAPI.DTOs.Movies
{
    public class MovieDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string? PosterUrl { get; set; }
        public string? AgeRating { get; set; }
        public int DurationMinutes { get; set; }
        public DateOnly ReleaseDate { get; set; }
        public virtual ICollection<MovieGenre> MovieGenres { get; set; } = new List<MovieGenre>();
        public string? TrailerUrl { get; set; }
    }
}
