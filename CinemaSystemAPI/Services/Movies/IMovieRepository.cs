using CinemaSystemAPI.Models;

namespace CinemaSystemAPI.Services.Movies
{
    public interface IMovieRepository
    {
        IQueryable<Movie> GetMovies();
        Task<Movie?> GetMovieById(int id);
    }
}
