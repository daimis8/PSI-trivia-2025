namespace backend.DTOs;

public class UpdateUsernameRequest
{
    public required string Username { get; set; }
}

public class UpdateEmailRequest
{
    public required string Email { get; set; }
}

public class UpdatePasswordRequest
{
    public required string CurrentPassword { get; set; }
    public required string NewPassword { get; set; }
}

