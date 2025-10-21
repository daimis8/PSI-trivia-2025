using backend.DTOs;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace backend.Controllers;

[ApiController]
[Route("api/games")]
public class GameController : ControllerBase
{
    private readonly GameService _gameService;
    private readonly QuizService _quizService;

    public GameController(GameService gameService, QuizService quizService)
    {
        _gameService = gameService;
        _quizService = quizService;
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateGameRequest req)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
        {
            return Unauthorized(new { message = "Unauthorized" });
        }

        var quiz = await _quizService.GetQuizByIdAsync(req.QuizId);
        if (quiz == null)
        {
            return NotFound(new { message = "Quiz not found" });
        }

        if (quiz.CreatorID != int.Parse(userId))
        {
            return Forbid();
        }

        var game = _gameService.CreateGame(int.Parse(userId), quiz.ID, quiz.Questions);
        return Ok(new CreateGameResponse { Code = game.Code });
    }

    // Check if a game with code exists (no auth required)
    [HttpGet("{code}/exists")]
    public IActionResult Exists(string code)
    {
        if (_gameService.TryGetGame(code, out var game) && game != null)
        {
            return Ok(new { exists = true, phase = game.Phase.ToString() });
        }
        return NotFound(new { exists = false });
    }
}
