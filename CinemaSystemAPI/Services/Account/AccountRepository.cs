using CinemaSystemAPI.DTOs.User;
using CinemaSystemAPI.Models;
using CinemaSystemAPI.Services.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace CinemaSystemAPI.Services.Account
{
    public class AccountRepository : IAccountRepository
    {
        private readonly CinemaDbContext _context;
        private readonly IMemoryCache _memoryCache;
        private readonly IEmailService _emailService;

        public AccountRepository(
            CinemaDbContext context,
            IMemoryCache memoryCache,
            IEmailService emailService)
        {
            _context = context;
            _memoryCache = memoryCache;
            _emailService = emailService;
        }

        public async Task<bool> ChangePassword(int id, ChangePasswordDTO user)
        {
            var checkUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (checkUser == null)
            {
                return false;
            }

            // 1. Kiểm tra mật khẩu hiện tại
            bool isOldPasswordValid = BCrypt.Net.BCrypt.Verify(user.CurrentPassword, checkUser.PasswordHash);
            if (!isOldPasswordValid)
            {
                throw new Exception("Mật khẩu hiện tại không chính xác!");
            }

            // 2. Kiểm tra xác nhận mật khẩu mới
            if (user.NewPassword != user.ConfirmPassword)
            {
                throw new Exception("Mật khẩu xác nhận không trùng khớp!");
            }

            // 3. Kiểm tra mật khẩu mới không được trùng với mật khẩu cũ
            if (user.CurrentPassword == user.NewPassword)
            {
                throw new Exception("Mật khẩu mới không được trùng với mật khẩu cũ!");
            }

            // 3. Cập nhật mật khẩu mới đã băm và thời gian cập nhật
            checkUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.NewPassword);
            checkUser.UpdatedAt = DateTime.Now;

            _context.Users.Update(checkUser);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CreateUser(CreateUserDTO user)
        {
            var checkExist = await _context.Users.FirstOrDefaultAsync(u => u.Email == user.Email);
            if (checkExist != null)
            {
                return false;
            }
            else
            {
                var newUser = new User
                {
                    FullName = user.FullName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash),
                    Role = "Customer",
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };
                await _context.Users.AddAsync(newUser);
                await _context.SaveChangesAsync();
                return true;
            }
        }

        public async Task<User?> GetUserById(int id)
        {
            var checkUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (checkUser == null)
            {
                return null;
            }
            else
            {
                return checkUser;
            }
        }

        public async Task<User?> LoginUser(LoginDTO user)
        {
            var checkExist = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == user.Email);
            if (checkExist == null)
            {
                return null;
            }
            else
            {
                if (checkExist.IsActive == false)
                {
                    return null;
                }
                else
                {
                    bool isPasswordValid = BCrypt.Net.BCrypt.Verify(user.Password, checkExist.PasswordHash);
                    if (!isPasswordValid)
                    {
                        return null;
                    }
                    else
                    {
                        return checkExist;
                    }
                }
            }
        }

        public async Task<User?> UpdateUserById(int id, UpdateUserDTO user)
        {
            var checkUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (checkUser == null)
            {
                return null;
            }
            else
            {
                var emailExist = await _context.Users.AnyAsync(u => u.Email == user.Email && u.Id != id);
                if (emailExist)
                {
                    throw new Exception("Email này đã được sử dụng bởi một tài khoản khác!");
                }
                checkUser.FullName = user.FullName;
                checkUser.Email = user.Email;
                checkUser.PhoneNumber = user.PhoneNumber;
                checkUser.UpdatedAt = DateTime.Now;
                _context.Users.Update(checkUser);
                await _context.SaveChangesAsync();
                return checkUser;
            }
        }

        public async Task<bool> SendForgotPasswordOtp(string email)
        {
            var normalizedEmail = email.Trim().ToLower();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail && u.IsActive);
            if (user == null)
            {
                throw new Exception("Không tìm thấy tài khoản với email này trong hệ thống!");
            }

            // Sinh mã OTP 6 chữ số
            var otpCode = Random.Shared.Next(100000, 999999).ToString();

            // Lưu vào MemoryCache trong 5 phút
            var cacheKey = $"otp_reset_{normalizedEmail}";
            _memoryCache.Set(cacheKey, otpCode, TimeSpan.FromMinutes(5));

            // Gửi email chứa mã OTP
            await _emailService.SendOtpEmailAsync(user.Email, user.FullName, otpCode);
            return true;
        }

        public async Task<bool> ResetPasswordWithOtp(ResetPasswordOtpDTO dto)
        {
            if (dto.NewPassword != dto.ConfirmPassword)
            {
                throw new Exception("Mật khẩu xác nhận không trùng khớp!");
            }

            var normalizedEmail = dto.Email.Trim().ToLower();
            var cacheKey = $"otp_reset_{normalizedEmail}";

            if (!_memoryCache.TryGetValue(cacheKey, out string? cachedOtp) || string.IsNullOrEmpty(cachedOtp))
            {
                throw new Exception("Mã OTP đã hết hạn hoặc không tồn tại. Vui lòng yêu cầu mã OTP mới!");
            }

            if (cachedOtp != dto.Otp.Trim())
            {
                throw new Exception("Mã OTP không chính xác. Vui lòng kiểm tra lại!");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail && u.IsActive);
            if (user == null)
            {
                throw new Exception("Không tìm thấy thông tin tài khoản hợp lệ!");
            }

            // Cập nhật mật khẩu mới đã băm bằng BCrypt
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            user.UpdatedAt = DateTime.Now;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            // Xóa mã OTP khỏi cache
            _memoryCache.Remove(cacheKey);

            return true;
        }
    }
}
