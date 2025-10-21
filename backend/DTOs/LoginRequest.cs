namespace backend.DTOs;

public class LoginRequest
{
    public required string Identifier { get; set; }
    public required string Password { get; set; }
}