using backend.DTOs;
using backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace backend.Controllers;

[ApiController]
[Route("api/userstats")]
public class UserStatController : ControllerBase
{
    private readonly UserStatsService _userStatsService;

    public UserStatController(UserStatsService stats)
    {
        _userStatsService = stats;
    }

    // Get user's stats by user ID
    [HttpGet("users/{userId:int}")]
    public async Task<IActionResult> GetUserStats(int userId)
    {
        var stats = await _userStatsService.GetUserStatsAsync(userId);
        if (stats == null)
        {
            return NotFound(new { message = "User stats not found"});
        }

        return Ok(new UserStatsDto
        {
            UserId = stats.UserId,
            GamesPlayed = stats.GamesPlayed,
            GamesWon = stats.GamesWon,
            QuizzesCreated = stats.QuizzesCreated,
            QuizPlays = stats.QuizPlays
        });
    }
}