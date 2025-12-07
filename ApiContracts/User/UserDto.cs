namespace ApiContracts;

public class UserDto
{
    public required int Id { get; set; }
    public required string Username { get; set; }
    public string Email { get; set; }
    public  int Score { get; set; }
}