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
    
   // POST /api/users/register
    [HttpPost("register")]
    public async Task<ActionResult<UserDto>> RegisterAsync([FromBody] CreateUserDto request)
    {
        Console.WriteLine($"Registration: username={request.Username}, email={request.Email}");
        Console.WriteLine($"Plain password received: '{request.Password}'");
        
        try
        {
            // HASH the password BEFORE sending to Java
            string hashedPassword = authService.HashPassword(request.Password);
            Console.WriteLine($"Password hashed: {hashedPassword.Substring(0, Math.Min(30, hashedPassword.Length))}...");
            
            User user = new() 
            { 
                Username = request.Username,
                Email = request.Email,
                Password = hashedPassword  // Send HASHED password
            };
            
            User created = await _userRepository.AddAsync(user);
            
            UserDto dto = new() 
            {
                Id = created.Id,
                Username = created.Username,
                Email = created.Email
            };
            
            Console.WriteLine($"User registered: {created.Email} (ID: {created.Id})");
            return Created($"/api/users/{dto.Id}", dto);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Registration error: {ex.Message}");
            return StatusCode(500);
        }
    }

    // POST /api/users/login
    [HttpPost("login")]
    public async Task<ActionResult<UserDto>> LoginAsync([FromBody] LoginDto request)
    {
        Console.WriteLine($"Login attempt: email={request.Email}");
        Console.WriteLine($"Plain password received: {request.Password}");

        try
        {
            User? user = await authService.ValidateUserAsync(request.Email, request.Password);

            if (user == null)
            {
                Console.WriteLine($"Login failed");
                return Unauthorized(new UserDto()
                {
                    Id = 0,
                    Username = ""
                });
            }

            var response = new UserDto()
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Points = user.Points
            };

            Console.WriteLine($"Login successful: {user.Email} (ID: {user.Id})");
            return Ok(response);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Login error: {ex.Message}");
            return StatusCode(500, new UserDto()
            {
                Id = 0,
                Username = ""
            });
        }
    }

    
    // GET /api/users/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetUser(int id)
    {
        try
        {
            User? user = await _userRepository.GetSingleAsync(id);
            
            if (user == null)
            {
                return NotFound();
            }

            UserDto dto = new()
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Points = user.Points
            };

            return Ok(dto);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting user: {ex.Message}");
            return StatusCode(500);
        }
    }
    
}