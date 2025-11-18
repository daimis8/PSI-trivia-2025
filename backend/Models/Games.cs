using System.Collections.Concurrent;

namespace backend.Models;

public enum GamePhase
{
    Lobby,
    Question,
    Leaderboard,
    Ended
}

public class GamePlayer
{
    public required string ConnectionId { get; set; }
    public int? UserId { get; set; }
    public required string Username { get; set; }
    public int TotalScore { get; set; } = 0;
    public bool HasAnsweredCurrent { get; set; } = false;
    public int? SelectedOptionIndex { get; set; }
    public long? AnswerTimeMs { get; set; }
}

public class PlayerAnswerSummary
{
    public required string Username { get; set; }
    public bool Correct { get; set; }
    public int Points { get; set; }
    public long TimeMs { get; set; }
}

public class LeaderboardEntry : IComparable<LeaderboardEntry>
{
    public required string Username { get; set; }
    public int Score { get; set; }

    public int CompareTo(LeaderboardEntry? other)
    {
        if (other == null) return 1;
        
        int scoreComparison = other.Score.CompareTo(this.Score);
        
        if (scoreComparison == 0)
        {
            return string.Compare(this.Username, other.Username, StringComparison.OrdinalIgnoreCase);
        }
        
        return scoreComparison;
    }
}

public class Game
{
    public required string Code { get; set; }
    public required int HostUserId { get; set; }
    public string? HostConnectionId { get; set; }

    public required int QuizId { get; set; }
    public required List<QuizQuestion> Questions { get; set; }

    public int CurrentQuestionIndex { get; set; } = -1;
    public GamePhase Phase { get; set; } = GamePhase.Lobby;

    public DateTimeOffset? QuestionStartTime { get; set; }
    public DateTimeOffset? QuestionEndTime { get; set; }
    public int QuestionDurationSeconds { get; set; } = 15;

    public ConcurrentDictionary<string, GamePlayer> Players { get; } = new();

    public CancellationTokenSource? CurrentTimerCts { get; set; }

    public bool StatsRecorded { get; set; } = false;
}
