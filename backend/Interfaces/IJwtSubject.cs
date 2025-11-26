namespace backend.Interfaces;

public interface IJwtSubject
    {
        int Id { get; }
        string Email { get; }
        string Username { get; }
    }