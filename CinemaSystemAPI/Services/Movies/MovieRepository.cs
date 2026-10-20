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
        public Task<Movie?> GetMovieById(int id)
        {
            throw new NotImplementedException();
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
