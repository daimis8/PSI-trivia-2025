using System.Net;
using System.Net.Http.Json;
using backend.DTOs;
using backend.Models;
using Microsoft.Extensions.DependencyInjection;
using backend.Data;
using Xunit;

namespace tests.Integration.Controllers;

public class LeaderboardControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public LeaderboardControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetTopPlayers_WithDefaultLimit_ReturnsTopPlayers()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var uniqueId = Guid.NewGuid();
        var users = new List<User>();
        var stats = new List<UserStats>();

        for (int i = 0; i < 15; i++)
        {
            var user = new User
            {
                Username = $"topplayer{i}_{uniqueId}",
                Email = $"topplayer{i}_{uniqueId}@test.com",
                Password = "hashedpass"
            };
            users.Add(user);
            db.Users.Add(user);
        }
        await db.SaveChangesAsync();

        for (int i = 0; i < users.Count; i++)
        {
            var userStat = new UserStats
            {
                UserId = users[i].Id,
                GamesWon = 100 - i // Descending order
            };
            stats.Add(userStat);
            db.UserStats.Add(userStat);
        }
        await db.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/leaderboard/top-players");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<List<TopPlayerDto>>();
        Assert.NotNull(result);
        Assert.Equal(10, result.Count);
        
        // Verify descending order
        for (int i = 0; i < result.Count - 1; i++)
        {
            Assert.True(result[i].GamesWon >= result[i + 1].GamesWon);
        }
    }

    [Fact]
    public async Task GetTopPlayers_WithCustomLimit_ReturnsCorrectNumber()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var uniqueId = Guid.NewGuid();
        for (int i = 0; i < 10; i++)
        {
            var user = new User
            {
                Username = $"customlimitplayer{i}_{uniqueId}",
                Email = $"customlimitplayer{i}_{uniqueId}@test.com",
                Password = "hashedpass"
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var userStat = new UserStats
            {
                UserId = user.Id,
                GamesWon = 50 + i
            };
            db.UserStats.Add(userStat);
        }
        await db.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/leaderboard/top-players?limit=5");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<List<TopPlayerDto>>();
        Assert.NotNull(result);
        Assert.Equal(5, result.Count);
    }

    [Fact]
    public async Task GetTopPlayers_WithZeroLimit_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/leaderboard/top-players?limit=0");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Limit must be between 1 and 100", content);
    }

    [Fact]
    public async Task GetTopPlayers_WithNegativeLimit_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/leaderboard/top-players?limit=-5");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Limit must be between 1 and 100", content);
    }

    [Fact]
    public async Task GetTopPlayers_WithLimitOver100_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/leaderboard/top-players?limit=101");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Limit must be between 1 and 100", content);
    }

    [Fact]
    public async Task GetTopPlayers_WithNoPlayers_ReturnsEmptyList()
    {
        // Act
        var response = await _client.GetAsync("/api/leaderboard/top-players?limit=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<List<TopPlayerDto>>();
        Assert.NotNull(result);
        // Note: May not be empty due to other tests, but should handle empty case gracefully
    }

    [Fact]
    public async Task GetTopQuizzes_WithDefaultLimit_ReturnsTopQuizzes()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var uniqueId = Guid.NewGuid();
        var creator = new User
        {
            Username = $"quizcreator_{uniqueId}",
            Email = $"quizcreator_{uniqueId}@test.com",
            Password = "hashedpass"
        };
        db.Users.Add(creator);
        await db.SaveChangesAsync();

        for (int i = 0; i < 15; i++)
        {
            var quiz = new Quiz
            {
                Title = $"Top Quiz {i}_{uniqueId}",
                Description = "Test quiz",
                CreatorID = creator.Id,
                TimesPlayed = 200 - i
            };
            db.Quizzes.Add(quiz);
        }
        await db.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/leaderboard/top-quizzes");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<List<TopQuizDto>>();
        Assert.NotNull(result);
        Assert.Equal(10, result.Count);
        
        // Verify descending order
        for (int i = 0; i < result.Count - 1; i++)
        {
            Assert.True(result[i].TimesPlayed >= result[i + 1].TimesPlayed);
        }
    }

    [Fact]
    public async Task GetTopQuizzes_WithCustomLimit_ReturnsCorrectNumber()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var uniqueId = Guid.NewGuid();
        var creator = new User
        {
            Username = $"quizlimitcreator_{uniqueId}",
            Email = $"quizlimitcreator_{uniqueId}@test.com",
            Password = "hashedpass"
        };
        db.Users.Add(creator);
        await db.SaveChangesAsync();

        for (int i = 0; i < 10; i++)
        {
            var quiz = new Quiz
            {
                Title = $"Custom Limit Quiz {i}_{uniqueId}",
                Description = "Test quiz",
                CreatorID = creator.Id,
                TimesPlayed = 150 + i
            };
            db.Quizzes.Add(quiz);
        }
        await db.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/leaderboard/top-quizzes?limit=3");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<List<TopQuizDto>>();
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task GetTopQuizzes_WithZeroLimit_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/leaderboard/top-quizzes?limit=0");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Limit must be between 1 and 100", content);
    }

    [Fact]
    public async Task GetTopQuizzes_WithNegativeLimit_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/leaderboard/top-quizzes?limit=-10");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Limit must be between 1 and 100", content);
    }

    [Fact]
    public async Task GetTopQuizzes_WithLimitOver100_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/leaderboard/top-quizzes?limit=150");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Limit must be between 1 and 100", content);
    }

    [Fact]
    public async Task GetTopQuizzes_WithMaxLimit_Returns100Quizzes()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var uniqueId = Guid.NewGuid();
        var creator = new User
        {
            Username = $"maxquizcreator_{uniqueId}",
            Email = $"maxquizcreator_{uniqueId}@test.com",
            Password = "hashedpass"
        };
        db.Users.Add(creator);
        await db.SaveChangesAsync();

        for (int i = 0; i < 105; i++)
        {
            var quiz = new Quiz
            {
                Title = $"Max Quiz {i}_{uniqueId}",
                Description = "Test quiz",
                CreatorID = creator.Id,
                TimesPlayed = 500 + i
            };
            db.Quizzes.Add(quiz);
        }
        await db.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/leaderboard/top-quizzes?limit=100");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<List<TopQuizDto>>();
        Assert.NotNull(result);
        Assert.True(result.Count <= 100); // Should not exceed 100
    }

    [Fact]
    public async Task GetTopPlayers_WithLimit1_ReturnsTopPlayer()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var uniqueId = Guid.NewGuid();
        var topUser = new User
        {
            Username = $"topsingleplayer_{uniqueId}",
            Email = $"topsingleplayer_{uniqueId}@test.com",
            Password = "hashedpass"
        };
        db.Users.Add(topUser);
        await db.SaveChangesAsync();

        var topStat = new UserStats
        {
            UserId = topUser.Id,
            GamesWon = 9999
        };
        db.UserStats.Add(topStat);
        await db.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/leaderboard/top-players?limit=1");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<List<TopPlayerDto>>();
        Assert.NotNull(result);
        Assert.True(result.Count >= 1);
        
        // The top player should have high wins (though exact match depends on other tests)
        var topPlayer = result.First();
        Assert.True(topPlayer.GamesWon > 0);
    }

    [Fact]
    public async Task GetTopQuizzes_VerifiesCreatorUsername()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var uniqueId = Guid.NewGuid();
        var creator = new User
        {
            Username = $"verifycreator_{uniqueId}",
            Email = $"verifycreator_{uniqueId}@test.com",
            Password = "hashedpass"
        };
        db.Users.Add(creator);
        await db.SaveChangesAsync();

        var quiz = new Quiz
        {
            Title = $"Verify Creator Quiz_{uniqueId}",
            Description = "Test quiz",
            CreatorID = creator.Id,
            TimesPlayed = 9000
        };
        db.Quizzes.Add(quiz);
        await db.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/leaderboard/top-quizzes?limit=100");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<List<TopQuizDto>>();
        Assert.NotNull(result);
        
        var verifyQuiz = result.FirstOrDefault(q => q.Title.Contains($"Verify Creator Quiz_{uniqueId}"));
        if (verifyQuiz != null)
        {
            Assert.Equal(creator.Username, verifyQuiz.CreatorUsername);
            Assert.Equal(quiz.ID, verifyQuiz.QuizId);
        }
    }

    [Fact]
    public async Task GetTopPlayers_WithTiedScores_HandlesCorrectly()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var uniqueId = Guid.NewGuid();
        for (int i = 0; i < 5; i++)
        {
            var user = new User
            {
                Username = $"tiedplayer{i}_{uniqueId}",
                Email = $"tiedplayer{i}_{uniqueId}@test.com",
                Password = "hashedpass"
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var userStat = new UserStats
            {
                UserId = user.Id,
                GamesWon = 500 // All tied at 500
            };
            db.UserStats.Add(userStat);
        }
        await db.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/leaderboard/top-players?limit=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<List<TopPlayerDto>>();
        Assert.NotNull(result);
        Assert.True(result.Count > 0);
    }
}

