namespace backend.DTOs;

public class QuestionDto
{
    public required int Index { get; set; }
    public required string QuestionText { get; set; }
    public required List<string> Options { get; set; }
    public required DateTimeOffset EndsAt { get; set; }
}

