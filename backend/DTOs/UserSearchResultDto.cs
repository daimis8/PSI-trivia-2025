namespace backend.DTOs;

public class UserSearchResultDto
{
    public required int UserId { get; set; }
    public required string Username { get; set; }
    public int GamesPlayed { get; set; }
    public int QuizPlays { get; set; }
}
