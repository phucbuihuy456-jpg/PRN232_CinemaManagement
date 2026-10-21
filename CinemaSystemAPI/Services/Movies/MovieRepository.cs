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
