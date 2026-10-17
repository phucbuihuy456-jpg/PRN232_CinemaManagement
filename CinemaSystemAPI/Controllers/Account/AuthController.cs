using CinemaSystemAPI.DTOs;
using CinemaSystemAPI.DTOs.User;
using CinemaSystemAPI.Services;
using CinemaSystemAPI.Services.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CinemaSystemAPI.Controllers.Account
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAccountRepository _accountRepository;
        private readonly JwtService _jwtService;
        public AuthController(IAccountRepository accountRepository, JwtService jwtService)
        {
            _accountRepository = accountRepository;
            _jwtService = jwtService;
        }
        [HttpPost("register")]
        public async Task<IActionResult> Create(CreateUserDTO user)
        {
            var isSuccess = await _accountRepository.CreateUser(user);
            if (!isSuccess)
            {
                return BadRequest(new { message = "Email này đã được sử dụng. Vui lòng chọn email khác!" });
            }
            return Ok(new { message = "Đăng ký tài khoản thành công!" });
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDTO user)
        {
            var existingUser = await _accountRepository.LoginUser(user);
            if (existingUser == null)
            {
                return Unauthorized(new { message = "Tài khoản không tồn tại. Vui lòng nhập lại Email hoặc mật khẩu!" });
            }
            else
            {
                var token = _jwtService.GenerateToken(existingUser);
                return Ok(new
                {
                    token = token,
                    userId = existingUser.Id,
                    fullName = existingUser.FullName,
                    email = existingUser.Email,
                    role = existingUser.Role
                });
            }
        }
        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetUser()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "Token không hợp lệ!" });
            }
            var user = await _accountRepository.GetUserById(userId);
            if (user == null)
            {
                return NotFound(new { message = "Không tìm thấy thông tin người dùng" });
            }
            var profile = new UserProfileDTO
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role,
                CreatedAt = user.CreatedAt
            };
            return Ok(profile);
        }
    }
}
