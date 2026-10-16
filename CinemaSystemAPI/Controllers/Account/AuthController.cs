using CinemaSystemAPI.DTOs;
using CinemaSystemAPI.Services;
using CinemaSystemAPI.Services.Account;
using Microsoft.AspNetCore.Mvc;

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
    }
}
