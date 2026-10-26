using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaSystemAPI.Models
{
    [Table("Movies")]
    public class Movie
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = null!;

        // NVARCHAR(MAX)
        public string? Description { get; set; }

        [MaxLength(100)]
        public string? Director { get; set; }

        [MaxLength(255)]
        public string? Cast { get; set; }

        // CHECK > 0
        public int DurationMinutes { get; set; }

        public DateOnly ReleaseDate { get; set; }

        // CHECK >= ReleaseDate
        public DateOnly EndDate { get; set; }

        [Column(TypeName = "varchar(500)")]
        public string? PosterUrl { get; set; }

        [Column(TypeName = "varchar(500)")]
        public string? TrailerUrl { get; set; }

        // P, C13, C16, C18
        [Column(TypeName = "varchar(10)")]
        public string? AgeRating { get; set; }

        [MaxLength(50)]
        public string? Language { get; set; }

        // Soft delete
        public bool IsDeleted { get; set; } = false;

        [Column(TypeName = "datetime")]
        public DateTime CreatedAt { get; set; }

        [Column(TypeName = "datetime")]
        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<MovieGenre> MovieGenres { get; set; } = new List<MovieGenre>();
        public virtual ICollection<Showtime> Showtimes { get; set; } = new List<Showtime>();
    }
}
