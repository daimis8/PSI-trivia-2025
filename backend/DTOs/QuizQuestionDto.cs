namespace backend.DTOs;

public record QuizQuestionDto(int Id, string QuestionText, List<string> Options, int CorrectOptionIndex, int TimeLimit);

