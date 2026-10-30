using CinemaWeb.Models;
using CinemaWeb.Services;
using Microsoft.AspNetCore.Mvc;

namespace CinemaWeb.Controllers
{
    public class HomeController : Controller
    {
        private readonly IMovieApiClient _movieApiClient;
        private readonly ILogger<HomeController> _logger;

        public HomeController(IMovieApiClient movieApiClient, ILogger<HomeController> logger)
        {
            _movieApiClient = movieApiClient;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] int? genreId)
        {
            var genresTask = _movieApiClient.GetGenresAsync();
            var moviesTask = _movieApiClient.GetMoviesFromODataAsync(genreId);

            await Task.WhenAll(genresTask, moviesTask);

            var genres = await genresTask;
            var movies = await moviesTask;

            var today = DateOnly.FromDateTime(DateTime.Today);

            // Phân loại phim
            // Phim đang chiếu: ngày chiếu <= ngày hiện tại VÀ ngày kết thúc >= ngày hiện tại
            var nowShowing = movies.Where(m => m.ReleaseDate <= today && today <= m.EndDate).ToList();

            // Phim sắp chiếu: ngày chiếu > ngày hiện tại
            var comingSoon = movies.Where(m => m.ReleaseDate > today).ToList();

            // Nếu dữ liệu mẫu hạt giống có ngày quá khứ, đưa phim chưa hết hạn hoặc tất cả vào để người dùng luôn có phim trải nghiệm
            if (!nowShowing.Any() && !comingSoon.Any() && movies.Any())
            {
                nowShowing = movies;
            }

            // Banner nổi bật: lấy tối đa 5 phim đầu tiên (ưu tiên phim có poster đẹp)
            var bannerMovies = movies.Take(5).ToList();

            var viewModel = new HomeIndexViewModel
            {
                BannerMovies = bannerMovies,
                NowShowingMovies = nowShowing,
                ComingSoonMovies = comingSoon,
                Genres = genres,
                SelectedGenreId = genreId
            };

            return View(viewModel);
        }

        /// <summary>
        /// Action phụ hỗ trợ lọc phim nhanh qua Ajax không cần tải lại toàn bộ trang
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> FilterMovies([FromQuery] int? genreId)
        {
            var movies = await _movieApiClient.GetMoviesFromODataAsync(genreId);
            var today = DateOnly.FromDateTime(DateTime.Today);

            var nowShowing = movies.Where(m => m.ReleaseDate <= today && today <= m.EndDate).ToList();
            var comingSoon = movies.Where(m => m.ReleaseDate > today).ToList();

            if (!nowShowing.Any() && !comingSoon.Any() && movies.Any())
            {
                nowShowing = movies;
            }

            return Json(new
            {
                nowShowing,
                comingSoon
            });
        }
    }
}
