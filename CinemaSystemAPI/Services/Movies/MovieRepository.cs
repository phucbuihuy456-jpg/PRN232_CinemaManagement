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
                return null;
            }
            if (movie.EndDate < movie.ReleaseDate)
            {
                return null;
            }
            string[] allowedAge = ["P", "C13", "C16", "C18"];
            if (string.IsNullOrEmpty(movie.AgeRating) || !allowedAge.Contains(movie.AgeRating))
            {
                return null;
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
    }
}
