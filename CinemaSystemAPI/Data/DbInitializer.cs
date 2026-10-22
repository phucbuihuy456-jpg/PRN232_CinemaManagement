using CinemaSystemAPI.Models;

namespace CinemaSystemAPI.Data
{
    public static class DbInitializer
    {
        public static void SeedData(CinemaDbContext context)
        {
            // 1. Seed Combos
            if (!context.Combos.Any())
            {
                var sampleCombos = new List<Combo>
                {
                    new Combo
                    {
                        Name = "Combo Solo",
                        Description = "1 Bắp ngọt vừa + 1 Nước ngọt có ga vừa",
                        Price = 85000,
                        ImageUrl = "https://images.unsplash.com/photo-1585647347384-2593bc35786b?auto=format&fit=crop&w=500&q=80",
                        IsActive = true
                    },
                    new Combo
                    {
                        Name = "Combo Couple",
                        Description = "1 Bắp ngọt lớn + 2 Nước ngọt có ga lớn",
                        Price = 119000,
                        ImageUrl = "https://images.unsplash.com/photo-1578849278619-e73505e9610f?auto=format&fit=crop&w=500&q=80",
                        IsActive = true
                    },
                    new Combo
                    {
                        Name = "Combo Party Family",
                        Description = "2 Bắp ngọt lớn + 4 Nước ngọt có ga lớn + 1 Khoai tây lắc phô mai",
                        Price = 209000,
                        ImageUrl = "https://images.unsplash.com/photo-1505686994434-e3cc5abf1330?auto=format&fit=crop&w=500&q=80",
                        IsActive = true
                    },
                    new Combo
                    {
                        Name = "Combo VIP Caramel Cheese",
                        Description = "1 Bắp phô mai/caramel lớn + 2 Nước ép hoa quả",
                        Price = 139000,
                        ImageUrl = "https://images.unsplash.com/photo-1512149673953-1e251807ec7c?auto=format&fit=crop&w=500&q=80",
                        IsActive = true
                    }
                };

                context.Combos.AddRange(sampleCombos);
                context.SaveChanges();
            }

            // 2. Seed SeatTypes
            if (!context.SeatTypes.Any())
            {
                var standard = new SeatType { Name = "Standard", ExtraPrice = 0 };
                var vip = new SeatType { Name = "VIP", ExtraPrice = 20000 };
                var couple = new SeatType { Name = "Couple", ExtraPrice = 40000 };

                context.SeatTypes.AddRange(standard, vip, couple);
                context.SaveChanges();
            }

            // 3. Seed Rooms & Seats
            if (!context.Rooms.Any())
            {
                var room = new Room
                {
                    Name = "Phòng Chiếu 01",
                    ScreenType = "2D Digital",
                    TotalSeats = 30,
                    IsActive = true
                };
                context.Rooms.Add(room);
                context.SaveChanges();

                var standardType = context.SeatTypes.First(st => st.Name == "Standard");
                var vipType = context.SeatTypes.First(st => st.Name == "VIP");
                var coupleType = context.SeatTypes.First(st => st.Name == "Couple");

                var seats = new List<Seat>();

                // Hàng A (1-10): Ghế Standard
                for (int i = 1; i <= 10; i++)
                {
                    seats.Add(new Seat
                    {
                        RoomId = room.Id,
                        RowCode = "A",
                        SeatNumber = i,
                        SeatTypeId = standardType.Id,
                        IsActive = true
                    });
                }

                // Hàng B (1-10): Ghế VIP
                for (int i = 1; i <= 10; i++)
                {
                    seats.Add(new Seat
                    {
                        RoomId = room.Id,
                        RowCode = "B",
                        SeatNumber = i,
                        SeatTypeId = vipType.Id,
                        IsActive = true
                    });
                }

                // Hàng C (1-10): Ghế Couple, ghế C10 IsActive = false (Disabled - đang bảo trì)
                for (int i = 1; i <= 10; i++)
                {
                    seats.Add(new Seat
                    {
                        RoomId = room.Id,
                        RowCode = "C",
                        SeatNumber = i,
                        SeatTypeId = coupleType.Id,
                        IsActive = i != 10 // C10 bảo trì
                    });
                }

                context.Seats.AddRange(seats);
                context.SaveChanges();
            }

            // 4. Seed Movies & Genres
            if (!context.Movies.Any())
            {
                var genreAction = new Genre { Name = "Hành Động" };
                var genreComedy = new Genre { Name = "Hài Hước" };
                var genreSciFi = new Genre { Name = "Khoa Học Viễn Tưởng" };

                context.Genres.AddRange(genreAction, genreComedy, genreSciFi);
                context.SaveChanges();

                var movie = new Movie
                {
                    Title = "Deadpool & Wolverine",
                    Description = "Cuộc hội ngộ hài hước và bùng nổ giữa hai nhân vật huyền thoại của vũ trụ Marvel.",
                    Director = "Shawn Levy",
                    Cast = "Ryan Reynolds, Hugh Jackman, Emma Corrin",
                    DurationMinutes = 128,
                    ReleaseDate = new DateOnly(2026, 7, 26),
                    EndDate = new DateOnly(2026, 12, 31),
                    PosterUrl = "https://images.unsplash.com/photo-1536440136628-849c177e76a1?auto=format&fit=crop&w=500&q=80",
                    TrailerUrl = "https://www.youtube.com/watch?v=73_1biulkYk",
                    AgeRating = "C18",
                    Language = "Tiếng Anh - Phụ đề Tiếng Việt",
                    IsDeleted = false
                };
                context.Movies.Add(movie);
                context.SaveChanges();

                context.MovieGenres.AddRange(
                    new MovieGenre { MovieId = movie.Id, GenreId = genreAction.Id },
                    new MovieGenre { MovieId = movie.Id, GenreId = genreComedy.Id },
                    new MovieGenre { MovieId = movie.Id, GenreId = genreSciFi.Id }
                );
                context.SaveChanges();
            }

            // 5. Seed Showtimes
            if (!context.Showtimes.Any())
            {
                var movie = context.Movies.First();
                var room = context.Rooms.First();
                var today = DateTime.Today;

                var showtime = new Showtime
                {
                    MovieId = movie.Id,
                    RoomId = room.Id,
                    StartTime = today.AddHours(19), // 19:00 hôm nay
                    EndTime = today.AddHours(21).AddMinutes(15), // 21:15 hôm nay
                    BasePrice = 80000,
                    IsActive = true
                };

                context.Showtimes.Add(showtime);
                context.SaveChanges();

                // 6. Seed một booking mẫu đã bán (Sold) cho ghế B5, B6
                var seats = context.Seats.Where(s => s.RoomId == room.Id).ToList();
                var seatB5 = seats.FirstOrDefault(s => s.RowCode == "B" && s.SeatNumber == 5);
                var seatB6 = seats.FirstOrDefault(s => s.RowCode == "B" && s.SeatNumber == 6);

                if (seatB5 != null && seatB6 != null)
                {
                    var booking = new Booking
                    {
                        ShowtimeId = showtime.Id,
                        TotalAmount = (showtime.BasePrice + 20000) * 2, // 200.000 VNĐ
                        Status = "Confirmed",
                        PaymentStatus = "Paid",
                        QrCodeToken = Guid.NewGuid().ToString("N")
                    };
                    context.Bookings.Add(booking);
                    context.SaveChanges();

                    context.BookingSeats.AddRange(
                        new BookingSeat { BookingId = booking.Id, SeatId = seatB5.Id, Price = showtime.BasePrice + 20000 },
                        new BookingSeat { BookingId = booking.Id, SeatId = seatB6.Id, Price = showtime.BasePrice + 20000 }
                    );
                    context.SaveChanges();
                }

                // 7. Seed một SeatHold đang giữ (Held) cho ghế B7 (còn hạn 5 phút)
                var seatB7 = seats.FirstOrDefault(s => s.RowCode == "B" && s.SeatNumber == 7);
                if (seatB7 != null)
                {
                    context.SeatHolds.Add(new SeatHold
                    {
                        ShowtimeId = showtime.Id,
                        SeatId = seatB7.Id,
                        SessionId = "demo_session_123",
                        HeldAt = DateTime.Now,
                        ExpiresAt = DateTime.Now.AddMinutes(5)
                    });
                    context.SaveChanges();
                }
            }
        }
    }
}
