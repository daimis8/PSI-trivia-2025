using Microsoft.AspNetCore.Mvc;
using backend.Services;
using backend.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace backend.Controllers;

[ApiController]
[Route("api/quizzes")]
public class QuizController : ControllerBase
{
    private readonly UserService _userService;
    private readonly QuizService _quizService;

    public QuizController(UserService userService, QuizService quizService)
    {
        _userService = userService;
        _quizService = quizService;
    }

    // Get all quizes
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var quizzes = await _quizService.GetAllQuizzesAsync();
        return Ok(quizzes);
    }

    // Get all personal quizes
    [Authorize]
    [HttpGet("my")]
    public async Task<IActionResult> GetAllPersonal()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (userId == null)
        {
            return Unauthorized(new { message = "Unauthorized" });
        }

        var quizzes = await _quizService.GetQuizzesByUserIdAsync(userId);
        return Ok(quizzes);
    }

    // Get quiz by ID
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var quiz = await _quizService.GetQuizByIdAsync(id);
        if (quiz == null)
        {
            return NotFound(new { message = "Quiz not found" });
        }

        return Ok(quiz);
    }

    // Create a new quiz
    [Authorize]
    [HttpPost("my")]
    public async Task<IActionResult> Create()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (userId == null)
        {
            return Unauthorized(new { message = "Unauthorized" });
        }

        var quiz = new Quiz
        {
            Title = "New Quiz",
            Description = "",
            CreatorID = int.Parse(userId),
            Questions = new List<QuizQuestion>()
        };

        var createdQuiz = await _quizService.CreateQuizAsync(quiz);
        return CreatedAtAction(nameof(GetById), new { id = createdQuiz.ID }, createdQuiz);
    }
}