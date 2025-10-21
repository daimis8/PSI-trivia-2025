namespace backend.Models;

public class Quiz
{
    public int ID { get; set; }

    public required int CreatorID { get; set; }

    public required string Title { get; set; }

    public required string Description { get; set; }

    public required List<QuizQuestion> Questions { get; set; }

}

public class QuizQuestion
{
    public int Id { get; set; }

    public required string QuestionText { get; set; }

    public required List<string> Options { get; set; }

    public required int CorrectOptionIndex { get; set; }

    public required int TimeLimit { get; set; }
}