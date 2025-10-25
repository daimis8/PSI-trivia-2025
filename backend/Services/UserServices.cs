using backend.Models;
using backend.Data;
using backend.Extensions;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace backend.Services;

public class UserService
{
    private readonly ApplicationDbContext _context;
    private readonly PasswordService _passwordService;
    private static readonly Regex EmailRegex = new Regex(
        @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    public UserService(ApplicationDbContext context, PasswordService passwordService)
    {
        _context = context;
        _passwordService = passwordService;
    }

    // Get all users
    public async Task<List<User>> GetAllUsersAsync()
    {
        return await _context.Users.ToListAsync();
    }

    // Add a new user
    public async Task<User> AddUserAsync(User user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    // Get user by email
    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    // Get user by ID
    public async Task<User?> GetUserByIdAsync(int id)
    {
        return await _context.Users.FindAsync(id);
    }

    // Get user by username
    public async Task<User?> GetUserByUsernameAsync(string username)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
    }

    // Delete user by ID
    public async Task<bool> DeleteUserAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user != null)
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }
        return false;
    }

    // Validate user login with either email or username
    public async Task<User?> ValidateLoginAsync(string identifier, string password)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == identifier || u.Username == identifier);

        if (user == null)
        {
            return null;
        }

        bool isPasswordValid = _passwordService.VerifyPassword(password, user.Password);
        return isPasswordValid ? user : null;
    }

    public async Task<bool> IsUsernameValidAsync(string username)
    {
        return await _context.Users.AnyAsync(u => u.Username == username);
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

        var user = await _context.Users.FindAsync(userId);

        if (user == null)
        {
            return (null, "User not found");
        }

        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == newUsername && u.Id != userId);
        if (existingUser != null)
        {
            return (null, "Username already taken");
        }

        user.Username = newUsername;
        _context.Users.Update(user);
        await _context.SaveChangesAsync();

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

        var user = await _context.Users.FindAsync(userId);

        if (user == null)
        {
            return (null, "User not found");
        }

        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == newEmail && u.Id != userId);
        if (existingUser != null)
        {
            return (null, "Email already taken");
        }

        user.Email = newEmail;
        _context.Users.Update(user);
        await _context.SaveChangesAsync();

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

        var user = await _context.Users.FindAsync(userId);

        if (user == null)
        {
            return (false, "User not found");
        }

        if (!_passwordService.VerifyPassword(currentPassword, user.Password))
        {
            return (false, "Current password is incorrect");
        }

        user.Password = _passwordService.HashPassword(newPassword);
        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        return (true, null);
    }
}