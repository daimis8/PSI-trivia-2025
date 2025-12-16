using Xunit;
using backend.Services;
using backend.Models;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using backend.Data;

namespace tests.Integration.Services;

public class UserServiceExtendedTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public UserServiceExtendedTests(CustomWebApplicationFactory factory) =>
        _factory = factory;

    [Fact]
    public async Task GetAllUsers_ReturnsAllUsers()
    {
        // Arrange
        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            db.Users.Add(new User { Id = 10001, Username = "user10001", Email = "user10001@test.com", Password = "pass" });
            db.Users.Add(new User { Id = 10002, Username = "user10002", Email = "user10002@test.com", Password = "pass" });
            await db.SaveChangesAsync();
        }

        // Act
        using (var scope = _factory.Services.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<UserService>();
            var users = await service.GetAllUsersAsync();

            // Assert
            Assert.NotNull(users);
            var ourUsers = users.Where(u => u.Id >= 10001 && u.Id <= 10002).ToList();
            Assert.Equal(2, ourUsers.Count);
        }
    }

    [Fact]
    public async Task GetUserById_ExistingUser_ReturnsUser()
    {
        // Arrange
        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Users.Add(new User { Id = 10101, Username = "specificuser", Email = "specificuser@test.com", Password = "pass" });
            await db.SaveChangesAsync();
        }

        // Act
        using (var scope = _factory.Services.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<UserService>();
            var user = await service.GetUserByIdAsync(10101);

            // Assert
            Assert.NotNull(user);
            Assert.Equal("specificuser", user.Username);
        }
    }

    [Fact]
    public async Task GetUserById_NonExistent_ReturnsNull()
    {
        // Act
        using (var scope = _factory.Services.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<UserService>();
            var user = await service.GetUserByIdAsync(99999);

            // Assert
            Assert.Null(user);
        }
    }

    [Fact]
    public async Task GetUserByUsername_ExistingUser_ReturnsUser()
    {
        // Arrange
        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Users.Add(new User { Id = 10201, Username = "findmebyname", Email = "findmebyname@test.com", Password = "pass" });
            await db.SaveChangesAsync();
        }

        // Act
        using (var scope = _factory.Services.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<UserService>();
            var user = await service.GetUserByUsernameAsync("findmebyname");

            // Assert
            Assert.NotNull(user);
            Assert.Equal("findmebyname@test.com", user.Email);
        }
    }

    [Fact]
    public async Task GetUserByUsername_NonExistent_ReturnsNull()
    {
        // Act
        using (var scope = _factory.Services.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<UserService>();
            var user = await service.GetUserByUsernameAsync("nonexistentuser");

            // Assert
            Assert.Null(user);
        }
    }

    [Fact]
    public async Task UpdatePassword_WithCorrectOldPassword_UpdatesPassword()
    {
        // Arrange
        string oldPassword = "OldPass123";
        string newPassword = "NewPass456";

        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var passwordService = seedScope.ServiceProvider.GetRequiredService<PasswordService>();
            
            db.Users.Add(new User 
            { 
                Id = 10301, 
                Username = "passchange", 
                Email = "passchange@test.com", 
                Password = passwordService.HashPassword(oldPassword)
            });
            await db.SaveChangesAsync();
        }

        // Act
        using (var scope = _factory.Services.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<UserService>();
            var (success, error) = await service.UpdatePasswordAsync(10301, oldPassword, newPassword);

            // Assert
            Assert.True(success);
            Assert.Null(error);
        }

        // Verify password was actually updated
        using (var checkScope = _factory.Services.CreateScope())
        {
            var db = checkScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var passwordService = checkScope.ServiceProvider.GetRequiredService<PasswordService>();
            var user = await db.Users.FindAsync(10301);
            
            Assert.NotNull(user);
            Assert.True(passwordService.VerifyPassword(newPassword, user.Password));
        }
    }

    [Fact]
    public async Task UpdatePassword_WithIncorrectOldPassword_Fails()
    {
        // Arrange
        string oldPassword = "OldPass123";
        string wrongPassword = "WrongPass";

        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var passwordService = seedScope.ServiceProvider.GetRequiredService<PasswordService>();
            
            db.Users.Add(new User 
            { 
                Id = 10401, 
                Username = "wrongpass", 
                Email = "wrongpass@test.com", 
                Password = passwordService.HashPassword(oldPassword)
            });
            await db.SaveChangesAsync();
        }

        // Act
        using (var scope = _factory.Services.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<UserService>();
            var (success, error) = await service.UpdatePasswordAsync(10401, wrongPassword, "NewPass123");

            // Assert
            Assert.False(success);
            Assert.NotNull(error);
            Assert.Contains("current password", error.ToLower());
        }
    }

    [Fact]
    public async Task SearchUsers_FindsMatchingUsers()
    {
        // Arrange
        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            db.Users.Add(new User { Id = 10501, Username = "searchable1", Email = "searchable1@test.com", Password = "pass" });
            db.Users.Add(new User { Id = 10502, Username = "searchable2", Email = "searchable2@test.com", Password = "pass" });
            db.Users.Add(new User { Id = 10503, Username = "different", Email = "different@test.com", Password = "pass" });
            await db.SaveChangesAsync();

            db.UserStats.Add(new UserStats { UserId = 10501, GamesPlayed = 0, GamesWon = 0, QuizzesCreated = 0, QuizPlays = 0 });
            db.UserStats.Add(new UserStats { UserId = 10502, GamesPlayed = 0, GamesWon = 0, QuizzesCreated = 0, QuizPlays = 0 });
            db.UserStats.Add(new UserStats { UserId = 10503, GamesPlayed = 0, GamesWon = 0, QuizzesCreated = 0, QuizPlays = 0 });
            await db.SaveChangesAsync();
        }

        // Act
        using (var scope = _factory.Services.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<UserService>();
            var results = await service.SearchUsersAsync("searchable");

            // Assert
            Assert.NotNull(results);
            var ourResults = results.Where(r => r.UserId >= 10501 && r.UserId <= 10502).ToList();
            Assert.Equal(2, ourResults.Count);
            Assert.All(ourResults, r => Assert.Contains("searchable", r.Username.ToLower()));
        }
    }

    [Fact]
    public async Task GetUserProfile_WithStats_ReturnsCompleteProfile()
    {
        // Arrange
        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            db.Users.Add(new User { Id = 10601, Username = "profileuser", Email = "profileuser@test.com", Password = "pass" });
            await db.SaveChangesAsync();

            db.UserStats.Add(new UserStats 
            { 
                UserId = 10601, 
                GamesPlayed = 50, 
                GamesWon = 25, 
                QuizzesCreated = 10, 
                QuizPlays = 100 
            });
            await db.SaveChangesAsync();
        }

        // Act
        using (var scope = _factory.Services.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<UserService>();
            var profile = await service.GetUserProfileAsync(10601);

        // Assert
        Assert.NotNull(profile);
        Assert.Equal("profileuser", profile.Username);
        Assert.NotNull(profile.Stats);
        Assert.Equal(50, profile.Stats.GamesPlayed);
        Assert.Equal(25, profile.Stats.GamesWon);
        Assert.Equal(10, profile.Stats.QuizzesCreated);
        Assert.Equal(100, profile.Stats.QuizPlays);
        }
    }

    [Fact]
    public async Task ValidateLogin_WithEmail_ValidatesCorrectly()
    {
        // Arrange
        string password = "TestPass123";

        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var passwordService = seedScope.ServiceProvider.GetRequiredService<PasswordService>();
            
            db.Users.Add(new User 
            { 
                Id = 10701, 
                Username = "loginuser", 
                Email = "loginuser@test.com", 
                Password = passwordService.HashPassword(password)
            });
            await db.SaveChangesAsync();
        }

        // Act
        using (var scope = _factory.Services.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<UserService>();
            var user = await service.ValidateLoginAsync("loginuser@test.com", password);

            // Assert
            Assert.NotNull(user);
            Assert.Equal("loginuser", user.Username);
        }
    }

    [Fact]
    public async Task ValidateLogin_WithUsername_ValidatesCorrectly()
    {
        // Arrange
        string password = "TestPass123";

        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var passwordService = seedScope.ServiceProvider.GetRequiredService<PasswordService>();
            
            db.Users.Add(new User 
            { 
                Id = 10801, 
                Username = "loginbyname", 
                Email = "loginbyname@test.com", 
                Password = passwordService.HashPassword(password)
            });
            await db.SaveChangesAsync();
        }

        // Act
        using (var scope = _factory.Services.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<UserService>();
            var user = await service.ValidateLoginAsync("loginbyname", password);

            // Assert
            Assert.NotNull(user);
            Assert.Equal("loginbyname@test.com", user.Email);
        }
    }

    [Fact]
    public async Task ValidateLogin_WithWrongPassword_ReturnsNull()
    {
        // Arrange
        string password = "CorrectPass";

        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var passwordService = seedScope.ServiceProvider.GetRequiredService<PasswordService>();
            
            db.Users.Add(new User 
            { 
                Id = 10901, 
                Username = "wrongpassuser", 
                Email = "wrongpassuser@test.com", 
                Password = passwordService.HashPassword(password)
            });
            await db.SaveChangesAsync();
        }

        // Act
        using (var scope = _factory.Services.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<UserService>();
            var user = await service.ValidateLoginAsync("wrongpassuser@test.com", "WrongPassword");

            // Assert
            Assert.Null(user);
        }
    }
}

