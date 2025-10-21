namespace backend.DTOs;

public class LobbyUpdateDto
{
    public required string Code { get; set; }
    public required List<LobbyPlayerDto> Players { get; set; }
}

