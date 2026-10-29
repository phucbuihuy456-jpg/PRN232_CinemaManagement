using CinemaSystemAPI.DTOs.User;

namespace CinemaSystemAPI.Services.Account
{
    public interface IUserRepository
    {
        Task<PagedResult> GetUser(int pageIndex, int pageSize, string? search, string? role, bool? isActive);
        Task<UserDTO?> GetUserById(int id);
        Task<bool> UpdateUserRole(int id, string role);
        Task<bool> UpdateUserStatus(int id, bool isActive);
    }
}
