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

    private static GameDTO MapGameToDto(Game game)
    {
        return new GameDTO
        {
            Id = game.Id,
            PlayerXId = game.PlayerXId,
            PlayerOId = game.PlayerOId,
            InviteCode = game.InviteCode,
            WinnerId = game.WinnerId,
            Status = game.Status.ToString(), // enum -> string
            CreatedAt = game.CreatedAt,
            Board = game.Board,
            Moves = game.Moves
                .Select(MapMoveToDto)
                .ToList()
        };
    }

    private static MoveDTO MapMoveToDto(Move move)
    {
        return new MoveDTO
        {
            MoveId = move.MoveId,
            GameId = move.GameId,
            PlayerId = move.PlayerId,
            Position = move.Position
        };
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


    // creating a gaem
    public async Task<GameDTO> CreateGame(int playerId)
    {
        try
        {
            var game = _gameService.CreateGame(playerId);


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


    public async Task<GameDTO> JoinGame(string inviteCode, int playerOId)
    {
        var game = _gameService.JoinGame(inviteCode, playerOId);


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
        Game game;
        Move? move = null;

        try
        {
            (game, move) = _gameService.MakeMove(gameId, playerId, position);
        }
        catch (TimeoutException)
        {
            // lose because of timer 
            game = _gameService.GetGameById(gameId)
                   ?? throw new Exception("Game not found after timeout.");

            // no new move here
        }

        var gameDto = MapGameToDto(game);

        if (move is not null)
        {
            var moveDto = MapMoveToDto(move);

            await Clients.Group(gameId.ToString())
                .SendAsync("MoveMade", moveDto);
        }

        await Clients.Group(game.Id.ToString())
            .SendAsync("GameUpdated", gameDto);

        if (game.Status == GameStatus.Finished)
        {
            if (game.WinnerId.HasValue)
            {
                await AddPointsAsync(game.WinnerId.Value, 2);
            }
            else
            {
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

    


    public Task<GameDTO> GetGameState(int gameId)
    {
        var game = _gameService.GetGameById(gameId);
        var dto = MapGameToDto(game);
        return Task.FromResult(dto);
    }

    public async Task SendTest(string message)
    {
        // visszaküldjük ugyanannak a kliensnek
        await Clients.Caller.SendAsync("TestMessage", $"Szerver válasza: {message}");
    }
}