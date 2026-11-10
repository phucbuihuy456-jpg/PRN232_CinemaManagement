using CinemaSystemAPI.Controllers;
using CinemaSystemAPI.DTOs.Reports;
using CinemaSystemAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace CinemaSystemAPI.Tests
{
    public class ReportsControllerTests
    {
        private CinemaDbContext CreateInMemoryDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<CinemaDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            var context = new CinemaDbContext(options);
            context.Database.EnsureCreated();

            // Seed sample data for testing reports
            var room = new Room { Id = 1, Name = "Phòng IMAX 1", ScreenType = "IMAX", TotalSeats = 20, IsActive = true };
            var movie = new Movie
            {
                Id = 1,
                Title = "Dune 2",
                DurationMinutes = 160,
                ReleaseDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-10)),
                EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
                IsDeleted = false
            };
            var showtime = new Showtime
            {
                Id = 1,
                MovieId = 1,
                RoomId = 1,
                StartTime = DateTime.Today.AddHours(14),
                EndTime = DateTime.Today.AddHours(17),
                BasePrice = 100000,
                IsActive = true
            };

            var user = new User
            {
                Id = 1,
                FullName = "Test Customer",
                Email = "test@customer.com",
                PasswordHash = "hash",
                Role = "Customer",
                CreatedAt = DateTime.Today
            };

            var seat1 = new Seat { Id = 1, RoomId = 1, RowCode = "A", SeatNumber = 1, SeatTypeId = 1, IsActive = true };
            var seat2 = new Seat { Id = 2, RoomId = 1, RowCode = "A", SeatNumber = 2, SeatTypeId = 1, IsActive = true };

            var combo = new Combo { Id = 1, Name = "Bắp Nước", Price = 50000, IsActive = true };

            var booking = new Booking
            {
                Id = 1,
                UserId = 1,
                ShowtimeId = 1,
                TotalAmount = 250000, // 200k vé + 50k combo
                Status = "Confirmed",
                PaymentStatus = "Paid",
                CreatedAt = DateTime.Today
            };

            context.Rooms.Add(room);
            context.Movies.Add(movie);
            context.Showtimes.Add(showtime);
            context.Users.Add(user);
            context.Seats.AddRange(seat1, seat2);
            context.Combos.Add(combo);
            context.Bookings.Add(booking);

            context.BookingSeats.AddRange(
                new BookingSeat { Id = 1, BookingId = 1, SeatId = 1, Price = 100000 },
                new BookingSeat { Id = 2, BookingId = 1, SeatId = 2, Price = 100000 }
            );

            context.BookingCombos.Add(
                new BookingCombo { BookingId = 1, ComboId = 1, Quantity = 1, UnitPrice = 50000 }
            );

            context.SaveChanges();
            return context;
        }

        [Fact]
        public async Task GetRevenueReport_CalculatesAccurately()
        {
            // Arrange
            using var context = CreateInMemoryDbContext("Report_Revenue_Db");
            var controller = new ReportsController(context, NullLogger<ReportsController>.Instance);

            // Act
            var actionResult = await controller.GetRevenueReport(DateTime.Today.AddDays(-1), DateTime.Today.AddDays(1));

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var report = Assert.IsType<RevenueReportDto>(okResult.Value);

            Assert.Equal(250000, report.TotalRevenue);
            Assert.Equal(200000, report.TicketRevenue);
            Assert.Equal(50000, report.ComboRevenue);
            Assert.Equal(2, report.TotalTicketsSold);
            Assert.Equal(1, report.TotalBookingsCount);
            Assert.NotEmpty(report.DailyRevenues);
        }

        [Fact]
        public async Task GetTopMovies_ReturnsRankedMovies()
        {
            // Arrange
            using var context = CreateInMemoryDbContext("Report_TopMovies_Db");
            var controller = new ReportsController(context, NullLogger<ReportsController>.Instance);

            // Act
            var actionResult = await controller.GetTopMovies(limit: 5);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var list = Assert.IsType<List<TopMovieReportDto>>(okResult.Value);

            Assert.Single(list);
            Assert.Equal("Dune 2", list[0].Title);
            Assert.Equal(2, list[0].TotalTicketsSold);
            Assert.Equal(250000, list[0].TotalRevenue);
        }

        [Fact]
        public async Task GetDailySummary_ReturnsTodayMetrics()
        {
            // Arrange
            using var context = CreateInMemoryDbContext("Report_DailySummary_Db");
            var controller = new ReportsController(context, NullLogger<ReportsController>.Instance);

            // Act
            var actionResult = await controller.GetDailySummary();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var summary = Assert.IsType<DailySummaryDto>(okResult.Value);

            Assert.Equal(250000, summary.TodayRevenue);
            Assert.Equal(2, summary.TodayTicketsSold);
            Assert.Equal(1, summary.TodayBookingsCount);
            Assert.Equal(1, summary.ActiveShowtimesCount);
            Assert.Equal(1, summary.NewUsersCount);
        }

        [Fact]
        public async Task GetOccupancyReport_CalculatesRateCorrectly()
        {
            // Arrange
            using var context = CreateInMemoryDbContext("Report_Occupancy_Db");
            var controller = new ReportsController(context, NullLogger<ReportsController>.Instance);

            // Act
            var actionResult = await controller.GetOccupancyReport(DateTime.Today.AddDays(-1), DateTime.Today.AddDays(1));

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var list = Assert.IsType<List<OccupancyReportDto>>(okResult.Value);

            Assert.Single(list);
            Assert.Equal("Phòng IMAX 1", list[0].RoomName);
            Assert.Equal(20, list[0].TotalCapacity);
            Assert.Equal(2, list[0].TotalSeatsBooked);
            Assert.Equal(10.0, list[0].OccupancyRatePercentage); // 2/20 = 10%
        }
    }
}

