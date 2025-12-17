namespace backend.DTOs;

public record QuizQuestionPublicDto(int Id, string QuestionText, List<string> Options, int TimeLimit);

