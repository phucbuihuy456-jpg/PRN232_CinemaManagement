using CinemaSystemAPI.Controllers;
using CinemaSystemAPI.DTOs.Bookings;
using CinemaSystemAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace CinemaSystemAPI.Tests
{
    public class BookingsControllerTests
    {
        private CinemaDbContext CreateInMemoryDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<CinemaDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            var context = new CinemaDbContext(options);
            context.Database.EnsureCreated();

            // Seed master data
            if (!context.Rooms.Any())
            {
                var room = new Room { Id = 1, Name = "Phòng 1", TotalSeats = 10, ScreenType = "2D", IsActive = true };
                var movie = new Movie
                {
                    Id = 1,
                    Title = "Test Movie",
                    DurationMinutes = 120,
                    ReleaseDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-10)),
                    EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
                    IsDeleted = false
                };
                var seatType = new SeatType { Id = 1, Name = "Standard", ExtraPrice = 0 };

                context.Rooms.Add(room);
                context.Movies.Add(movie);
                context.SeatTypes.Add(seatType);

                for (int i = 1; i <= 5; i++)
                {
                    context.Seats.Add(new Seat
                    {
                        Id = i,
                        RoomId = 1,
                        RowCode = "A",
                        SeatNumber = i,
                        SeatTypeId = 1,
                        IsActive = true
                    });
                }

                // Showtime in future
                var showtime = new Showtime
                {
                    Id = 1,
                    MovieId = 1,
                    RoomId = 1,
                    StartTime = DateTime.Now.AddHours(2),
                    EndTime = DateTime.Now.AddHours(4),
                    BasePrice = 80000,
                    IsActive = true
                };
                context.Showtimes.Add(showtime);

                var combo = new Combo { Id = 1, Name = "Combo Solo", Price = 85000, IsActive = true };
                context.Combos.Add(combo);

                context.SaveChanges();
            }

            return context;
        }

        [Fact]
        public async Task HoldSeats_ShouldSucceed_WhenSeatsAvailable()
        {
            // Arrange
            using var context = CreateInMemoryDbContext("Db_Hold_Success");
            var controller = new BookingsController(context, NullLogger<BookingsController>.Instance);

            var request = new HoldSeatsRequestDto
            {
                ShowtimeId = 1,
                SeatIds = new List<int> { 1, 2 }
            };

            // Act
            var result = await controller.HoldSeats(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<HoldSeatsResponseDto>(okResult.Value);

            Assert.False(string.IsNullOrEmpty(response.HoldToken));
            Assert.Equal(2, response.HeldSeatIds.Count);
            Assert.Equal(160000, response.TotalSeatPrice);

            // Kiểm tra CSDL
            var savedHolds = await context.SeatHolds.Where(h => h.ShowtimeId == 1).ToListAsync();
            Assert.Equal(2, savedHolds.Count);
        }

        [Fact]
        public async Task HoldSeats_ShouldConflict_WhenSeatAlreadyHeldByAnotherUser()
        {
            // Arrange
            using var context = CreateInMemoryDbContext("Db_Hold_Conflict");
            var controller = new BookingsController(context, NullLogger<BookingsController>.Instance);

            // Người 1 giữ ghế 1
            await controller.HoldSeats(new HoldSeatsRequestDto
            {
                ShowtimeId = 1,
                SeatIds = new List<int> { 1 }
            });

            // Người 2 cố gắng giữ cùng ghế 1
            var request2 = new HoldSeatsRequestDto
            {
                ShowtimeId = 1,
                SeatIds = new List<int> { 1, 2 }
            };

            // Act
            var result2 = await controller.HoldSeats(request2);

            // Assert
            var conflictResult = Assert.IsType<ConflictObjectResult>(result2.Result);
            Assert.NotNull(conflictResult.Value);
        }

        [Fact]
        public async Task HoldSeats_ShouldConflict_WhenSeatAlreadySold()
        {
            // Arrange
            using var context = CreateInMemoryDbContext("Db_Hold_Sold_Conflict");
            var controller = new BookingsController(context, NullLogger<BookingsController>.Instance);

            // Tạo một đơn đặt vé Confirmed cho ghế 1
            var booking = new Booking
            {
                ShowtimeId = 1,
                TotalAmount = 80000,
                Status = "Confirmed",
                PaymentStatus = "Paid",
                CreatedAt = DateTime.Now
            };
            context.Bookings.Add(booking);
            await context.SaveChangesAsync();

            context.BookingSeats.Add(new BookingSeat { BookingId = booking.Id, SeatId = 1, Price = 80000 });
            await context.SaveChangesAsync();

            // Act: Khách hàng cố gắng giữ ghế 1 đã bán
            var result = await controller.HoldSeats(new HoldSeatsRequestDto
            {
                ShowtimeId = 1,
                SeatIds = new List<int> { 1 }
            });

            // Assert
            Assert.IsType<ConflictObjectResult>(result.Result);
        }

        [Fact]
        public async Task CancelBooking_ShouldSucceed_AndFreeSeats()
        {
            // Arrange (Task 32)
            using var context = CreateInMemoryDbContext("Db_Cancel_Success");
            var controller = new BookingsController(context, NullLogger<BookingsController>.Instance);

            var booking = new Booking
            {
                ShowtimeId = 1,
                TotalAmount = 160000,
                Status = "Confirmed",
                PaymentStatus = "Paid",
                CreatedAt = DateTime.Now
            };
            context.Bookings.Add(booking);
            await context.SaveChangesAsync();

            context.BookingSeats.Add(new BookingSeat { BookingId = booking.Id, SeatId = 3, Price = 80000 });
            context.BookingSeats.Add(new BookingSeat { BookingId = booking.Id, SeatId = 4, Price = 80000 });
            context.Payments.Add(new Payment
            {
                BookingId = booking.Id,
                Amount = 160000,
                PaymentMethod = "Cash",
                Status = "Success",
                CreatedAt = DateTime.Now
            });
            await context.SaveChangesAsync();

            // Act
            var cancelResult = await controller.CancelBooking(booking.Id, new CancelBookingRequestDto { Reason = "Khách bận đột xuất" });

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(cancelResult.Result);
            var response = Assert.IsType<CancelBookingResponseDto>(okResult.Value);

            Assert.Equal("Cancelled", response.Status);
            Assert.Equal("Refunded", response.PaymentStatus);
            Assert.Equal(160000, response.RefundAmount);
            Assert.Equal(2, response.FreedSeatNames.Count);

            // Kiểm tra DB: Booking đã chuyển thành Cancelled
            var updatedBooking = await context.Bookings.FindAsync(booking.Id);
            Assert.Equal("Cancelled", updatedBooking!.Status);
            Assert.Equal("Refunded", updatedBooking.PaymentStatus);

            // Ghế 3 và 4 đã được giải phóng -> Thử giữ ghế 3 lại thành công!
            var reholdResult = await controller.HoldSeats(new HoldSeatsRequestDto
            {
                ShowtimeId = 1,
                SeatIds = new List<int> { 3 }
            });
            Assert.IsType<OkObjectResult>(reholdResult.Result);
        }

        [Fact]
        public async Task CancelBooking_ShouldFail_WhenShowtimeAlreadyStarted()
        {
            // Arrange: Suất chiếu trong quá khứ
            using var context = CreateInMemoryDbContext("Db_Cancel_Past_Showtime");
            var controller = new BookingsController(context, NullLogger<BookingsController>.Instance);

            var pastShowtime = new Showtime
            {
                Id = 99,
                MovieId = 1,
                RoomId = 1,
                StartTime = DateTime.Now.AddHours(-3),
                EndTime = DateTime.Now.AddHours(-1),
                BasePrice = 80000,
                IsActive = true
            };
            context.Showtimes.Add(pastShowtime);

            var booking = new Booking
            {
                ShowtimeId = 99,
                TotalAmount = 80000,
                Status = "Confirmed",
                PaymentStatus = "Paid",
                CreatedAt = DateTime.Now.AddHours(-4)
            };
            context.Bookings.Add(booking);
            await context.SaveChangesAsync();

            // Act
            var result = await controller.CancelBooking(booking.Id);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.NotNull(badRequest.Value);
        }

        [Fact]
        public async Task HoldSeats_Concurrency_TwoUsersHoldSameSeatSimultaneously_OnlyOneSucceeds()
        {
            // Arrange (Task 33: Concurrency & Race condition test)
            // Giả lập 2 người dùng gửi request giữ cùng Ghế 5 tại cùng 1 mili-giây
            using var context = CreateInMemoryDbContext("Db_Concurrency_Hold");
            var controller = new BookingsController(context, NullLogger<BookingsController>.Instance);

            var task1 = controller.HoldSeats(new HoldSeatsRequestDto
            {
                ShowtimeId = 1,
                SeatIds = new List<int> { 5 }
            });

            var task2 = controller.HoldSeats(new HoldSeatsRequestDto
            {
                ShowtimeId = 1,
                SeatIds = new List<int> { 5 }
            });

            // Act: Chạy đồng thời 2 task song song
            var results = await Task.WhenAll(task1, task2);

            // Assert: Đúng 1 task thành công (200 OK) và 1 task xung đột (409 Conflict)
            int okCount = results.Count(r => r.Result is OkObjectResult);
            int conflictCount = results.Count(r => r.Result is ConflictObjectResult);

            Assert.Equal(1, okCount);
            Assert.Equal(1, conflictCount);

            // Tính toàn vẹn của SeatHolds trong CSDL: Chỉ có duy nhất 1 bản ghi SeatHold cho Ghế 5
            var holdsForSeat5 = await context.SeatHolds.Where(h => h.ShowtimeId == 1 && h.SeatId == 5).ToListAsync();
            Assert.Single(holdsForSeat5);
        }
    }
}
