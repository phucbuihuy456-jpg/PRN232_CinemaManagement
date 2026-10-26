using CinemaSystemAPI.DTOs.Movies;
using CinemaSystemAPI.Services.Movies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
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
        [Authorize(Roles = "Admin")]
        [HttpPost("api/movies")]
        public async Task<IActionResult> CreateMovie(CreateMovieDTO movieDto)
        {
            var createdMovie = await _movieRepository.CreateMovie(movieDto);
            if (createdMovie == null)
            {
                return BadRequest(new { message = "Dữ liệu phim không hợp lệ (Thời lượng, ngày chiếu hoặc độ tuổi sai quy định)!" });
            }
            return CreatedAtAction(nameof(GetMovieById), new { id = createdMovie.Id }, createdMovie);
        }
    }
}
