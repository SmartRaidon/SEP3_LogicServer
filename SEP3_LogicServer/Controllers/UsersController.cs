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
    
    
    [HttpPatch("{id}/password")]
    public async Task<ActionResult> ChangePassword(int id, [FromBody] ChangePasswrodDTO dto)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound($"User with ID {id} not found");
            }
            
            user.Password = authService.HashPassword(dto.NewPassword);
            await _userRepository.UpdateAsync(user);
            return NoContent();
        }
        catch (Exception e)
        {
            return StatusCode(500, e.Message);
        }
    }

    [HttpPatch("{id}/username")]
    public async Task<ActionResult> ChangeUsername(int id, [FromBody] ChangeUsernameDTO dto)
    {
        try
        {
            Console.WriteLine($"ChangeUsername called: id={id}, newUsername={dto.NewUsername}");
            var user = await _userRepository.GetSingleAsync(id);
            if (user == null)
            {
                return NotFound($"User with ID {id} not found");
            }
        
            Console.WriteLine($"Current user: Id={user.Id}, Username={user.Username}, Email={user.Email}");
            user.Username = dto.NewUsername;
            Console.WriteLine($"Updating user to: Id={user.Id}, Username={user.Username}, Email={user.Email}");
        
            await _userRepository.UpdateAsync(user);
        
            Console.WriteLine("Username updated successfully");
            return NoContent();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error in ChangeUsername: {e.Message}");
            return StatusCode(500, e.Message);
        }
    }

    [HttpGet("top10")]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetTop10Players()
    {
        try
        {
            var topPlayers = await _userRepository.GetTop10PlayersAsync();
            return Ok(topPlayers);
        }
        catch (Exception e)
        {
            return StatusCode(500, e.Message);
        }
    }

}