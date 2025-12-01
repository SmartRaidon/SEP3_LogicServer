namespace ApiContracts.Game;

public class GameDTO
{
    public int Id { get; set; }
    public int PlayerXId { get; set; }
    public int? PlayerOId { get; set; } //maybe still waiting for player
    public required string InviteCode { get; set; }
    public int? WinnerId { get; set; } //nullable becase game can be in progress or draw
    public string Status { get; set; }
    public DateTime CreatedAt { get; set; }
}