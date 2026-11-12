using CinemaSystemAPI.Data;
using CinemaSystemAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CinemaSystemAPI.Controllers.Movie
{
    [ApiController]
    [Route("api/[controller]")]
    public class GenresController : ControllerBase
    {
        private readonly CinemaDbContext _context;

        public GenresController(CinemaDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetGenres()
        {
            var genres = await _context.Genres
                .AsNoTracking()
                .OrderBy(g => g.Name)
                .Select(g => new
                {
                    g.Id,
                    g.Name,
                    MovieCount = g.MovieGenres.Count(mg => !mg.Movie.IsDeleted)
                })
                .ToListAsync();

            return Ok(genres);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> CreateGenre([FromBody] CreateGenreDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto?.Name))
            {
                return BadRequest(new { message = "Tên thể loại không được để trống!" });
            }

            var cleanName = dto.Name.Trim();
            if (await _context.Genres.AnyAsync(g => g.Name.ToLower() == cleanName.ToLower()))
            {
                return BadRequest(new { message = $"Thể loại '{cleanName}' đã tồn tại!" });
            }

            var genre = new Genre { Name = cleanName };
            _context.Genres.Add(genre);
            await _context.SaveChangesAsync();

            return Ok(new { id = genre.Id, name = genre.Name, message = "Thêm thể loại mới thành công!" });
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateGenre(int id, [FromBody] CreateGenreDto dto)
        {
            var genre = await _context.Genres.FindAsync(id);
            if (genre == null)
            {
                return NotFound(new { message = $"Không tìm thấy thể loại với ID = {id}" });
            }

            if (string.IsNullOrWhiteSpace(dto?.Name))
            {
                return BadRequest(new { message = "Tên thể loại không được để trống!" });
            }

            var cleanName = dto.Name.Trim();
            if (await _context.Genres.AnyAsync(g => g.Id != id && g.Name.ToLower() == cleanName.ToLower()))
            {
                return BadRequest(new { message = $"Thể loại '{cleanName}' đã tồn tại!" });
            }

            genre.Name = cleanName;
            await _context.SaveChangesAsync();

            return Ok(new { id = genre.Id, name = genre.Name, message = "Cập nhật thể loại thành công!" });
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteGenre(int id)
        {
            var genre = await _context.Genres.Include(g => g.MovieGenres).FirstOrDefaultAsync(g => g.Id == id);
            if (genre == null)
            {
                return NotFound(new { message = $"Không tìm thấy thể loại với ID = {id}" });
            }

            if (genre.MovieGenres.Any())
            {
                return BadRequest(new { message = $"Không thể xóa thể loại '{genre.Name}' vì đang có {genre.MovieGenres.Count} bộ phim thuộc thể loại này!" });
            }

            _context.Genres.Remove(genre);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Xóa thể loại '{genre.Name}' thành công!" });
        }
    }

    public class CreateGenreDto
    {
        public string Name { get; set; } = string.Empty;
    }
}
