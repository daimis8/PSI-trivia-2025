namespace backend.DTOs;

public class UserStatsDto
{
    public required int UserId { get; set; }
    public int GamesPlayed { get; set; }
    public int GamesWon { get; set; }
    public int QuizzesCreated { get; set; }
    public int QuizPlays { get; set; }
}