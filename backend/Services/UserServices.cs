using backend.Models;

namespace backend.Services;

public class UserService
{
    private readonly DataStorage<User> _storage;
    private readonly PasswordService _passwordService;

    public UserService(PasswordService passwordService)
    {
        _storage = new DataStorage<User>("users.json");
        _passwordService = passwordService;
    }

    // Get all users
    public Task<List<User>> GetAllUsersAsync()
    {
        return Task.FromResult(_storage.GetAll().ToList());
    }

    // Add a new user
    public async Task<User> AddUserAsync(User user)
    {
        var users = _storage.GetAll().ToList();
        
        user.Id = users.Any() ? users.Max(u => u.Id) + 1 : 1;
        
        await _storage.SetAsync(user.Email, user);
        
        return user;
    }

    // Get user by email
    public Task<User?> GetUserByEmailAsync(string email)
    {
        var user = _storage.Get(email);
        return Task.FromResult(user);
    }

    // Get user by ID
    public Task<User?> GetUserByIdAsync(int id)
    {
        var user = _storage.GetAll().FirstOrDefault(u => u.Id == id);
        return Task.FromResult(user);
    }

    // Delete user by ID
    public async Task<bool> DeleteUserAsync(int id)
    {
        var user = _storage.GetAll().FirstOrDefault(u => u.Id == id);
        if (user != null)
        {
            return await _storage.RemoveAsync(user.Email);
        }
        return false;
    }

    // Validate user login
    public Task<User?> ValidateLoginAsync(string email, string password)
    {
        var user = _storage.Get(email);
        
        if (user == null)
        {
            return Task.FromResult<User?>(null);
        }
        
        bool isPasswordValid = _passwordService.VerifyPassword(password, user.Password);
        
        return Task.FromResult(isPasswordValid ? user : null);
    }
}