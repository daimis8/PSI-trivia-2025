namespace backend.DTOs;

public class QuizResponse
{
    public int ID { get; set; }
    public int CreatorID { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int TimesPlayed { get; set; }
    public List<QuizQuestionDto> Questions { get; set; } = new();
}

