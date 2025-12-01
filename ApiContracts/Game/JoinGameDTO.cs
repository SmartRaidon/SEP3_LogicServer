namespace ApiContracts.Game;

public class JoinGameDTO
{
    public required string InviteCode { get; set; }
    public required int PlayerId { get; set; }
}