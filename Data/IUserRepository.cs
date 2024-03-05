using FinanceAPI.Models;

namespace FinanceAPI.Data
{
    public interface IUserRepository
    {
        Task AddUserAsync(ApplicationUser user);
        Task<ApplicationUser> GetUserByEmailAsync(string email);
        Task<ApplicationUser> UpdateUserAsync(ApplicationUser user);
    }
}
