using CinemaSystemAPI.DTOs;
using CinemaSystemAPI.Services.Account;
using Microsoft.AspNetCore.Mvc;

namespace CinemaSystemAPI.Controllers.Account
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAccountRepository _accountRepository;
        public AuthController(IAccountRepository accountRepository)
        {
            _accountRepository = accountRepository;
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
    }
}
