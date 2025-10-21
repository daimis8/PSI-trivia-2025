namespace backend.DTOs;

public record LobbyUpdateDto(string Code, List<LobbyPlayerDto> Players);

