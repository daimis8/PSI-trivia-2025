namespace backend.DTOs;

public class PlayerAnswerResultDto
{
    public required string Username { get; set; }
    public bool Correct { get; set; }
    public int Points { get; set; }
    public long TimeMs { get; set; }
}

