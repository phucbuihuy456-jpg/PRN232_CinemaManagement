using CinemaSystemAPI.Data;
using CinemaSystemAPI.Models;
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
                    g.Name
                })
                .ToListAsync();

            return Ok(genres);
        }
    }
}
