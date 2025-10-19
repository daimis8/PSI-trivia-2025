namespace backend.DTOs;

public class CreateQuizRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
}

public class UpdateQuizRequest
{
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required List<QuizQuestionDto> Questions { get; set; }
}

public class QuizQuestionDto
{
    public int Id { get; set; }
    public required string QuestionText { get; set; }
    public required List<string> Options { get; set; }
    public required int CorrectOptionIndex { get; set; }
    public required int TimeLimit { get; set; }
}

public class QuizResponse
{
    public int ID { get; set; }
    public int CreatorID { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<QuizQuestionDto> Questions { get; set; } = new();
}
