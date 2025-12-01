using System.Numerics;
using Entities;

namespace SEP3_LogicServer.Services;

public class GameService
{
    private Dictionary<string, int> inviteCodeToGameID= new Dictionary<string, int>();
    private Dictionary<int, Game> gameBase = new Dictionary<int, Game>();

    public Game CreateGame(int playerId, string inviteCode)
    {
        
        int id;
        do
        {
            id = Random.Shared.Next();//Random Shared is less risk for same number
        }
        while (gameBase.ContainsKey(id));

        
        if (inviteCodeToGameID.ContainsKey(inviteCode))
        {
            throw new Exception("Invite code already exists...");
        }
        
        var game = new Game
        {
            Id = id,
            PlayerXId = playerId,
            PlayerOId = null,
            InviteCode = inviteCode,
            WinnerId = null,
            Status = GameStatus.WaitingForOpponent,
            CreatedAt = DateTime.Now
        };
        gameBase.Add(id, game);
        inviteCodeToGameID[inviteCode] = id;
        return game;
    }

    public Game? GetGameByInviteCode(string inviteCode)
    {
        if (inviteCodeToGameID.TryGetValue(inviteCode, out var id))
        {
            return gameBase[id];
        }
        return null;
    }

    public Game? GetGameById(int id)
    {
        
        if (gameBase.ContainsKey(id))
        {
            Console.Write("Game is available with status "+ gameBase[id].Status );
            return gameBase[id];
        }
        
        throw new Exception("Game with given ID don't exist "+ id );

    }

    public Game UpdateGame(Game game)
    {
        gameBase[game.Id] = game;
        return game;
    }


}