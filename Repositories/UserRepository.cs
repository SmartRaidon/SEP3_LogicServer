using Entities;
using RepositoryContracts;
using Sep3_Proto;


namespace Repositories;

public class UserRepository : IUserRepository
{
    private readonly UserService.UserServiceClient _grpcClient;
    
    public UserRepository(UserService.UserServiceClient grpcClient)
    {
        _grpcClient = grpcClient;
    }

    public async Task<User> AddAsync(User user)
    {
        var request = new CreateUserRequest
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