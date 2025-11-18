namespace backend.Models;

public class UserStats
{
    public int UserId { get; set; }
    public int GamesPlayed { get; set; }
    public int GamesWon { get; set; }
    public int QuizzesCreated { get; set; }
    public int QuizPlays { get; set; }
}