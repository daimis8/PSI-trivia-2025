namespace backend.DTOs;

public class CreateGameRequest
{
    public required int QuizId { get; set; }
}

public class CreateGameResponse
{
    public required string Code { get; set; }
}

public class LobbyPlayerDto
{
    public required string Username { get; set; }
    public bool IsHost { get; set; }
}

public class LobbyUpdateDto
{
    public required string Code { get; set; }
    public required List<LobbyPlayerDto> Players { get; set; }
}

public class QuestionDto
{
    public required int Index { get; set; }
    public required string QuestionText { get; set; }
    public required List<string> Options { get; set; }
    public required DateTimeOffset EndsAt { get; set; }
}

public class QuestionEndedDto
{
    public required int Index { get; set; }
    public required int CorrectOptionIndex { get; set; }
    public required List<PlayerAnswerResultDto> Answers { get; set; }
    public required List<LeaderboardEntryDto> Leaderboard { get; set; }
}

public class PlayerAnswerResultDto
{
    public required string Username { get; set; }
    public bool Correct { get; set; }
    public int Points { get; set; }
    public long TimeMs { get; set; }
}

public class LeaderboardEntryDto
{
    public required string Username { get; set; }
    public int Score { get; set; }
}
