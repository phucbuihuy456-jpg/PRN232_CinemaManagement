namespace CinemaWeb.Models
{
    public class AdminShowtimeItemViewModel
    {
        public int Id { get; set; }
        public int MovieId { get; set; }
        public string MovieTitle { get; set; } = string.Empty;
        public string? PosterUrl { get; set; }
        public int RoomId { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public string ScreenType { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public decimal BasePrice { get; set; }
        public bool IsActive { get; set; } = true;

        public bool IsEnded => EndTime <= DateTime.Now;
        public bool IsUpcoming => StartTime > DateTime.Now;
        public bool IsOngoing => StartTime <= DateTime.Now && EndTime > DateTime.Now;
    }

    public class AdminRoomItemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ScreenType { get; set; } = string.Empty;
        public int TotalSeats { get; set; }
    }

    public class CreateShowtimeRequestModel
    {
        public int MovieId { get; set; }
        public int RoomId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public decimal BasePrice { get; set; }
    }

    public class AdminShowtimesListViewModel
    {
        public List<AdminShowtimeItemViewModel> Showtimes { get; set; } = new();
        public List<MovieItemViewModel> Movies { get; set; } = new();
        public List<AdminRoomItemViewModel> Rooms { get; set; } = new();
        public int? SelectedMovieId { get; set; }
        public string? SelectedDate { get; set; }
    }
}
