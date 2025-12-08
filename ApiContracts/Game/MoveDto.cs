namespace ApiContracts.Game;

public class MoveDto
{
    public int MoveId { get; set; }
    public int GameId { get; set; }
    public int PlayerId { get; set; }
    public int Position { get; set; }
}