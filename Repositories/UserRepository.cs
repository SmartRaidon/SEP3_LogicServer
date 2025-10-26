using Entities;
using RepositoryContracts;
// 

namespace Repositories;

public class UserRepository : IUserRepository
{
    //private readonly UserRepository.UserRepositoryClient _grpcClient;
    public async Task<User> AddAsync(User user)
    {
        // sending 'user' to Java persistence server via gRPC
        Console.WriteLine($"Forwarding to Java server: {user.Username}");
        await Task.Delay(100); // simulate network delay
        return user;
    }
}