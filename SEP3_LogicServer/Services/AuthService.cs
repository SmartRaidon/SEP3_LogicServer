namespace SEP3_LogicServer.Services;

public class AuthService
{
    public string HashPassword(string plainPassword)
    {
        return BCrypt.Net.BCrypt.EnhancedHashPassword(plainPassword,12);
    }
    
    public bool VerifyPassword(string plainPassword, string hashedPassword)
    {
        return BCrypt.Net.BCrypt.Verify(plainPassword, hashedPassword);
    }
}