using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaSystemAPI.Models
{
    [Table("Payments")]
    public class Payment
    {
        public int Id { get; set; }

        public int BookingId { get; set; }

        // CHECK >= 0
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        // Cash, BankTransfer, EWallet, CreditCard
        [Required]
        [Column(TypeName = "varchar(30)")]
        public string PaymentMethod { get; set; } = null!;

        // Mã giao dịch từ payment gateway - Unique, Nullable
        [Column(TypeName = "varchar(255)")]
        public string? TransactionId { get; set; }

        // Pending, Success, Failed, Refunded
        [Required]
        [Column(TypeName = "varchar(20)")]
        public string Status { get; set; } = "Pending";

        // Response JSON từ cổng thanh toán - NVARCHAR(MAX)
        public string? GatewayResponse { get; set; }

        [Column(TypeName = "datetime")]
        public DateTime? PaidAt { get; set; }

        [Column(TypeName = "datetime")]
        public DateTime CreatedAt { get; set; }

        public virtual Booking Booking { get; set; } = null!;
    }
}
