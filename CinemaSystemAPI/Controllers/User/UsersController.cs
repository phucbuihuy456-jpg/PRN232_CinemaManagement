using CinemaSystemAPI.DTOs.User;
using CinemaSystemAPI.Services.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CinemaSystemAPI.Controllers.User
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        public UsersController(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetUsers(
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] string? role = null,
            [FromQuery] bool? isActive = null)
        {
            if (pageIndex < 1) pageIndex = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10; // Giới hạn tối đa 100 để chống quá tải
            var result = await _userRepository.GetUser(pageIndex, pageSize, search, role, isActive);
            return Ok(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            var user = await _userRepository.GetUserById(id);
            if (user == null)
            {
                return NotFound(new { message = $"Không tồn tại user có Id là {id}" });
            }
            return Ok(user);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id:int}/role")]
        public async Task<IActionResult> UpdateUserRole(int id, [FromBody] UpdateUserRoleDTO dto)
        {
            // 1. Kiểm tra danh sách vai trò hợp lệ
            var validRoles = new[] { "Admin", "Staff", "Customer" };
            if (!validRoles.Contains(dto.Role))
            {
                return BadRequest(new { message = "Vai trò không hợp lệ! Chỉ chấp nhận: Admin, Staff, Customer." });
            }

            // 2. Chặn Admin không được tự đổi vai trò của chính mình
            var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(currentUserIdClaim, out int currentUserId) && currentUserId == id)
            {
                return BadRequest(new { message = "Bạn không thể tự thay đổi vai trò của chính mình!" });
            }

            // 3. Cập nhật vai trò
            var isSuccess = await _userRepository.UpdateUserRole(id, dto.Role);
            if (!isSuccess)
            {
                return NotFound(new { message = $"Không tìm thấy User nào có ID là {id}" });
            }

            return Ok(new { message = "Cập nhật vai trò người dùng thành công!" });
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id:int}/status")]
        public async Task<IActionResult> UpdateUserStatus(int id, [FromBody] UpdateUserStatusDTO dto)
        {
            // 1. Chặn Admin không được tự khóa tài khoản của chính mình
            var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(currentUserIdClaim, out int currentUserId) && currentUserId == id && !dto.IsActive)
            {
                return BadRequest(new { message = "Bạn không thể tự khóa tài khoản của chính mình!" });
            }

            // 2. Cập nhật trạng thái
            var isSuccess = await _userRepository.UpdateUserStatus(id, dto.IsActive);
            if (!isSuccess)
            {
                return NotFound(new { message = $"Không tìm thấy User nào có ID là {id}" });
            }

            var statusMessage = dto.IsActive ? "Kích hoạt tài khoản thành công!" : "Khóa tài khoản thành công!";
            return Ok(new { message = statusMessage });
        }
    }
}
