using CinemaSystemAPI.DTOs.Movies;
using CinemaSystemAPI.Services.Movies;
using Microsoft.AspNetCore.Authorization;
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
            try
            {
                var createdMovie = await _movieRepository.CreateMovie(movieDto);
                if (createdMovie == null)
                {
                    return BadRequest(new { message = "Không thể tạo phim!" });
                }
                return CreatedAtAction(nameof(GetMovieById), new { id = createdMovie.Id }, createdMovie);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("api/movies/{id}")]
        public async Task<IActionResult> UpdateMovie(int id, [FromBody] UpdateMovieDTO movieDto)
        {
            try
            {
                var updatedMovie = await _movieRepository.UpdateMovie(id, movieDto);
                if (updatedMovie == null)
                {
                    return NotFound(new { message = $"Không tìm thấy bộ phim có ID = {id}!" });
                }

                return Ok(new
                {
                    message = "Cập nhật thông tin phim thành công!",
                    data = updatedMovie
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("api/movies/{id}")]
        public async Task<IActionResult> DeleteMovie(int id)
        {
            try
            {
                var isSuccess = await _movieRepository.DeleteMovie(id);
                if (!isSuccess)
                {
                    return NotFound(new { message = $"Không tìm thấy bộ phim có ID = {id} hoặc phim đã bị xóa trước đó!" });
                }

                return Ok(new { message = "Xóa mềm phim thành công!" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
