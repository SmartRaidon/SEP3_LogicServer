namespace Entities;

public class Move
{
    public int MoveId { get; set; }
    public int GameId { get; set; }
    public int PlayerId { get; set; }
    public int Position { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}