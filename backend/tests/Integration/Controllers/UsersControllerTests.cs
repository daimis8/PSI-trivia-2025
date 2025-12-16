using Xunit;
using backend.Models;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http.Json;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using backend.Data;
using backend.Services;
using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;

namespace tests.Integration.Controllers;

public class UsersControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public UsersControllerTests(CustomWebApplicationFactory factory) =>
        _factory = factory;

    [Fact]
    public async Task GetUserById_ReturnsUser()
    {
        // Arrange
        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Users.Add(new User
            {
                Id = 5001,
                Username = "getuser",
                Email = "getuser@test.com",
                Password = "pass"
            });
            await db.SaveChangesAsync();
        }

        // Act
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/users/5001");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<User>();
        Assert.NotNull(user);
        Assert.Equal("getuser", user.Username);
    }

    [Fact]
    public async Task GetUserById_NonExistent_ReturnsNotFound()
    {
        // Act
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/users/99999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetCurrentUserProfile_WithValidToken_ReturnsProfile()
    {
        // Arrange
        int userId = 5101;
        string token;
        
        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            var user = new User
            {
                Id = userId,
                Username = "profileuser",
                Email = "profileuser@test.com",
                Password = "pass"
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            db.UserStats.Add(new UserStats
            {
                UserId = userId,
                GamesPlayed = 10,
                GamesWon = 5,
                QuizzesCreated = 2,
                QuizPlays = 20
            });
            await db.SaveChangesAsync();

            var jwtService = seedScope.ServiceProvider.GetRequiredService<JwtService>();
            token = jwtService.GenerateToken(user);
        }

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/users/profile");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await response.Content.ReadFromJsonAsync<dynamic>();
        Assert.NotNull(profile);
    }

    [Fact]
    public async Task GetCurrentUserProfile_WithoutToken_ReturnsUnauthorized()
    {
        // Act
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/users/profile");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetUserProfile_ReturnsProfile()
    {
        // Arrange
        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            db.Users.Add(new User
            {
                Id = 5201,
                Username = "publicprofile",
                Email = "publicprofile@test.com",
                Password = "pass"
            });
            await db.SaveChangesAsync();

            db.UserStats.Add(new UserStats
            {
                UserId = 5201,
                GamesPlayed = 15,
                GamesWon = 8,
                QuizzesCreated = 3,
                QuizPlays = 25
            });
            await db.SaveChangesAsync();
        }

        // Act
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/users/5201/profile");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await response.Content.ReadFromJsonAsync<dynamic>();
        Assert.NotNull(profile);
    }

    [Fact]
    public async Task UpdateUsername_WithValidToken_UpdatesUsername()
    {
        // Arrange
        int userId = 5301;
        string token;
        
        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            var user = new User
            {
                Id = userId,
                Username = "oldusername",
                Email = "oldusername@test.com",
                Password = "pass"
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var jwtService = seedScope.ServiceProvider.GetRequiredService<JwtService>();
            token = jwtService.GenerateToken(user);
        }

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var updateData = new { username = "newusername123" };

        // Act
        var response = await client.PutAsJsonAsync("/api/users/username", updateData);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        using (var checkScope = _factory.Services.CreateScope())
        {
            var db = checkScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await db.Users.FindAsync(userId);
            Assert.NotNull(user);
            Assert.Equal("newusername123", user.Username);
        }
    }

    [Fact]
    public async Task UpdateUsername_WithDuplicateUsername_ReturnsBadRequest()
    {
        // Arrange
        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            db.Users.Add(new User
            {
                Id = 5401,
                Username = "user1",
                Email = "user1@test.com",
                Password = "pass"
            });
            
            db.Users.Add(new User
            {
                Id = 5402,
                Username = "user2",
                Email = "user2@test.com",
                Password = "pass"
            });
            await db.SaveChangesAsync();
        }

        string token;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await db.Users.FindAsync(5402);
            var jwtService = scope.ServiceProvider.GetRequiredService<JwtService>();
            token = jwtService.GenerateToken(user!);
        }

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var updateData = new { username = "user1" }; // Duplicate

        // Act
        var response = await client.PutAsJsonAsync("/api/users/username", updateData);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEmail_WithValidToken_UpdatesEmail()
    {
        // Arrange
        int userId = 5501;
        string token;
        
        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            var user = new User
            {
                Id = userId,
                Username = "emailuser",
                Email = "oldemail@test.com",
                Password = "pass"
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var jwtService = seedScope.ServiceProvider.GetRequiredService<JwtService>();
            token = jwtService.GenerateToken(user);
        }

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var updateData = new { email = "newemail@test.com" };

        // Act
        var response = await client.PutAsJsonAsync("/api/users/email", updateData);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        using (var checkScope = _factory.Services.CreateScope())
        {
            var db = checkScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await db.Users.FindAsync(userId);
            Assert.NotNull(user);
            Assert.Equal("newemail@test.com", user.Email);
        }
    }

    [Fact]
    public async Task UpdatePassword_WithValidToken_UpdatesPassword()
    {
        // Arrange
        int userId = 5601;
        string token;
        string oldPassword = "OldPassword123!";
        
        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var passwordService = seedScope.ServiceProvider.GetRequiredService<PasswordService>();
            
            var user = new User
            {
                Id = userId,
                Username = "passworduser",
                Email = "passworduser@test.com",
                Password = passwordService.HashPassword(oldPassword)
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var jwtService = seedScope.ServiceProvider.GetRequiredService<JwtService>();
            token = jwtService.GenerateToken(user);
        }

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var updateData = new
        {
            currentPassword = oldPassword,
            newPassword = "NewPassword123!"
        };

        // Act
        var response = await client.PutAsJsonAsync("/api/users/password", updateData);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        using (var checkScope = _factory.Services.CreateScope())
        {
            var db = checkScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var passwordService = checkScope.ServiceProvider.GetRequiredService<PasswordService>();
            var user = await db.Users.FindAsync(userId);
            Assert.NotNull(user);
            Assert.True(passwordService.VerifyPassword("NewPassword123!", user.Password));
        }
    }

    [Fact]
    public async Task SearchUsers_ReturnsMatchingUsers()
    {
        // Arrange
        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            db.Users.Add(new User
            {
                Id = 5701,
                Username = "searchtest1",
                Email = "searchtest1@test.com",
                Password = "pass"
            });
            
            db.Users.Add(new User
            {
                Id = 5702,
                Username = "searchtest2",
                Email = "searchtest2@test.com",
                Password = "pass"
            });
            await db.SaveChangesAsync();
        }

        // Act
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/users/search?query=searchtest");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var users = await response.Content.ReadFromJsonAsync<System.Collections.Generic.List<dynamic>>();
        Assert.NotNull(users);
        Assert.True(users.Count >= 2);
    }

    [Fact]
    public async Task GetAll_ReturnsAllUsers()
    {
        // Arrange - Test for UsersController.cs:23-28 (GetAll method)
        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            // Add multiple users
            db.Users.AddRange(
                new User { Id = 8001, Username = "getall1", Email = "getall1@test.com", Password = "pass" },
                new User { Id = 8002, Username = "getall2", Email = "getall2@test.com", Password = "pass" },
                new User { Id = 8003, Username = "getall3", Email = "getall3@test.com", Password = "pass" }
            );
            await db.SaveChangesAsync();
        }

        // Act
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/users");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var users = await response.Content.ReadFromJsonAsync<System.Collections.Generic.List<User>>();
        Assert.NotNull(users);
        
        // Should contain our test users
        var ourUsers = users.Where(u => u.Id >= 8001 && u.Id <= 8003).ToList();
        Assert.True(ourUsers.Count >= 3, $"Expected at least 3 users, found {ourUsers.Count}");
    }

    [Fact]
    public async Task Delete_WithValidId_DeletesUser()
    {
        // Arrange - Test for UsersController.cs:93-104 (Delete method)
        int userId = 8101;
        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Users.Add(new User 
            { 
                Id = userId, 
                Username = "todelete", 
                Email = "todelete@test.com", 
                Password = "pass" 
            });
            await db.SaveChangesAsync();
        }

        // Act
        var client = _factory.CreateClient();
        var response = await client.DeleteAsync($"/api/users/{userId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.True(content.TryGetProperty("message", out var message));
        Assert.Contains("deleted", message.GetString()?.ToLower() ?? "");

        // Verify user is actually deleted
        using (var verifyScope = _factory.Services.CreateScope())
        {
            var db = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var deletedUser = await db.Users.FindAsync(userId);
            Assert.Null(deletedUser);
        }
    }

    [Fact]
    public async Task Delete_WithNonExistentId_ReturnsNotFound()
    {
        var client = _factory.CreateClient();

        // Act
        var response = await client.DeleteAsync("/api/users/999999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var content = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.True(content.TryGetProperty("message", out var message));
        Assert.Contains("not found", message.GetString()?.ToLower() ?? "");
    }

    [Fact]
    public async Task Delete_RemovesUserStats_WhenUserHasStats()
    {
        // Arrange - Test cascade delete behavior
        int userId = 8201;
        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = new User 
            { 
                Id = userId, 
                Username = "withstats", 
                Email = "withstats@test.com", 
                Password = "pass" 
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            // Add stats for this user
            db.UserStats.Add(new UserStats
            {
                UserId = userId,
                GamesPlayed = 10,
                GamesWon = 5,
                QuizzesCreated = 2,
                QuizPlays = 15
            });
            await db.SaveChangesAsync();
        }

        // Act
        var client = _factory.CreateClient();
        var response = await client.DeleteAsync($"/api/users/{userId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Verify both user and stats are deleted
        using (var verifyScope = _factory.Services.CreateScope())
        {
            var db = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var deletedUser = await db.Users.FindAsync(userId);
            var deletedStats = await db.UserStats.FirstOrDefaultAsync(s => s.UserId == userId);
            
            Assert.Null(deletedUser);
            Assert.Null(deletedStats);
        }
    }
}

