using System.Numerics;
using Entities;

namespace SEP3_LogicServer.Services;

public class GameService
{
    private Dictionary<string, int> inviteCodeToGameID = new Dictionary<string, int>();
    private Dictionary<int, Game> gameBase = new Dictionary<int, Game>();

    public Game CreateGame(int playerId)
    {
        int id;
        do
        {
            id = Random.Shared.Next(); //Random Shared is less risk for same number
        } while (gameBase.ContainsKey(id));


        string inviteCode;
        do
        {
            inviteCode = GenerateInviteCode(6);
        } while (inviteCodeToGameID.ContainsKey(inviteCode));

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

    private string GenerateInviteCode(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var buffer = new char[length];

        for (int i = 0; i < length; i++)
        {
            buffer[i] = chars[Random.Shared.Next(chars.Length)];
        }

        return new string(buffer);
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
            Console.Write("Game is available with status " + gameBase[id].Status);
            return gameBase[id];
        }

        throw new Exception("Game with given ID don't exist " + id);
    }

    public Game UpdateGame(Game game)
    {
        gameBase[game.Id] = game;
        return game;
    }


    public Game JoinGame(string inviteCode, int playerOId)
    {
        var game = GetGameByInviteCode(inviteCode)
                   ?? throw new Exception("Game not found with invite code");

        if (game.Status != GameStatus.WaitingForOpponent)
            throw new Exception("Game already started");

        if (game.PlayerOId is not null)
            throw new Exception("Game already has opponent");

        game.PlayerOId = playerOId;
        game.Status = GameStatus.InProgress;

        return UpdateGame(game);
    }


    public (Game Game, Move Move) MakeMove(int gameId, int playerId, int position)
    {
        var game = GetGameById(gameId);

        if (game.Status != GameStatus.InProgress)
            throw new Exception("Game is not in progress");

        if (position < 0 || position > 8)
            throw new Exception("Invalid board position");

        game.Board ??= new int[9];
        game.Moves ??= new List<Move>();

        if (game.Board[position] != 0)
            throw new Exception("Cell already taken");

        // whose turn isit
        int xMoves = game.Moves.Count(m => m.PlayerId == game.PlayerXId);
        int oMoves = game.PlayerOId.HasValue
            ? game.Moves.Count(m => m.PlayerId == game.PlayerOId.Value)
            : 0;

        bool xTurn = xMoves == oMoves; // X starts

        if (xTurn && playerId != game.PlayerXId)
            throw new Exception("Not your turn (expected X player)");
        if (!xTurn && playerId != game.PlayerOId)
            throw new Exception("Not your turn (expected O player)");

        int mark = xTurn ? 1 : 2; // 1 = X, 2 = O
        game.Board[position] = mark;

        var move = new Move
        {
            MoveId = game.Moves.Count + 1,
            GameId = game.Id,
            PlayerId = playerId,
            Position = position,
            Timestamp = DateTime.Now
        };

        game.Moves.Add(move);

        // Win / draw check
        if (IsWinningMove(game.Board, mark))
        {
            game.Status = GameStatus.Finished;
            game.WinnerId = playerId;
        }
        else if (IsDraw(game.Board))
        {
            game.Status = GameStatus.Finished;
            game.WinnerId = null;
        }

        UpdateGame(game);

        return (game, move);
    }

    private bool IsWinningMove(int[] board, int mark)
    {
        int[][] lines =
        {
            new[] { 0, 1, 2 }, new[] { 3, 4, 5 }, new[] { 6, 7, 8 }, // rows
            new[] { 0, 3, 6 }, new[] { 1, 4, 7 }, new[] { 2, 5, 8 }, // cols
            new[] { 0, 4, 8 }, new[] { 2, 4, 6 } // diags
        };

        return lines.Any(line => line.All(i => board[i] == mark));
    }

    private bool IsDraw(int[] board) => board.All(c => c != 0);
}