using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaSystemAPI.DTOs.SeatMap
{
    public class ShowTimeDetailDto
    {
        public int Id { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public decimal BasePrice { get; set; }
        public bool IsActive { get; set; }
        public int MovieId { get; set; }
        public string? Title { get; set; }
        public string? PosterUrl { get; set; }
        public string? AgeRating { get; set; }
        public int DurationMinutes { get; set; }
        public string? Language { get; set; }
        public int RoomId { get; set; }
        public string? RoomName { get; set; }
        public string? ScreenType { get; set; }
        public int TotalSeats { get; set; }
        public int AvailableSeats { get; set; }
    }
}
