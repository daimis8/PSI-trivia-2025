namespace backend.DTOs;

public class UpdateQuizRequest
{
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required List<QuizQuestionDto> Questions { get; set; }
}

