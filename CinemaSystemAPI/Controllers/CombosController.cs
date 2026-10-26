using CinemaSystemAPI.DTOs.Combos;
using CinemaSystemAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CinemaSystemAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CombosController : ControllerBase
    {
        private readonly CinemaDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public CombosController(CinemaDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        /// <summary>
        /// Lấy danh sách combo bắp nước.
        /// Mặc định chỉ lấy combo đang bán (IsActive = true).
        /// </summary>
        /// <param name="includeInactive">true nếu muốn lấy cả combo đã ngưng bán (dành cho Admin)</param>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ComboDto>>> GetAll([FromQuery] bool includeInactive = false)
        {
            var query = _context.Combos.AsNoTracking();

            if (!includeInactive)
            {
                query = query.Where(c => c.IsActive);
            }

            var combos = await query
                .OrderBy(c => c.Id)
                .Select(c => new ComboDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    Price = c.Price,
                    ImageUrl = c.ImageUrl,
                    IsActive = c.IsActive
                })
                .ToListAsync();

            return Ok(combos);
        }

        /// <summary>
        /// Lấy chi tiết combo theo ID.
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ComboDto>> GetById(int id)
        {
            var combo = await _context.Combos
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);

            if (combo == null)
            {
                return NotFound(new { message = $"Không tìm thấy combo với ID = {id}" });
            }

            return Ok(new ComboDto
            {
                Id = combo.Id,
                Name = combo.Name,
                Description = combo.Description,
                Price = combo.Price,
                ImageUrl = combo.ImageUrl,
                IsActive = combo.IsActive
            });
        }

        /// <summary>
        /// Tạo combo bắp nước mới.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ComboDto>> Create([FromBody] CreateComboDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var combo = new Combo
            {
                Name = dto.Name.Trim(),
                Description = dto.Description?.Trim(),
                Price = dto.Price,
                ImageUrl = dto.ImageUrl?.Trim(),
                IsActive = dto.IsActive
            };

            _context.Combos.Add(combo);
            await _context.SaveChangesAsync();

            var resultDto = new ComboDto
            {
                Id = combo.Id,
                Name = combo.Name,
                Description = combo.Description,
                Price = combo.Price,
                ImageUrl = combo.ImageUrl,
                IsActive = combo.IsActive
            };

            return CreatedAtAction(nameof(GetById), new { id = combo.Id }, resultDto);
        }

        /// <summary>
        /// Cập nhật combo (Tên, Mô tả, Giá, Ảnh, Trạng thái).
        /// </summary>
        [HttpPut("{id:int}")]
        public async Task<ActionResult<ComboDto>> Update(int id, [FromBody] UpdateComboDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var combo = await _context.Combos.FindAsync(id);
            if (combo == null)
            {
                return NotFound(new { message = $"Không tìm thấy combo với ID = {id}" });
            }

            combo.Name = dto.Name.Trim();
            combo.Description = dto.Description?.Trim();
            combo.Price = dto.Price;
            combo.ImageUrl = dto.ImageUrl?.Trim();
            combo.IsActive = dto.IsActive;

            await _context.SaveChangesAsync();

            return Ok(new ComboDto
            {
                Id = combo.Id,
                Name = combo.Name,
                Description = combo.Description,
                Price = combo.Price,
                ImageUrl = combo.ImageUrl,
                IsActive = combo.IsActive
            });
        }

        /// <summary>
        /// Soft delete combo (Chuyển trạng thái IsActive sang false).
        /// </summary>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> SoftDelete(int id)
        {
            var combo = await _context.Combos.FindAsync(id);
            if (combo == null)
            {
                return NotFound(new { message = $"Không tìm thấy combo với ID = {id}" });
            }

            combo.IsActive = false;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = $"Combo '{combo.Name}' đã được chuyển sang trạng thái ngưng bán (soft-deleted).",
                id = combo.Id,
                isActive = combo.IsActive
            });
        }

        /// <summary>
        /// Upload ảnh cho combo bắp nước lên máy chủ.
        /// </summary>
        [HttpPost("upload-image")]
        public async Task<IActionResult> UploadImage(IFormFile? file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "Vui lòng chọn file ảnh hợp lệ." });
            }

            // Giới hạn kích thước tối đa 5MB
            if (file.Length > 5 * 1024 * 1024)
            {
                return BadRequest(new { message = "Kích thước ảnh tối đa là 5MB." });
            }

            // Kiểm tra định dạng ảnh
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                return BadRequest(new { message = "Chỉ chấp nhận file ảnh (.jpg, .jpeg, .png, .webp, .gif)." });
            }

            var webRootPath = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
            var uploadDir = Path.Combine(webRootPath, "uploads", "combos");

            if (!Directory.Exists(uploadDir))
            {
                Directory.CreateDirectory(uploadDir);
            }

            var uniqueFileName = $"combo_{Guid.NewGuid():N}{extension}";
            var filePath = Path.Combine(uploadDir, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var relativeUrl = $"/uploads/combos/{uniqueFileName}";
            return Ok(new { imageUrl = relativeUrl });
        }
    }
}
