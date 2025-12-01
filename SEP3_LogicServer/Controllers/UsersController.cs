using ApiContracts;
using Entities;
using Microsoft.AspNetCore.Mvc;
using RepositoryContracts;
using SEP3_LogicServer.Services;

namespace SEP3_LogicServer.Controllers;

[ApiController]
[Route("api/[controller]")]

public class UsersController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly AuthService authService;
    public UsersController(IUserRepository userRepository, AuthService authService)
    {
        _userRepository = userRepository;
        this.authService = authService;
    }
    
    [HttpPost]
    public async Task<ActionResult<UserDto>> RegisterAsync([FromBody] CreateUserDto request)
    {
        Console.WriteLine("Incoming request... Registering... " + request.Username + " - "  + request.Password);
        User user = new() // create user
        { 
            Username = request.Username,
            //Password = request.Password hashing for work factory - 12 should be the best for general Web app
            Password = authService.HashPassword(request.Password),
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