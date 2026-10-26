using CinemaSystemAPI.DTOs.Movies;
using CinemaSystemAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaSystemAPI.Services.Movies
{
    public class MovieRepository : IMovieRepository
    {
        private readonly CinemaDbContext _context;
        public MovieRepository(CinemaDbContext context)
        {
            _context = context;
        }

        public async Task<Movie?> CreateMovie(CreateMovieDTO movie)
        {
            if (movie.DurationMinutes <= 0)
            {
                throw new ArgumentException("Thời lượng phim phải lớn hơn 0!");
            }
            if (movie.EndDate < movie.ReleaseDate)
            {
                throw new ArgumentException("Ngày kết thúc chiếu phải lớn hơn hoặc bằng ngày khởi chiếu!");
            }
            string[] allowedAge = ["P", "C13", "C16", "C18"];
            if (!string.IsNullOrEmpty(movie.AgeRating) && !allowedAge.Contains(movie.AgeRating))
            {
                throw new ArgumentException("Độ tuổi không hợp lệ! Chỉ chấp nhận: P, C13, C16, C18");
            }

            var newMovie = new Movie
            {
                DurationMinutes = movie.DurationMinutes,
                EndDate = movie.EndDate,
                ReleaseDate = movie.ReleaseDate,
                Language = movie.Language,
                PosterUrl = movie.PosterUrl,
                Director = movie.Director,
                AgeRating = movie.AgeRating,
                Cast = movie.Cast,
                Description = movie.Description,
                TrailerUrl = movie.TrailerUrl,
                Title = movie.Title,
                IsDeleted = false,
                CreatedAt = DateTime.Now
            };

            if (movie.GenreIds != null && movie.GenreIds.Any())
            {
                foreach (var genreId in movie.GenreIds)
                {
                    newMovie.MovieGenres.Add(new MovieGenre
                    {
                        GenreId = genreId
                    });
                }
            }

            await _context.Movies.AddAsync(newMovie);
            await _context.SaveChangesAsync();
            return newMovie;
        }

        public async Task<MovieDTO?> GetMovieById(int id)
        {
            var movie = await _context.Movies
                .Include(m => m.MovieGenres)
                    .ThenInclude(mg => mg.Genre)
                .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);

            if (movie == null)
            {
                return null;
            }

            return new MovieDTO
            {
                Id = movie.Id,
                Title = movie.Title,
                PosterUrl = movie.PosterUrl,
                AgeRating = movie.AgeRating,
                DurationMinutes = movie.DurationMinutes,
                ReleaseDate = movie.ReleaseDate,
                MovieGenres = movie.MovieGenres,
                TrailerUrl = movie.TrailerUrl
            };
        }

        public IQueryable<Movie> GetMovies()
        {
            return _context.Movies
                .Include(m => m.MovieGenres)
                    .ThenInclude(mg => mg.Genre)
                .Where(m => !m.IsDeleted);
        }

        public async Task<Movie?> UpdateMovie(int id, UpdateMovieDTO movie)
        {
            var checkExist = await _context.Movies
                .Include(m => m.MovieGenres)
                .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);

            if (checkExist == null)
            {
                return null;
            }

            if (movie.DurationMinutes <= 0)
            {
                throw new ArgumentException("Thời lượng phim phải lớn hơn 0!");
            }
            if (movie.EndDate < movie.ReleaseDate)
            {
                throw new ArgumentException("Ngày kết thúc chiếu phải lớn hơn hoặc bằng ngày khởi chiếu!");
            }
            string[] allowedAge = ["P", "C13", "C16", "C18"];
            if (!string.IsNullOrEmpty(movie.AgeRating) && !allowedAge.Contains(movie.AgeRating))
            {
                throw new ArgumentException("Độ tuổi không hợp lệ! Chỉ chấp nhận: P, C13, C16, C18");
            }

            checkExist.Title = movie.Title;
            checkExist.Director = movie.Director;
            checkExist.DurationMinutes = movie.DurationMinutes;
            checkExist.Cast = movie.Cast;
            checkExist.AgeRating = movie.AgeRating;
            checkExist.Description = movie.Description;
            checkExist.EndDate = movie.EndDate;
            checkExist.ReleaseDate = movie.ReleaseDate;
            checkExist.Language = movie.Language;
            checkExist.PosterUrl = movie.PosterUrl;
            checkExist.TrailerUrl = movie.TrailerUrl;
            checkExist.UpdatedAt = DateTime.Now;

            // Cập nhật thể loại an toàn (Diffing)
            var targetGenreIds = movie.GenreIds ?? new List<int>();

            // 1. Xóa các thể loại cũ không còn được chọn
            var genresToRemove = checkExist.MovieGenres
                .Where(mg => !targetGenreIds.Contains(mg.GenreId))
                .ToList();
            _context.MovieGenres.RemoveRange(genresToRemove);

            // 2. Thêm các thể loại mới chưa có trong danh sách cũ
            var currentGenreIds = checkExist.MovieGenres.Select(mg => mg.GenreId).ToList();
            var genreIdsToAdd = targetGenreIds
                .Where(gid => !currentGenreIds.Contains(gid))
                .ToList();

            foreach (var genreId in genreIdsToAdd)
            {
                checkExist.MovieGenres.Add(new MovieGenre
                {
                    MovieId = checkExist.Id,
                    GenreId = genreId
                });
            }

            _context.Movies.Update(checkExist);
            await _context.SaveChangesAsync();
            return checkExist;
        }
    }
}
