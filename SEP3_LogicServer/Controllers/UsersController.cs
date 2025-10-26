using ApiContracts;
using Entities;
using Microsoft.AspNetCore.Mvc;
using RepositoryContracts;

namespace SEP3_LogicServer.Controllers;

[ApiController]
[Route("api/[controller]")]

public class UsersController : ControllerBase
{
    private readonly IUserRepository _userRepository;

    public UsersController(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }
    
    [HttpPost]
    public async Task<ActionResult<UserDto>> RegisterAsync([FromBody] CreateUserDto request)
    {
        Console.WriteLine("Incoming request... Registering... " + request.Username + " - "  + request.Password);
        User user = new() // create user
        { 
            Username = request.Username,
            Password = request.Password
        };
        User created = await _userRepository.AddAsync(user); // add user to repository
        UserDto dto = new() // create DTO
        {
            Id = created.Id,
            UserName = created.Username
        };
        return Created($"/users/{dto.Id}", dto); // return created userDTO
    }
}