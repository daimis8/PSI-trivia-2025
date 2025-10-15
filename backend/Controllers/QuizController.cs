using Microsoft.AspNetCore.Mvc;
using backend.Services;
using backend.Models;
using backend.DTOs;
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
        var response = quizzes.Select(MapToResponse).ToList();
        return Ok(response);
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
        var response = quizzes.Select(MapToResponse).ToList();
        return Ok(response);
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

        var response = MapToResponse(quiz);
        return Ok(response);
    }

    // Delete quiz by ID
    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteById(int id)
    {
        var result = await _quizService.DeleteQuizAsync(id);
        if (!result)
        {
            return NotFound(new { message = "Quiz not found" });
        }

        return Ok();
    }

    // Create a new quiz
    [Authorize]
    [HttpPost("my")]
    public async Task<IActionResult> Create([FromBody] CreateQuizRequest? request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (userId == null)
        {
            return Unauthorized(new { message = "Unauthorized" });
        }

        var quiz = new Quiz
        {
            Title = request?.Title ?? "New Quiz",
            Description = request?.Description ?? "",
            CreatorID = int.Parse(userId),
            Questions = new List<QuizQuestion>()
        };

        var createdQuiz = await _quizService.CreateQuizAsync(quiz);
        var response = MapToResponse(createdQuiz);
        return CreatedAtAction(nameof(GetById), new { id = createdQuiz.ID }, response);
    }

    // Update a quiz
    [Authorize]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateQuizRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (userId == null)
        {
            return Unauthorized(new { message = "Unauthorized" });
        }

        var existingQuiz = await _quizService.GetQuizByIdAsync(id);
        if (existingQuiz == null)
        {
            return NotFound(new { message = "Quiz not found" });
        }

        if (existingQuiz.CreatorID != int.Parse(userId))
        {
            return Forbid();
        }

        var quiz = new Quiz
        {
            ID = id,
            CreatorID = existingQuiz.CreatorID,
            Title = request.Title,
            Description = request.Description,
            Questions = request.Questions.Select(q => new QuizQuestion
            {
                Id = q.Id,
                QuestionText = q.QuestionText,
                Options = q.Options,
                CorrectOptionIndex = q.CorrectOptionIndex
            }).ToList()
        };

        var updatedQuiz = await _quizService.UpdateQuizAsync(id, quiz);
        
        if (updatedQuiz == null)
        {
            return NotFound(new { message = "Quiz not found" });
        }

        var response = MapToResponse(updatedQuiz);
        return Ok(response);
    }

    private static QuizResponse MapToResponse(Quiz quiz)
    {
        return new QuizResponse
        {
            ID = quiz.ID,
            CreatorID = quiz.CreatorID,
            Title = quiz.Title,
            Description = quiz.Description,
            Questions = quiz.Questions.Select(q => new QuizQuestionDto
            {
                Id = q.Id,
                QuestionText = q.QuestionText,
                Options = q.Options,
                CorrectOptionIndex = q.CorrectOptionIndex
            }).ToList()
        };
    }
}