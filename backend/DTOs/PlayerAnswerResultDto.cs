namespace backend.DTOs;

public record PlayerAnswerResultDto(string Username, bool Correct, int Points, long TimeMs);

