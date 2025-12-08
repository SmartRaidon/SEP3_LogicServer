namespace ApiContracts.Game;

public class GameDTO
{
    public int Id { get; set; }
    public int PlayerXId { get; set; }
    public string PlayerXName { get; set; }
    public int? PlayerOId { get; set; } //maybe still waiting for player
    public string PlayerOName { get; set; }
    public int NextPlayerId { get; set; }
    public required string InviteCode { get; set; }
    public int? WinnerId { get; set; } //nullable becase game can be in progress or draw
    public string Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public int[] Board { get; set; }
    public List<MoveDTO> Moves { get; set; }
    public int[]? WinningCells { get; set; }
}