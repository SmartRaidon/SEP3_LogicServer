namespace Entities;

public class Game
{
    public int Id { get; set; }
    public int PlayerXId { get; set; }
    public string PlayerXName { get; set; }
    public int? PlayerOId { get; set; } //maybe still waiting for player
    public string PlayerOName { get; set; }
    public int CurrentTurnPlayerId { get; set; }
    public required string InviteCode { get; set; }
    public int? WinnerId { get; set; } //nullable becase game can be in progress or draw
    public required GameStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<Move> Moves { get; set; } = new();
    public int[] Board { get; set; } = new int[9];
    public int[]? WinningCells { get; set; }
}

public enum GameStatus
{
    WaitingForOpponent,
    InProgress,
    Finished
}