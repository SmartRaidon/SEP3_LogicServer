using Entities;

namespace RepositoryContracts;

public interface IUserRepository
{
    Task<User> AddAsync(User user);
    Task<User?> GetByIdAsync(int id);

    Task<User> UpdateAsync(User user);
    Task<User?> GetByEmailAsync(string username);
    Task<User?> GetSingleAsync(int id);

}