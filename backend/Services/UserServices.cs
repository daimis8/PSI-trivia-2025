using System.Text.Json;
using backend.Models;

namespace backend.Services;

public class UserService
{
    private readonly string _filePath;
    private readonly JsonSerializerOptions _jsonOptions;

    public UserService()
    {
        _filePath = Path.Combine(Directory.GetCurrentDirectory(), "users.json");
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };
    }

    // Get all users
    public async Task<List<User>> GetAllUsersAsync()
    {
        if (!File.Exists(_filePath))
        {
            return new List<User>();
        }

        var json = await File.ReadAllTextAsync(_filePath);
        return JsonSerializer.Deserialize<List<User>>(json, _jsonOptions) ?? new List<User>();
    }

    // Add a new user
    public async Task<User> AddUserAsync(User user)
    {
        var users = await GetAllUsersAsync();

        // Auto-increment ID
        user.Id = users.Any() ? users.Max(u => u.Id) + 1 : 1;

        users.Add(user);
        await SaveUsersAsync(users);

        return user;
    }

    // Get user by username
    public async Task<User?> GetUserByUsernameAsync(string username)
    {
        var users = await GetAllUsersAsync();
        return users.FirstOrDefault(u => u.Username == username);
    }

    // Get user by email
    public async Task<User?> GetUserByEmailAsync(string email)
    {
        var users = await GetAllUsersAsync();
        return users.FirstOrDefault(u => u.Email == email);
    }

    // Save users to file
    private async Task SaveUsersAsync(List<User> users)
    {
        var json = JsonSerializer.Serialize(users, _jsonOptions);
        await File.WriteAllTextAsync(_filePath, json);
    }

    // Validate user login
    public async Task<User?> ValidateLoginAsync(string username, string password)
    {
        var users = await GetAllUsersAsync();
        return users.FirstOrDefault(u =>
            u.Username == username && u.Password == password);
    }

    // Check if file exists
    public bool FileExists() => File.Exists(_filePath);
}