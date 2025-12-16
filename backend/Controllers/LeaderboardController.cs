using Microsoft.AspNetCore.Mvc;
using backend.Services;
using backend.DTOs;

namespace backend.Controllers;

[ApiController]
[Route("api/leaderboard")]
public class LeaderboardController : ControllerBase
{
    private readonly UserStatsService _userStatsService;
    private readonly QuizService _quizService;

    public LeaderboardController(UserStatsService userStatsService, QuizService quizService)
    {
        _userStatsService = userStatsService;
        _quizService = quizService;
    }

    // Get top players by games won
    [HttpGet("top-players")]
    public async Task<IActionResult> GetTopPlayers([FromQuery] int limit = 10)
    {
        if (limit < 1 || limit > 100)
        {
            return BadRequest(new { message = "Limit must be between 1 and 100" });
        }

        var topPlayers = await _userStatsService.GetTopPlayersAsync(limit);
        var response = topPlayers.Select(p => new TopPlayerDto(
            UserId: p.Item1,
            Username: p.Item2,
            GamesWon: p.Item3
        )).ToList();

        return Ok(response);
    }

    // Get top quizzes by times played
    [HttpGet("top-quizzes")]
    public async Task<IActionResult> GetTopQuizzes([FromQuery] int limit = 10)
    {
        if (limit < 1 || limit > 100)
        {
            return BadRequest(new { message = "Limit must be between 1 and 100" });
        }

        var topQuizzes = await _quizService.GetTopQuizzesAsync(limit);
        var response = topQuizzes.Select(q => new TopQuizDto(
            QuizId: q.Item1,
            Title: q.Item2,
            CreatorUsername: q.Item3,
            TimesPlayed: q.Item4
        )).ToList();

        return Ok(response);
    }
}

