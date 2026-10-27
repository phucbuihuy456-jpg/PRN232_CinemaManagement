namespace CinemaSystemAPI.DTOs.SeatMap
{
    public class CreateShowTimeDto
    {
        public int MovieId { get; set; }
        public int RoomId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public decimal BasePrice { get; set; }
    }
}
