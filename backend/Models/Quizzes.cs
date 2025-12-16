using System.Text.Json.Serialization;

namespace backend.Models;

public class Quiz
{
    public int ID { get; set; }

    public int CreatorID { get; set; }

    [JsonIgnore]
    public User? Creator { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int TimesPlayed { get; set; }

    public List<QuizQuestion> Questions { get; set; } = new();

}

public class QuizQuestion
{
    public int Id { get; set; }

    public string QuestionText { get; set; } = string.Empty;

    public List<string> Options { get; set; } = new();

    public int CorrectOptionIndex { get; set; }

    public int TimeLimit { get; set; }

    [JsonIgnore]
    public int QuizId { get; set; }

    [JsonIgnore]
    public Quiz? Quiz { get; set; }
}