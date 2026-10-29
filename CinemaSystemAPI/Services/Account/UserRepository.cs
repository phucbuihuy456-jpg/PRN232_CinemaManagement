using CinemaSystemAPI.DTOs.User;
using CinemaSystemAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaSystemAPI.Services.Account
{
    public class UserRepository : IUserRepository
    {
        private readonly CinemaDbContext _context;
        public UserRepository(CinemaDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult> GetUser(int pageIndex, int pageSize, string? search, string? role, bool? isActive)
        {
            var query = _context.Users.AsNoTracking();
            // 1. Tìm kiếm theo tên hoặc email
            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.Trim().ToLower();
                query = query.Where(u => u.FullName.ToLower().Contains(keyword) || u.Email.ToLower().Contains(keyword));
            }
            // 2. Lọc theo Role
            if (!string.IsNullOrWhiteSpace(role))
            {
                query = query.Where(u => u.Role == role);
            }
            // 3. Lọc theo trạng thái IsActive
            if (isActive.HasValue)
            {
                query = query.Where(u => u.IsActive == isActive.Value);
            }
            // 4. Đếm tổng số bản ghi thỏa điều kiện
            var totalItems = await query.CountAsync();
            var items = await query
                .OrderByDescending(u => u.CreatedAt) // Người dùng mới nhất lên đầu
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new UserDTO
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    Role = u.Role,
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt
                })
                .ToListAsync();
            return new PagedResult
            {
                Items = items,
                TotalItems = totalItems,
                PageIndex = pageIndex,
                PageSize = pageSize
            };
        }

        public async Task<UserDTO?> GetUserById(int id)
        {
            var checkUserExist = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id);
            if (checkUserExist == null)
            {
                return null;
            }
            else
            {
                return new UserDTO
                {
                    Id = checkUserExist.Id,
                    FullName = checkUserExist.FullName,
                    Email = checkUserExist.Email,
                    PhoneNumber = checkUserExist.PhoneNumber,
                    Role = checkUserExist.Role,
                    IsActive = checkUserExist.IsActive,
                    CreatedAt = checkUserExist.CreatedAt
                };
            }
        }

        public async Task<bool> UpdateUserRole(int id, string role)
        {
            var checkUserExist = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (checkUserExist == null)
            {
                return false;
            }

            checkUserExist.Role = role;
            checkUserExist.UpdatedAt = DateTime.Now;

            _context.Users.Update(checkUserExist);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateUserStatus(int id, bool isActive)
        {
            var checkUserExist = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (checkUserExist == null)
            {
                return false;
            }

            checkUserExist.IsActive = isActive;
            checkUserExist.UpdatedAt = DateTime.Now;

            _context.Users.Update(checkUserExist);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
