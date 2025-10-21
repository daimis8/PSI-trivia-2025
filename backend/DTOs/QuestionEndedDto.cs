namespace backend.DTOs;

public class QuestionEndedDto
{
    public required int Index { get; set; }
    public required int CorrectOptionIndex { get; set; }
    public required List<PlayerAnswerResultDto> Answers { get; set; }
    public required List<LeaderboardEntryDto> Leaderboard { get; set; }
}

