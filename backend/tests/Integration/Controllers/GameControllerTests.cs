using Xunit;
using backend.Models;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using backend.Data;
using backend.Services;
using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;

namespace tests.Integration.Controllers;

public class GameControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public GameControllerTests(CustomWebApplicationFactory factory) =>
        _factory = factory;

    [Fact]
    public async Task CreateGame_WithValidQuiz_ReturnsCode()
    {
        // Arrange
        int userId = 8001;
        string token;

        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = new User { Id = userId, Username = "gamehost8001", Email = "gamehost8001@test.com", Password = "pass" };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var quiz = new Quiz 
            { 
                ID = 8001, 
                CreatorID = userId, 
                Title = "Game Quiz", 
                Description = "Test", 
                TimesPlayed = 0,
                Questions = new List<QuizQuestion>
                {
                    new QuizQuestion
                    {
                        QuestionText = "Test Question",
                        Options = new List<string> { "A", "B", "C" },
                        CorrectOptionIndex = 0,
                        TimeLimit = 15
                    }
                }
            };
            db.Quizzes.Add(quiz);
            await db.SaveChangesAsync();

            var jwtService = seedScope.ServiceProvider.GetRequiredService<JwtService>();
            token = jwtService.GenerateToken(user);
        }

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new { quizId = 8001 };

        // Act
        var response = await client.PostAsJsonAsync("/api/games", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.NotNull(content);
        Assert.True(content.ContainsKey("code"));
    }

    [Fact]
    public async Task CreateGame_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var client = _factory.CreateClient();
        var request = new { quizId = 1 };
        var response = await client.PostAsJsonAsync("/api/games", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateGame_WithNonExistentQuiz_ReturnsNotFound()
    {
        // Arrange
        int userId = 8101;
        string token;

        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = new User { Id = userId, Username = "gamehost8101", Email = "gamehost8101@test.com", Password = "pass" };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var jwtService = seedScope.ServiceProvider.GetRequiredService<JwtService>();
            token = jwtService.GenerateToken(user);
        }

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new { quizId = 99999 };

        // Act
        var response = await client.PostAsJsonAsync("/api/games", request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateGame_NotQuizOwner_ReturnsForbidden()
    {
        // Arrange
        int ownerId = 8201;
        int otherUserId = 8202;
        string token;

        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            db.Users.Add(new User { Id = ownerId, Username = "quizowner8201", Email = "quizowner8201@test.com", Password = "pass" });
            db.Users.Add(new User { Id = otherUserId, Username = "other8202", Email = "other8202@test.com", Password = "pass" });
            await db.SaveChangesAsync();

            var quiz = new Quiz 
            { 
                ID = 8201, 
                CreatorID = ownerId, 
                Title = "Owner Quiz", 
                Description = "Test", 
                TimesPlayed = 0,
                Questions = new List<QuizQuestion>
                {
                    new QuizQuestion
                    {
                        QuestionText = "Test",
                        Options = new List<string> { "A", "B" },
                        CorrectOptionIndex = 0,
                        TimeLimit = 10
                    }
                }
            };
            db.Quizzes.Add(quiz);
            await db.SaveChangesAsync();

            var jwtService = seedScope.ServiceProvider.GetRequiredService<JwtService>();
            var otherUser = await db.Users.FindAsync(otherUserId);
            token = jwtService.GenerateToken(otherUser!);
        }

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new { quizId = 8201 };

        // Act
        var response = await client.PostAsJsonAsync("/api/games", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateGame_WithEmptyQuiz_ReturnsBadRequest()
    {
        // Arrange
        int userId = 8301;
        string token;

        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = new User { Id = userId, Username = "gamehost8301", Email = "gamehost8301@test.com", Password = "pass" };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var quiz = new Quiz 
            { 
                ID = 8301, 
                CreatorID = userId, 
                Title = "Empty Quiz", 
                Description = "No questions", 
                TimesPlayed = 0,
                Questions = new List<QuizQuestion>() // Empty questions
            };
            db.Quizzes.Add(quiz);
            await db.SaveChangesAsync();

            var jwtService = seedScope.ServiceProvider.GetRequiredService<JwtService>();
            token = jwtService.GenerateToken(user);
        }

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new { quizId = 8301 };

        // Act
        var response = await client.PostAsJsonAsync("/api/games", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GameExists_NonExistent_ReturnsNotFound()
    {
        // Act
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/games/NONEXISTENT/exists");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GameExists_AfterCreation_ReturnsTrue()
    {
        // Arrange
        int userId = 8401;
        string token;
        string gameCode;

        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = new User { Id = userId, Username = "gamehost8401", Email = "gamehost8401@test.com", Password = "pass" };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var quiz = new Quiz 
            { 
                ID = 8401, 
                CreatorID = userId, 
                Title = "Exists Test Quiz", 
                Description = "Test", 
                TimesPlayed = 0,
                Questions = new List<QuizQuestion>
                {
                    new QuizQuestion
                    {
                        QuestionText = "Question",
                        Options = new List<string> { "A", "B" },
                        CorrectOptionIndex = 0,
                        TimeLimit = 10
                    }
                }
            };
            db.Quizzes.Add(quiz);
            await db.SaveChangesAsync();

            var jwtService = seedScope.ServiceProvider.GetRequiredService<JwtService>();
            token = jwtService.GenerateToken(user);
        }

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create game
        var createResponse = await client.PostAsJsonAsync("/api/games", new { quizId = 8401 });
        var createContent = await createResponse.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        gameCode = createContent!["code"];

        // Act
        var existsResponse = await client.GetAsync($"/api/games/{gameCode}/exists");

        // Assert
        Assert.Equal(HttpStatusCode.OK, existsResponse.StatusCode);
        var existsContent = await existsResponse.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.NotNull(existsContent);
        Assert.True(existsContent.ContainsKey("exists"));
    }
}

