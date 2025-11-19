using System.Text.RegularExpressions;
using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public class UserService
{
    private readonly AppDbContext _db;
    private readonly PasswordService _passwordService;
    private readonly UserStatsService _userStatsService;
    private static readonly Regex EmailRegex = new Regex(
        @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    public UserService(AppDbContext db, PasswordService passwordService, UserStatsService userStatsService)
    {
        _db = db;
        _passwordService = passwordService;
        _userStatsService = userStatsService;
    }

    public async Task<List<User>> GetAllUsersAsync()
    {
        return await _db.Users.AsNoTracking().ToListAsync();
    }

    public async Task<User> AddUserAsync(User user)
    {
        await _db.Users.AddAsync(user);
        await _db.SaveChangesAsync();

        await _userStatsService.AddUserStatsAsync(user.Id);
        
        return user;
    }

    // Get user by email
    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<User?> GetUserByIdAsync(int id)
    {
        return await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<User?> GetUserByUsernameAsync(string username)
    {
        return await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task<bool> DeleteUserAsync(int id)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null)
        {
            return false;
        }

        await _userStatsService.DeleteUserStatsAsync(id);
        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<User?> ValidateLoginAsync(string identifier, string password)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == identifier);

        if (user == null)
        {
            user = await _db.Users.FirstOrDefaultAsync(u => u.Username == identifier);
        }

        if (user == null)
        {
            return null;
        }

        return _passwordService.VerifyPassword(password, user.Password) ? user : null;
    }

    public bool IsUsernameValid(string username)
    {
        return _db.Users.Any(u => u.Username == username);
    }

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

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            return (null, "User not found");
        }

        var exists = await _db.Users.AnyAsync(u => u.Username == newUsername && u.Id != userId);
        if (exists)
        {
            return (null, "Username already taken");
        }

        user.Username = newUsername;
        await _db.SaveChangesAsync();

        return (user, null);
    }

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

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            return (null, "User not found");
        }

        var exists = await _db.Users.AnyAsync(u => u.Email == newEmail && u.Id != userId);
        if (exists)
        {
            return (null, "Email already taken");
        }

        user.Email = newEmail;
        await _db.SaveChangesAsync();

        return (user, null);
    }

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

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            return (false, "User not found");
        }

        if (!_passwordService.VerifyPassword(currentPassword, user.Password))
        {
            return (false, "Current password is incorrect");
        }

        user.Password = _passwordService.HashPassword(newPassword);
        await _db.SaveChangesAsync();

        return (true, null);
    }
}