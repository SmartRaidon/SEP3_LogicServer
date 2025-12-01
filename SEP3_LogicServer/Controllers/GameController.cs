using ApiContracts;
using ApiContracts.Game;
using Entities;
using Microsoft.AspNetCore.Mvc;
using SEP3_LogicServer.Services;

namespace SEP3_LogicServer.Controllers;
[ApiController]
[Route("api/[controller]")]
public class GameController : ControllerBase        
{
    private readonly GameService gameService;
    public GameController(GameService gameService)
    {
        this.gameService = gameService;
    }

    [HttpGet("{gameId}")]
    public ActionResult<GameDTO> GetById(int gameId)
    {
        Game? game = gameService.GetGameById(gameId);
        if (game == null)
        {
            return NotFound("Game not found");
        }

        GameDTO dto = new()
        {
            Id = game.Id,
            PlayerXId = game.PlayerXId,
            PlayerOId = game.PlayerOId,
            InviteCode = game.InviteCode,
            WinnerId = game.WinnerId,
            Status = game.Status.ToString(),
            CreatedAt = game.CreatedAt
        };
        return Ok(dto);

    }

    [HttpPost("create")]
    public ActionResult<GameDTO> Create([FromBody] CreateGameDTO request)
    {
        //genereate Invite Code 10 characters
        string inviteCode = GenerateInviteCode();
        
        Game game = gameService.CreateGame(request.PlayerId, inviteCode);

          
        GameDTO dto = new()
        {
            Id = game.Id,
            PlayerXId = game.PlayerXId,
            PlayerOId = game.PlayerOId,
            InviteCode = game.InviteCode,
            WinnerId = game.WinnerId,
            Status = game.Status.ToString(),
            CreatedAt = game.CreatedAt
        };
        return CreatedAtAction(nameof(GetById),$"/game/{game.Id}", dto);
    }

    [HttpPost("join")]
    public ActionResult<GameDTO> JoinMatch([FromBody] JoinGameDTO request)
    {
        Game? game = gameService.GetGameByInviteCode(request.InviteCode);
        if (game == null)
        {
            return NotFound("Game not found");
        }

        if (game.Status != GameStatus.WaitingForOpponent)
        {
            return BadRequest("Game is not waiting for opponent");
        }

        if (game.PlayerOId !=null)
        {
            return BadRequest("Game already has two players");
        }
        game.Status = GameStatus.InProgress;
        gameService.UpdateGame(game);
        GameDTO dto = new()
        {
            Id = game.Id,
            PlayerXId = game.PlayerXId,
            PlayerOId = game.PlayerOId,
            InviteCode = game.InviteCode,
            WinnerId = game.WinnerId,
            Status = game.Status.ToString(),
            CreatedAt = game.CreatedAt
        };
        return Ok(dto);
    }
    
    
    
    
    private string GenerateInviteCode()
    {
        return Convert.ToHexString(Guid.NewGuid().ToByteArray())
            .Substring(0, 10);
    }
}