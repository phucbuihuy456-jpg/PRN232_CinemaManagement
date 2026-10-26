using CinemaSystemAPI.DTOs.Movies;
using CinemaSystemAPI.Models;

namespace CinemaSystemAPI.Services.Movies
{
    public interface IMovieRepository
    {
        IQueryable<Movie> GetMovies();
        Task<MovieDTO?> GetMovieById(int id);
        Task<Movie?> CreateMovie(CreateMovieDTO movie);
    }
}
