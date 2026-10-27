using CinemaSystemAPI.DTOs.User;
using CinemaSystemAPI.Models;

namespace CinemaSystemAPI.Services.Account
{
    public interface IAccountRepository
    {
        Task<bool> CreateUser(CreateUserDTO user);
        Task<User?> LoginUser(LoginDTO user);
        Task<User?> GetUserById(int id);
        Task<User?> UpdateUserById(int id, UpdateUserDTO user);
        Task<bool> ChangePassword(int id, ChangePasswordDTO user);
    }
}
