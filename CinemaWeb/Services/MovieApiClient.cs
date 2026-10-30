using System.Text.Json;
using CinemaWeb.Models;

namespace CinemaWeb.Services
{
    public interface IMovieApiClient
    {
        Task<List<GenreViewModel>> GetGenresAsync();
        Task<List<MovieItemViewModel>> GetMoviesFromODataAsync(int? genreId = null);
    }

    public class MovieApiClient : IMovieApiClient
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _apiBaseUrl;
        private readonly ILogger<MovieApiClient> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public MovieApiClient(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<MovieApiClient> logger)
        {
            _httpClientFactory = httpClientFactory;
            _apiBaseUrl = configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5027";
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<List<GenreViewModel>> GetGenresAsync()
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client.GetAsync($"{_apiBaseUrl}/api/Genres");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var genres = JsonSerializer.Deserialize<List<GenreViewModel>>(json, _jsonOptions);
                    return genres ?? new List<GenreViewModel>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gọi API lấy danh sách thể loại");
            }
            return new List<GenreViewModel>();
        }

        public async Task<List<MovieItemViewModel>> GetMoviesFromODataAsync(int? genreId = null)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                // OData query: mở rộng thông tin MovieGenres kèm Genre
                string odataQuery = "odata/Movies?$expand=MovieGenres($expand=Genre)&$orderby=ReleaseDate desc";

                if (genreId.HasValue && genreId.Value > 0)
                {
                    // Lọc theo thể loại thông qua OData navigation property
                    odataQuery += $"&$filter=MovieGenres/any(g: g/GenreId eq {genreId.Value})";
                }

                var url = $"{_apiBaseUrl}/{odataQuery}";
                var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();

                    // OData response có thể trả về array trực tiếp hoặc wrapper {"value": [...]}
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.ValueKind == JsonValueKind.Array)
                    {
                        var movies = JsonSerializer.Deserialize<List<MovieItemViewModel>>(json, _jsonOptions);
                        return movies ?? new List<MovieItemViewModel>();
                    }
                    else if (doc.RootElement.TryGetProperty("value", out var valueProp) && valueProp.ValueKind == JsonValueKind.Array)
                    {
                        var movies = JsonSerializer.Deserialize<List<MovieItemViewModel>>(valueProp.GetRawText(), _jsonOptions);
                        return movies ?? new List<MovieItemViewModel>();
                    }
                }
                else
                {
                    _logger.LogWarning("Gọi OData Movies không thành công. Status: {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gọi OData Movies");
            }

            return new List<MovieItemViewModel>();
        }
    }
}
