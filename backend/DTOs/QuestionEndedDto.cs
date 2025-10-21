namespace backend.DTOs;

public record QuestionEndedDto(int Index, int CorrectOptionIndex, List<PlayerAnswerResultDto> Answers, List<LeaderboardEntryDto> Leaderboard);

