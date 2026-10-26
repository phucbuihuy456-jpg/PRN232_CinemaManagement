using CinemaSystemAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaSystemAPI.Services
{
    /// <summary>
    /// Background Service chạy định kỳ mỗi 30 giây để quét và xóa các bản ghi SeatHolds đã hết hạn (ExpiresAt <= NOW).
    /// Giúp tự động giải phóng ghế đang giữ tạm để người dùng khác có thể chọn.
    /// </summary>
    public class ExpiredSeatHoldCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ExpiredSeatHoldCleanupService> _logger;
        private readonly TimeSpan _period = TimeSpan.FromSeconds(30);

        public ExpiredSeatHoldCleanupService(
            IServiceProvider serviceProvider,
            ILogger<ExpiredSeatHoldCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[ExpiredSeatHoldCleanupService] Dịch vụ quét dọn giữ ghế quá hạn đã khởi động (chu kỳ 30s).");

            using var timer = new PeriodicTimer(_period);

            while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await CleanupExpiredHoldsAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[ExpiredSeatHoldCleanupService] Lỗi khi dọn dẹp các bản ghi giữ ghế quá hạn.");
                }
            }

            _logger.LogInformation("[ExpiredSeatHoldCleanupService] Dịch vụ quét dọn giữ ghế quá hạn đang dừng.");
        }

        private async Task CleanupExpiredHoldsAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<CinemaDbContext>();

            var now = DateTime.Now;

            // Tìm và xóa các SeatHold đã quá hạn
            var expiredHolds = await context.SeatHolds
                .Where(h => h.ExpiresAt <= now)
                .ToListAsync(cancellationToken);

            if (expiredHolds.Count != 0)
            {
                context.SeatHolds.RemoveRange(expiredHolds);
                var deletedCount = await context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("[ExpiredSeatHoldCleanupService] Đã tự động dọn dẹp {Count} bản ghi giữ ghế hết hạn vào lúc {Time:yyyy-MM-dd HH:mm:ss}.", deletedCount, now);
            }
        }
    }
}
