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
    }
}
