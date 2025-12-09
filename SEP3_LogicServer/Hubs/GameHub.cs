using RepositoryContracts;

namespace SEP3_LogicServer.Hubs;

using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using Entities;
using Microsoft.AspNetCore.SignalR;
using SEP3_LogicServer.Services;
using ApiContracts.Game;
using System.Linq;

public class GameHub : Hub
{
    private readonly GameService _gameService;
    private readonly IUserRepository _userRepository;

    public GameHub(GameService gameService, IUserRepository userRepository)
    {
        _gameService = gameService;
        _userRepository = userRepository;
    }

    private static GameDto MapGameToDto(Game game)
    {
        return new GameDto
        {
            Id = game.Id,
            PlayerXId = game.PlayerXId,
            PlayerOId = game.PlayerOId,
            PlayerXName = game.PlayerXName,
            PlayerOName = game.PlayerOName,
            InviteCode = game.InviteCode,
            WinnerId = game.WinnerId,
            Status = game.Status.ToString(), // enum -> string
            CreatedAt = game.CreatedAt,
            Board = game.Board,
            Moves = game.Moves
                .Select(MapMoveToDto)
                .ToList(),
            WinningCells = game.WinningCells,
            NextPlayerId = game.CurrentTurnPlayerId
        };
    }

    private static MoveDto MapMoveToDto(Move move)
    {
        if (move == null)
            return null;
        return new MoveDto
        {
            MoveId = move.MoveId,
            GameId = move.GameId,
            PlayerId = move.PlayerId,
            Position = move.Position
        };
    }
    
    public async Task<GameDto> CheckTimeout(int gameId)
    {
        Console.WriteLine($"[Hub] CheckTimeout called: game={gameId}");

        var game = _gameService.CheckTimeout(gameId);
        var dto = MapGameToDto(game);

        // every player gets the event in the game
        await Clients.Group(game.Id.ToString())
            .SendAsync("GameUpdated", dto);

        return dto; 
    }




    private async Task AddPointsAsync(int userId, int deltaPoints)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            Console.WriteLine($"AddPointsAsync: user not found: {userId}");
            return;
        }

        user.Points += deltaPoints;
        await _userRepository.UpdateAsync(user);

        Console.WriteLine($"AddPointsAsync: user {userId} új pontszám: {user.Points}");
    }


    // creating a game
    public async Task<GameDto> CreateGame(int playerId, string playerName)
    {
        try
        {
            var game = _gameService.CreateGame(playerId, playerName);
            await Groups.AddToGroupAsync(Context.ConnectionId, game.Id.ToString());

            // entity -> DTO
            var dto = MapGameToDto(game);
            return dto;
        }
        catch (Exception ex)
        {
            Console.WriteLine("HIBA A CreateGame-BEN:");
            Console.WriteLine(ex);
            throw;
        }
    }


    public async Task<GameDto> JoinGame(string inviteCode, int playerOId, string playerOName)
    {
        var game = _gameService.JoinGame(inviteCode, playerOId, playerOName);


        await Groups.AddToGroupAsync(Context.ConnectionId, game.Id.ToString());

        var dto = MapGameToDto(game);

        // broadcasting to everyone
        await Clients.Group(game.Id.ToString())
            .SendAsync("GameUpdated", dto);

        return dto;
    }


    // Player moves
    public async Task MakeMove(int gameId, int playerId, int position)
    {
        Console.WriteLine($"[Hub] MakeMove called: game={gameId}, player={playerId}, pos={position}");
        var (game, move) = _gameService.MakeMove(gameId, playerId, position);

        var gameDto = MapGameToDto(game);
        var moveDto = MapMoveToDto(move);

        // one step event
        await Clients.Group(gameId.ToString())
            .SendAsync("MoveMade", moveDto);

        // current game
        await Clients.Group(game.Id.ToString())
            .SendAsync("GameUpdated", gameDto);

        if (game.Status == GameStatus.Finished)
        {
            if (game.WinnerId.HasValue)
            {
                // winner 2 points
                await AddPointsAsync(game.WinnerId.Value, 2);
            }
            else
            {
                // no winner 1-1points for each
                await AddPointsAsync(game.PlayerXId, 1);

                if (game.PlayerOId.HasValue)
                {
                    await AddPointsAsync(game.PlayerOId.Value, 1);
                }
            }

            await Clients.Group(game.Id.ToString())
                .SendAsync("GameFinished", gameDto);
        }
    }


    public Task<GameDto> GetGameState(int gameId)
    {
        var game = _gameService.GetGameById(gameId);
        var dto = MapGameToDto(game);
        return Task.FromResult(dto);
    }

    public async Task SendTest(string message)
    {
        // visszaküldjük ugyanannak a kliensnek
        await Clients.Caller.SendAsync("TestMessage", $"Server response: {message}");
    }

    public async Task RequestReplay(int gameId, int playerId)
    {
        var game = _gameService.RequestReplay(gameId, playerId);

        // notify opponent that someone requested replay
        await Clients.Group(gameId.ToString()).SendAsync(
            "ReplayRequested", playerId);

        // if both agreed we send reset game state
        if (game.Status == GameStatus.InProgress &&
            !game.ReplayRequestedByX &&
            !game.ReplayRequestedByO)
        {
            var dto = MapGameToDto(game);
            await Clients.Group(gameId.ToString()).SendAsync(
                "ReplayStarted",
                dto
            );
        }
    }
}