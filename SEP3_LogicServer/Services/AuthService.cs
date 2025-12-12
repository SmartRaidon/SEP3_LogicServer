using ApiContracts;
using Entities;
using Microsoft.AspNetCore.Mvc;
using RepositoryContracts;

namespace SEP3_LogicServer.Services;

public class AuthService
{
    private readonly IUserRepository userRepository;

    public AuthService(IUserRepository userRepository)
    {
        this.userRepository = userRepository;
    }
     public string HashPassword(string plainPassword)
    {
        var hashed = BCrypt.Net.BCrypt.HashPassword(plainPassword,12);
        return hashed;
    }
    
    public bool VerifyPassword(string plainPassword, string hashedPassword)
    {
        try
        {
            bool result = BCrypt.Net.BCrypt.Verify(plainPassword, hashedPassword);
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR in VerifyPassword: {ex.Message}");
            return false;
        }
    }

    public async Task<User?> ValidateUserAsync(string email, string plainPassword)
    {
        
        User? user = await userRepository.GetByEmailAsync(email);
        
        if (user == null)
        {
            Console.WriteLine($"User not found");
            return null;
        }
        Console.WriteLine($"User found: {user.Email} (ID: {user.Id})");
        bool isPasswordCorrect = VerifyPassword(plainPassword, user.Password);
        
        if (!isPasswordCorrect)
        {
            Console.WriteLine($"Password verification failed");
            return null;
        }

        Console.WriteLine($"Password verified successfully");
        return user;
    }
    
    

    
}