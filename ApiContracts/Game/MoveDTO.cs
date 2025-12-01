namespace ApiContracts.Game;

public class MoveDTO
{
    public int Id { get; set; }
    public int GameId { get; set; }
    public int PlayerId { get; set; }
    public int Position { get; set; }
}