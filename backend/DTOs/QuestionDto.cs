namespace backend.DTOs;

public record QuestionDto(int Index, string QuestionText, List<string> Options, DateTimeOffset EndsAt);

