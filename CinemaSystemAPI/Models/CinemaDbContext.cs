using Microsoft.EntityFrameworkCore;

namespace CinemaSystemAPI.Models
{
    public class CinemaDbContext : DbContext
    {
        public CinemaDbContext(DbContextOptions<CinemaDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Movie> Movies { get; set; } = null!;
        public DbSet<Genre> Genres { get; set; } = null!;
        public DbSet<MovieGenre> MovieGenres { get; set; } = null!;
        public DbSet<Room> Rooms { get; set; } = null!;
        public DbSet<SeatType> SeatTypes { get; set; } = null!;
        public DbSet<Seat> Seats { get; set; } = null!;
        public DbSet<Showtime> Showtimes { get; set; } = null!;
        public DbSet<Booking> Bookings { get; set; } = null!;
        public DbSet<BookingSeat> BookingSeats { get; set; } = null!;
        public DbSet<Combo> Combos { get; set; } = null!;
        public DbSet<BookingCombo> BookingCombos { get; set; } = null!;
        public DbSet<Payment> Payments { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ===== Users =====
            modelBuilder.Entity<User>(e =>
            {
                e.HasIndex(x => x.Email).IsUnique();
                // HasSentinel(true): khi giá trị là true thì để DB tự gán DEFAULT 1, khi gán false thì EF vẫn ghi false.
                e.Property(x => x.IsActive).HasDefaultValue(true).HasSentinel(true);
                e.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");
            });

            // ===== Movies =====
            modelBuilder.Entity<Movie>(e =>
            {
                e.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_Movies_DurationMinutes", "[DurationMinutes] > 0");
                    t.HasCheckConstraint("CK_Movies_EndDate", "[EndDate] >= [ReleaseDate]");
                });
                e.Property(x => x.IsDeleted).HasDefaultValue(false);
                e.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");
                // Soft delete: lọc IsDeleted == false ở tầng Service khi truy vấn danh sách phim.
            });

            // ===== Genres =====
            modelBuilder.Entity<Genre>(e =>
            {
                e.HasIndex(x => x.Name).IsUnique();
            });

            // ===== MovieGenres (Composite PK) =====
            modelBuilder.Entity<MovieGenre>(e =>
            {
                e.HasKey(x => new { x.MovieId, x.GenreId });

                e.HasOne(x => x.Movie)
                    .WithMany(m => m.MovieGenres)
                    .HasForeignKey(x => x.MovieId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.Genre)
                    .WithMany(g => g.MovieGenres)
                    .HasForeignKey(x => x.GenreId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ===== Rooms =====
            modelBuilder.Entity<Room>(e =>
            {
                e.ToTable(t => t.HasCheckConstraint("CK_Rooms_TotalSeats", "[TotalSeats] > 0"));
                e.Property(x => x.IsActive).HasDefaultValue(true).HasSentinel(true);
            });

            // ===== SeatTypes =====
            modelBuilder.Entity<SeatType>(e =>
            {
                e.ToTable(t => t.HasCheckConstraint("CK_SeatTypes_ExtraPrice", "[ExtraPrice] >= 0"));
            });

            // ===== Seats =====
            modelBuilder.Entity<Seat>(e =>
            {
                e.HasIndex(x => new { x.RoomId, x.RowCode, x.SeatNumber }).IsUnique();
                e.Property(x => x.IsActive).HasDefaultValue(true).HasSentinel(true);

                e.HasOne(x => x.Room)
                    .WithMany(r => r.Seats)
                    .HasForeignKey(x => x.RoomId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(x => x.SeatType)
                    .WithMany(st => st.Seats)
                    .HasForeignKey(x => x.SeatTypeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ===== Showtimes =====
            modelBuilder.Entity<Showtime>(e =>
            {
                e.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_Showtimes_EndTime", "[EndTime] > [StartTime]");
                    t.HasCheckConstraint("CK_Showtimes_BasePrice", "[BasePrice] >= 0");
                });
                e.Property(x => x.IsActive).HasDefaultValue(true).HasSentinel(true);
                e.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");

                // Hỗ trợ truy vấn kiểm tra chồng chéo suất chiếu trong cùng phòng
                e.HasIndex(x => new { x.RoomId, x.StartTime, x.EndTime });

                e.HasOne(x => x.Movie)
                    .WithMany(m => m.Showtimes)
                    .HasForeignKey(x => x.MovieId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(x => x.Room)
                    .WithMany(r => r.Showtimes)
                    .HasForeignKey(x => x.RoomId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ===== Bookings =====
            modelBuilder.Entity<Booking>(e =>
            {
                // Unique nullable -> EF tự thêm filter "WHERE [QrCodeToken] IS NOT NULL"
                e.HasIndex(x => x.QrCodeToken).IsUnique();
                e.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");

                e.HasOne(x => x.User)
                    .WithMany(u => u.Bookings)
                    .HasForeignKey(x => x.UserId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(x => x.Showtime)
                    .WithMany(s => s.Bookings)
                    .HasForeignKey(x => x.ShowtimeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ===== BookingSeats =====
            modelBuilder.Entity<BookingSeat>(e =>
            {
                // Một ghế không xuất hiện 2 lần trong cùng 1 đơn
                e.HasIndex(x => new { x.BookingId, x.SeatId }).IsUnique();

                e.HasOne(x => x.Booking)
                    .WithMany(b => b.BookingSeats)
                    .HasForeignKey(x => x.BookingId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.Seat)
                    .WithMany(s => s.BookingSeats)
                    .HasForeignKey(x => x.SeatId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ===== Combos =====
            modelBuilder.Entity<Combo>(e =>
            {
                e.Property(x => x.IsActive).HasDefaultValue(true).HasSentinel(true);
            });

            // ===== BookingCombos (Composite PK) =====
            modelBuilder.Entity<BookingCombo>(e =>
            {
                e.HasKey(x => new { x.BookingId, x.ComboId });
                e.ToTable(t => t.HasCheckConstraint("CK_BookingCombos_Quantity", "[Quantity] > 0"));

                e.HasOne(x => x.Booking)
                    .WithMany(b => b.BookingCombos)
                    .HasForeignKey(x => x.BookingId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.Combo)
                    .WithMany(c => c.BookingCombos)
                    .HasForeignKey(x => x.ComboId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ===== Payments =====
            modelBuilder.Entity<Payment>(e =>
            {
                e.ToTable(t => t.HasCheckConstraint("CK_Payments_Amount", "[Amount] >= 0"));
                // Unique nullable -> EF tự thêm filter "WHERE [TransactionId] IS NOT NULL"
                e.HasIndex(x => x.TransactionId).IsUnique();
                e.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");

                e.HasOne(x => x.Booking)
                    .WithMany(b => b.Payments)
                    .HasForeignKey(x => x.BookingId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
