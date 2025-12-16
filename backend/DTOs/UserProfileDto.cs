namespace backend.DTOs;

public class UserProfileDto
{
    public required int UserId { get; set; }
    public required string Username { get; set; }
    public string? Email { get; set; }
    public required UserStatsDto Stats { get; set; }
}
