using CinemaSystemAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaSystemAPI.Data
{
    public static class DbInitializer
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<CinemaDbContext>();

            // Đảm bảo Database đã được tạo
            context.Database.EnsureCreated();

            // Nếu đã có dữ liệu SeatTypes thì coi như DB đã được seed
            if (context.SeatTypes.Any())
            {
                return;
            }

            // 1. SEAT TYPES
            var seatTypes = new List<SeatType>
            {
                new SeatType { Name = "Standard", ExtraPrice = 0 },
                new SeatType { Name = "VIP", ExtraPrice = 15000 },
                new SeatType { Name = "Couple", ExtraPrice = 30000 }
            };
            context.SeatTypes.AddRange(seatTypes);
            context.SaveChanges();

            // 2. GENRES
            var genres = new List<Genre>
            {
                new Genre { Name = "Hành Động" },
                new Genre { Name = "Kinh Dị" },
                new Genre { Name = "Tình Cảm" },
                new Genre { Name = "Hoạt Hình" },
                new Genre { Name = "Hài Hước" },
                new Genre { Name = "Khoa Học Viễn Tưởng" }
            };
            context.Genres.AddRange(genres);
            context.SaveChanges();

            // 3. ROOMS
            var room1 = new Room
            {
                Name = "Phòng Chiếu 01",
                ScreenType = "2D",
                TotalSeats = 30,
                IsActive = true
            };

            var room2 = new Room
            {
                Name = "Phòng Chiếu 02 (IMAX)",
                ScreenType = "IMAX",
                TotalSeats = 20,
                IsActive = true
            };
            context.Rooms.AddRange(room1, room2);
            context.SaveChanges();

            // 4. SEATS
            var standardType = seatTypes.First(s => s.Name == "Standard");
            var vipType = seatTypes.First(s => s.Name == "VIP");
            var coupleType = seatTypes.First(s => s.Name == "Couple");

            var seats = new List<Seat>();

            // Sinh ghế cho Phòng 1 (30 ghế: Hàng A, B là Standard (mỗi hàng 10 ghế), Hàng C là VIP (10 ghế))
            for (int i = 1; i <= 10; i++)
            {
                seats.Add(new Seat { RoomId = room1.Id, RowCode = "A", SeatNumber = i, SeatTypeId = standardType.Id, IsActive = true });
                seats.Add(new Seat { RoomId = room1.Id, RowCode = "B", SeatNumber = i, SeatTypeId = standardType.Id, IsActive = true });
                seats.Add(new Seat { RoomId = room1.Id, RowCode = "C", SeatNumber = i, SeatTypeId = vipType.Id, IsActive = true });
            }

            // Sinh ghế cho Phòng 2 IMAX (20 ghế: Hàng A, B là VIP (mỗi hàng 8 ghế), Hàng C là Couple (4 ghế đôi))
            for (int i = 1; i <= 8; i++)
            {
                seats.Add(new Seat { RoomId = room2.Id, RowCode = "A", SeatNumber = i, SeatTypeId = vipType.Id, IsActive = true });
                seats.Add(new Seat { RoomId = room2.Id, RowCode = "B", SeatNumber = i, SeatTypeId = vipType.Id, IsActive = true });
            }
            for (int i = 1; i <= 4; i++)
            {
                seats.Add(new Seat { RoomId = room2.Id, RowCode = "C", SeatNumber = i, SeatTypeId = coupleType.Id, IsActive = true });
            }
            context.Seats.AddRange(seats);
            context.SaveChanges();

            // 5. MOVIES
            var movie1 = new Movie
            {
                Title = "Mai",
                Description = "Câu chuyện về cuộc đời của Mai, người phụ nữ chịu nhiều định kiến tìm kiếm tình yêu và hạnh phúc đích thực.",
                Director = "Trấn Thành",
                Cast = "Phương Anh Đào, Tuấn Trần, Trấn Thành, Hồng Đào",
                DurationMinutes = 131,
                ReleaseDate = new DateOnly(2024, 2, 10),
                EndDate = new DateOnly(2024, 5, 30),
                PosterUrl = "https://images.unsplash.com/photo-1489599849927-2ee91cede3ba?w=500",
                TrailerUrl = "https://www.youtube.com/watch?v=example1",
                AgeRating = "C18",
                Language = "Tiếng Việt",
                IsDeleted = false
            };

            var movie2 = new Movie
            {
                Title = "Dune: Hành Tinh Cát - Phần 2",
                Description = "Paul Atreides hợp lực cùng Chani và tộc người Fremen để trả thù những kẻ đã hủy hoại gia tộc mình.",
                Director = "Denis Villeneuve",
                Cast = "Timothée Chalamet, Zendaya, Rebecca Ferguson",
                DurationMinutes = 166,
                ReleaseDate = new DateOnly(2024, 3, 1),
                EndDate = new DateOnly(2024, 6, 1),
                PosterUrl = "https://images.unsplash.com/photo-1536440136628-849c177e76a1?w=500",
                TrailerUrl = "https://www.youtube.com/watch?v=example2",
                AgeRating = "C13",
                Language = "Tiếng Anh - Phụ đề Tiếng Việt",
                IsDeleted = false
            };

            var movie3 = new Movie
            {
                Title = "Kung Fu Panda 4",
                Description = "Po bước vào hành trình mới để trở thành Thủ lĩnh Tinh thần của Thung lũng Bình Yên và tìm kiếm Thần Long Đại Hiệp kế vị.",
                Director = "Mike Mitchell",
                Cast = "Jack Black, Awkwafina, Viola Davis",
                DurationMinutes = 94,
                ReleaseDate = new DateOnly(2024, 3, 8),
                EndDate = new DateOnly(2024, 5, 20),
                PosterUrl = "https://images.unsplash.com/photo-1518709268805-4e9042af9f23?w=500",
                TrailerUrl = "https://www.youtube.com/watch?v=example3",
                AgeRating = "P",
                Language = "Lồng tiếng Việt",
                IsDeleted = false
            };

            context.Movies.AddRange(movie1, movie2, movie3);
            context.SaveChanges();

            // 6. MOVIE GENRES
            var genreAction = genres.First(g => g.Name == "Hành Động");
            var genreRomance = genres.First(g => g.Name == "Tình Cảm");
            var genreSciFi = genres.First(g => g.Name == "Khoa Học Viễn Tưởng");
            var genreAnimation = genres.First(g => g.Name == "Hoạt Hình");
            var genreComedy = genres.First(g => g.Name == "Hài Hước");

            context.MovieGenres.AddRange(
                new MovieGenre { MovieId = movie1.Id, GenreId = genreRomance.Id },
                new MovieGenre { MovieId = movie2.Id, GenreId = genreAction.Id },
                new MovieGenre { MovieId = movie2.Id, GenreId = genreSciFi.Id },
                new MovieGenre { MovieId = movie3.Id, GenreId = genreAnimation.Id },
                new MovieGenre { MovieId = movie3.Id, GenreId = genreComedy.Id }
            );
            context.SaveChanges();

            // 7. USERS (Mật khẩu mặc định: 123456)
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword("123456");

            var adminUser = new User
            {
                FullName = "Quản Trị Viên",
                Email = "admin@cinema.com",
                PasswordHash = hashedPassword,
                PhoneNumber = "0901234567",
                Role = "Admin",
                IsActive = true
            };

            var staffUser = new User
            {
                FullName = "Nhân Viên Bán Vé",
                Email = "staff@cinema.com",
                PasswordHash = hashedPassword,
                PhoneNumber = "0912345678",
                Role = "Staff",
                IsActive = true
            };

            var customerUser = new User
            {
                FullName = "Nguyễn Văn Khách",
                Email = "customer@gmail.com",
                PasswordHash = hashedPassword,
                PhoneNumber = "0987654321",
                Role = "Customer",
                IsActive = true
            };

            context.Users.AddRange(adminUser, staffUser, customerUser);
            context.SaveChanges();

            // 8. COMBOS
            var comboSolo = new Combo
            {
                Name = "Combo Solo",
                Description = "1 Bắp ngọt vừa (60oz) + 1 Nước ngọt vừa (22oz)",
                Price = 79000,
                ImageUrl = "https://images.unsplash.com/photo-1585647347483-22b66260dfff?w=500",
                IsActive = true
            };

            var comboCouple = new Combo
            {
                Name = "Combo Couple",
                Description = "1 Bắp lớn (85oz) + 2 Nước ngọt vừa (22oz)",
                Price = 109000,
                ImageUrl = "https://images.unsplash.com/photo-1572177191856-3cde618dee1f?w=500",
                IsActive = true
            };

            var comboFamily = new Combo
            {
                Name = "Combo Family Đỉnh Cao",
                Description = "2 Bắp phô mai lớn + 4 Nước ngọt lớn + 1 Snack khoai tây",
                Price = 189000,
                ImageUrl = "https://images.unsplash.com/photo-1505686994434-e3cc5abf1330?w=500",
                IsActive = true
            };

            context.Combos.AddRange(comboSolo, comboCouple, comboFamily);
            context.SaveChanges();

            // 9. SHOWTIMES
            var today = DateTime.Today;
            var showtime1 = new Showtime
            {
                MovieId = movie1.Id,
                RoomId = room1.Id,
                StartTime = today.AddHours(14),
                EndTime = today.AddHours(16).AddMinutes(30),
                BasePrice = 85000,
                IsActive = true
            };

            var showtime2 = new Showtime
            {
                MovieId = movie2.Id,
                RoomId = room2.Id,
                StartTime = today.AddHours(18),
                EndTime = today.AddHours(21),
                BasePrice = 120000,
                IsActive = true
            };

            var showtime3 = new Showtime
            {
                MovieId = movie3.Id,
                RoomId = room1.Id,
                StartTime = today.AddHours(9).AddMinutes(30),
                EndTime = today.AddHours(11).AddMinutes(30),
                BasePrice = 75000,
                IsActive = true
            };

            context.Showtimes.AddRange(showtime1, showtime2, showtime3);
            context.SaveChanges();

            // 10. SAMPLE BOOKING (Đơn đặt vé mẫu hoàn tất)
            var bookedSeat1 = seats.First(s => s.RoomId == room1.Id && s.RowCode == "B" && s.SeatNumber == 5);
            var bookedSeat2 = seats.First(s => s.RoomId == room1.Id && s.RowCode == "B" && s.SeatNumber == 6);

            var sampleBooking = new Booking
            {
                UserId = customerUser.Id,
                ShowtimeId = showtime1.Id,
                TotalAmount = (showtime1.BasePrice * 2) + comboSolo.Price, // 2 vé Standard + 1 Combo Solo
                Status = "Confirmed",
                PaymentStatus = "Paid",
                QrCodeToken = Guid.NewGuid().ToString("N")
            };
            context.Bookings.Add(sampleBooking);
            context.SaveChanges();

            // Chi tiết ghế đã đặt
            context.BookingSeats.AddRange(
                new BookingSeat { BookingId = sampleBooking.Id, SeatId = bookedSeat1.Id, Price = showtime1.BasePrice },
                new BookingSeat { BookingId = sampleBooking.Id, SeatId = bookedSeat2.Id, Price = showtime1.BasePrice }
            );

            // Chi tiết combo đã đặt
            context.BookingCombos.Add(new BookingCombo
            {
                BookingId = sampleBooking.Id,
                ComboId = comboSolo.Id,
                Quantity = 1,
                UnitPrice = comboSolo.Price
            });

            // Giao dịch thanh toán mẫu
            context.Payments.Add(new Payment
            {
                BookingId = sampleBooking.Id,
                Amount = sampleBooking.TotalAmount,
                PaymentMethod = "EWallet",
                TransactionId = "TXN_" + DateTime.Now.Ticks,
                Status = "Success",
                GatewayResponse = "{\"status\": \"SUCCESS\", \"bank\": \"MOMO\"}",
                PaidAt = DateTime.Now
            });

            context.SaveChanges();
        }
    }
}
