using Entities;
using Microsoft.Extensions.DependencyInjection;
using RepositoryContracts;



namespace Repositories;

public class UserRepository : IUserRepository
{
    private readonly Sep3_Proto.UserService.UserServiceClient _grpcClient;
    
    public UserRepository(IServiceProvider grpcClient)
    {
        _grpcClient = grpcClient.GetRequiredService<Sep3_Proto.UserService.UserServiceClient>();
    }

    public async Task<User> AddAsync(User user)
    {
        var request = new Sep3_Proto.CreateUserRequest
        {
            Username = user.Username,
            Password = user.Password
        };

        var response = await _grpcClient.CreateUserAsync(request);
        Console.WriteLine($"Created user on Java server: {response.Username} with id {response.Id}");

        return new User
        {
            Id = response.Id,
            Username = response.Username,
            Password = user.Password
        };
    }
}