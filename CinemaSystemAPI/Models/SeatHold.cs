using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaSystemAPI.Models
{
    // Quản lý giữ chỗ ghế tạm thời (5 phút) trước khi thanh toán
    [Table("SeatHolds")]
    public class SeatHold
    {
        public int Id { get; set; }

        public int ShowtimeId { get; set; }

        public int SeatId { get; set; }

        // NULL khi khách vãng lai hoặc chưa đăng nhập
        public int? UserId { get; set; }

        [MaxLength(100)]
        [Column(TypeName = "varchar(100)")]
        public string? SessionId { get; set; }

        [Column(TypeName = "datetime")]
        public DateTime HeldAt { get; set; }

        [Column(TypeName = "datetime")]
        public DateTime ExpiresAt { get; set; }

        public virtual Showtime Showtime { get; set; } = null!;
        public virtual Seat Seat { get; set; } = null!;
        public virtual User? User { get; set; }
    }
}
