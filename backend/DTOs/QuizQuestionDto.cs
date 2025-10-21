namespace backend.DTOs;

public class QuizQuestionDto
{
    public int Id { get; set; }
    public required string QuestionText { get; set; }
    public required List<string> Options { get; set; }
    public required int CorrectOptionIndex { get; set; }
    public required int TimeLimit { get; set; }
}

