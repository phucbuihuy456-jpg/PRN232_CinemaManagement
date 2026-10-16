using CinemaSystemAPI.DTOs;
using CinemaSystemAPI.Models;

namespace CinemaSystemAPI.Services.Account
{
    public interface IAccountRepository
    {
        Task<bool> CreateUser (CreateUserDTO user);
    }
}
