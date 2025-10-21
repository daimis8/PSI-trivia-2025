namespace backend.DTOs;

public class LobbyPlayerDto
{
    public required string Username { get; set; }
    public bool IsHost { get; set; }
}

