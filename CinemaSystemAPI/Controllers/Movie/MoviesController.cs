using CinemaSystemAPI.Services.Movies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;

namespace CinemaSystemAPI.Controllers.Movie
{
    [ApiController]
    public class MoviesController : ControllerBase
    {
        private readonly IMovieRepository _movieRepository;

        public MoviesController(IMovieRepository movieRepository)
        {
            _movieRepository = movieRepository;
        }

        [Route("odata/[controller]")]
        [HttpGet]
        [EnableQuery]
        public IActionResult GetMovies()
        {
            return Ok(_movieRepository.GetMovies());
        }
        [HttpGet("api/movies/{id}")]
        public async Task<IActionResult> GetMovieById(int id)
        {
            var movie = await _movieRepository.GetMovieById(id);
            if (movie == null)
            {
                return NotFound(new { message = "Không tìm thấy phim với ID này!" });
            }
            return Ok(movie);
        }
    }
}
