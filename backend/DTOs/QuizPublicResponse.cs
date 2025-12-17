namespace backend.DTOs;

public class QuizPublicResponse
{
    public int ID { get; set; }
    public int CreatorID { get; set; }
    public string CreatorUsername { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int TimesPlayed { get; set; }
    public List<QuizQuestionPublicDto> Questions { get; set; } = new();
}

