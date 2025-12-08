using System.Numerics;
using Entities;

namespace SEP3_LogicServer.Services;

public class GameService
{
    private Dictionary<string, int> inviteCodeToGameID = new Dictionary<string, int>();
    private Dictionary<int, Game> gameBase = new Dictionary<int, Game>();

    public Game CreateGame(int playerId, string playerName)
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
            PlayerXName = playerName,
            PlayerOId = null,
            PlayerOName = null,
            CurrentTurnPlayerId = playerId,
            InviteCode = inviteCode,
            WinnerId = null,
            Status = GameStatus.WaitingForOpponent,// will have to be changed to waiting for opponent now just testing
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


    public Game JoinGame(string inviteCode, int playerOId, string playerOName)
    {
        var game = GetGameByInviteCode(inviteCode)
                   ?? throw new Exception("Game not found with invite code");

        if (game.Status != GameStatus.WaitingForOpponent)
            throw new Exception("Game already started");

        if (game.PlayerOId is not null)
            throw new Exception("Game already has opponent");

        game.PlayerOId = playerOId;
        game.PlayerOName = playerOName;
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

        // --- TURN ENFORCEMENT ---
        if (playerId != game.CurrentTurnPlayerId)
            throw new Exception("Not your turn!");

        // Determine mark
        int mark = playerId == game.PlayerXId ? 1 : 2;
        game.Board[position] = mark;

        // Record move
        var move = new Move
        {
            MoveId = game.Moves.Count + 1,
            GameId = game.Id,
            PlayerId = playerId,
            Position = position,
            Timestamp = DateTime.Now
        };
        game.Moves.Add(move);

        // Check win / draw
        var winningCells = GetWinningCells(game.Board, mark);
        if (winningCells != null)
        {
            game.Status = GameStatus.Finished;
            game.WinnerId = playerId;
            game.WinningCells = winningCells;
            game.CurrentTurnPlayerId = 0; // no next turn
        }
        else if (IsDraw(game.Board))
        {
            game.Status = GameStatus.Finished;
            game.WinnerId = null;
            game.CurrentTurnPlayerId = 0; // no next turn
        }
        else
        {
            // Switch turn to the other player
            game.CurrentTurnPlayerId = (playerId == game.PlayerXId)
                ? game.PlayerOId ?? game.PlayerXId // in case O not joined yet
                : game.PlayerXId;
        }

        UpdateGame(game);
        return (game, move);
    }

    private int[]? GetWinningCells(int[] board, int mark)
    {
        int[][] lines =
        {
            new[] { 0, 1, 2 }, new[] { 3, 4, 5 }, new[] { 6, 7, 8 }, // rows
            new[] { 0, 3, 6 }, new[] { 1, 4, 7 }, new[] { 2, 5, 8 }, // cols
            new[] { 0, 4, 8 }, new[] { 2, 4, 6 } // diags
        };

        foreach (var line in lines)
        {
            if (line.All(i => board[i] == mark))
                return line; // return the winning indexes
        }

        return null;
    }

    private bool IsDraw(int[] board) => board.All(c => c != 0);

    public Game RequestReplay(int gameId, int playerId)
    {
        var game = GetGameById(gameId);
        if (game.Status != GameStatus.Finished)
            throw new Exception("Game is not finished yet. Replay only allowed after finishing.");

        if (playerId == game.PlayerXId)
            game.ReplayRequestedByX = true;
        else if (playerId == game.PlayerOId)
            game.ReplayRequestedByO = true;
        else
            throw new Exception("Player does not belong to this game");
        
        if (game.ReplayRequestedByX && game.ReplayRequestedByO)
        {
            game.Reset();
        }

        return UpdateGame(game);
    }
}