namespace ApiContracts;

public class UserDto
{
    public required int Id { get; set; }
    public required string UserName { get; set; }
    public  int Score { get; set; }
}