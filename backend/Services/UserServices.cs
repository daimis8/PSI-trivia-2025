using backend.Models;
using backend.Extensions;
using System.Text.RegularExpressions;

namespace backend.Services;

public class UserService
{
    private readonly DataStorage<string, User> _storage;
    private readonly PasswordService _passwordService;
    private readonly UserStatsService _userStatsService;
    private static readonly Regex EmailRegex = new Regex(
        @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    public UserService(PasswordService passwordService, UserStatsService userStatsService)
    {
        _storage = new DataStorage<string, User>("users.json");
        _passwordService = passwordService;
        _userStatsService = userStatsService;
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
        
        user.Id = users.IsNullOrEmpty() ? 1 : users.Max(u => u.Id) + 1;
        
        await _storage.SetAsync(user.Email, user);

        await _userStatsService.AddUserStatsAsync(user.Id);
        
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
    // Get user by username
    public Task<User?> GetUserByUsernameAsync(string username)
    {
        var user = _storage.GetAll().FirstOrDefault(u => u.Username == username);
        return Task.FromResult(user);
    }

    // Delete user by ID
    public async Task<bool> DeleteUserAsync(int id)
    {
        var user = _storage.GetAll().FirstOrDefault(u => u.Id == id);
        if (user != null)
        {
            await _userStatsService.DeleteUserStatsAsync(id);
            return await _storage.RemoveAsync(user.Email);
        }
        return false;
    }

    // Validate user login with either email or username
    public Task<User?> ValidateLoginAsync(string identifier, string password)
    {
        var user = _storage.Get(identifier);
        
        if (user == null)
        {
            user = _storage.GetAll().FirstOrDefault(u => u.Username == identifier);
        }
        
        if (user == null)
        {
            return Task.FromResult<User?>(null);
        }
        
        bool isPasswordValid = _passwordService.VerifyPassword(password, user.Password);
        return Task.FromResult(isPasswordValid ? user : null);
    }

    public bool IsUsernameValid(string username)
    {
        return _storage.GetAll().Any(u => u.Username == username);
    }

    // Update username
    public async Task<(User? user, string? error)> UpdateUsernameAsync(int userId, string newUsername)
    {
        if (string.IsNullOrWhiteSpace(newUsername))
        {
            return (null, "Username cannot be empty");
        }

        if (newUsername.Length < 3)
        {
            return (null, "Username must be at least 3 characters long");
        }

        var user = _storage.GetAll().FirstOrDefault(u => u.Id == userId);
        
        if (user == null)
        {
            return (null, "User not found");
        }

        var existingUser = _storage.GetAll().FirstOrDefault(u => u.Username == newUsername && u.Id != userId);
        if (existingUser != null)
        {
            return (null, "Username already taken");
        }

        user.Username = newUsername;
        await _storage.SetAsync(user.Email, user);
        
        return (user, null);
    }

    // Update email
    public async Task<(User? user, string? error)> UpdateEmailAsync(int userId, string newEmail)
    {
        if (string.IsNullOrWhiteSpace(newEmail))
        {
            return (null, "Email cannot be empty");
        }

        if (!EmailRegex.IsMatch(newEmail))
        {
            return (null, "Invalid email format");
        }

        var user = _storage.GetAll().FirstOrDefault(u => u.Id == userId);
        
        if (user == null)
        {
            return (null, "User not found");
        }

        var existingUser = _storage.GetAll().FirstOrDefault(u => u.Email == newEmail && u.Id != userId);
        if (existingUser != null)
        {
            return (null, "Email already taken");
        }

        var oldEmail = user.Email;
        user.Email = newEmail;
        
        await _storage.RemoveAsync(oldEmail);
        await _storage.SetAsync(newEmail, user);
        
        return (user, null);
    }

    // Update password
    public async Task<(bool success, string? error)> UpdatePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword))
        {
            return (false, "Password cannot be empty");
        }

        if (newPassword.Length < 8)
        {
            return (false, "Password must be at least 8 characters long");
        }

        var user = _storage.GetAll().FirstOrDefault(u => u.Id == userId);
        
        if (user == null)
        {
            return (false, "User not found");
        }

        if (!_passwordService.VerifyPassword(currentPassword, user.Password))
        {
            return (false, "Current password is incorrect");
        }

        user.Password = _passwordService.HashPassword(newPassword);
        await _storage.SetAsync(user.Email, user);
        
        return (true, null);
    }
}