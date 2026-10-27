using CinemaSystemAPI.DTOs.User;
using CinemaSystemAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaSystemAPI.Services.Account
{
    public class AccountRepository : IAccountRepository
    {
        private readonly CinemaDbContext _context;
        public AccountRepository(CinemaDbContext context)
        {
            _context = context;
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
    }
}
