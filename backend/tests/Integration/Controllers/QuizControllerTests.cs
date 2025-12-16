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

public class QuizControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public QuizControllerTests(CustomWebApplicationFactory factory) =>
        _factory = factory;

    [Fact]
    public async Task GetAllQuizzes_ReturnsOk()
    {
        // Arrange
        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = new User { Id = 7001, Username = "quizowner7001", Email = "quizowner7001@test.com", Password = "pass" };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            db.Quizzes.Add(new Quiz { ID = 7001, CreatorID = 7001, Title = "Public Quiz 1", Description = "Test", TimesPlayed = 0 });
            await db.SaveChangesAsync();
        }

        // Act
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/quizzes");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetQuizById_ReturnsQuiz()
    {
        // Arrange
        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = new User { Id = 7101, Username = "quizowner7101", Email = "quizowner7101@test.com", Password = "pass" };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            db.Quizzes.Add(new Quiz { ID = 7101, CreatorID = 7101, Title = "Specific Quiz", Description = "Test", TimesPlayed = 0 });
            await db.SaveChangesAsync();
        }

        // Act
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/quizzes/7101");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetQuizById_NonExistent_ReturnsNotFound()
    {
        // Act
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/quizzes/99999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteQuiz_WithAuth_DeletesQuiz()
    {
        // Arrange
        int userId = 7201;
        string token;

        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = new User { Id = userId, Username = "deleter7201", Email = "deleter7201@test.com", Password = "pass" };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            db.UserStats.Add(new UserStats { UserId = userId, GamesPlayed = 0, GamesWon = 0, QuizzesCreated = 1, QuizPlays = 0 });
            db.Quizzes.Add(new Quiz { ID = 7201, CreatorID = userId, Title = "To Delete", Description = "Test", TimesPlayed = 0 });
            await db.SaveChangesAsync();

            var jwtService = seedScope.ServiceProvider.GetRequiredService<JwtService>();
            token = jwtService.GenerateToken(user);
        }

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.DeleteAsync("/api/quizzes/7201");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateQuiz_WithAuth_UpdatesQuiz()
    {
        // Arrange
        int userId = 7301;
        string token;

        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = new User { Id = userId, Username = "updater7301", Email = "updater7301@test.com", Password = "pass" };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            db.Quizzes.Add(new Quiz { ID = 7301, CreatorID = userId, Title = "Old Title", Description = "Old Desc", TimesPlayed = 0 });
            await db.SaveChangesAsync();

            var jwtService = seedScope.ServiceProvider.GetRequiredService<JwtService>();
            token = jwtService.GenerateToken(user);
        }

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var updateData = new
        {
            title = "New Title",
            description = "New Desc",
            questions = new[]
            {
                new
                {
                    id = 0,
                    questionText = "Test Question",
                    options = new[] { "A", "B", "C" },
                    correctOptionIndex = 0,
                    timeLimit = 15
                }
            }
        };

        // Act
        var response = await client.PutAsJsonAsync("/api/quizzes/7301", updateData);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateQuiz_NotOwner_ReturnsForbidden()
    {
        // Arrange
        int ownerId = 7401;
        int otherUserId = 7402;
        string token;

        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            db.Users.Add(new User { Id = ownerId, Username = "owner7401", Email = "owner7401@test.com", Password = "pass" });
            db.Users.Add(new User { Id = otherUserId, Username = "other7402", Email = "other7402@test.com", Password = "pass" });
            await db.SaveChangesAsync();

            db.Quizzes.Add(new Quiz { ID = 7401, CreatorID = ownerId, Title = "Owner Quiz", Description = "Test", TimesPlayed = 0 });
            await db.SaveChangesAsync();

            var jwtService = seedScope.ServiceProvider.GetRequiredService<JwtService>();
            var otherUser = await db.Users.FindAsync(otherUserId);
            token = jwtService.GenerateToken(otherUser!);
        }

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var updateData = new
        {
            title = "Hacked Title",
            description = "Hacked",
            questions = new object[] { }
        };

        // Act
        var response = await client.PutAsJsonAsync("/api/quizzes/7401", updateData);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetMyQuizzes_WithAuth_ReturnsUserQuizzes()
    {
        // Arrange
        int userId = 7501;
        string token;

        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = new User { Id = userId, Username = "myquizuser7501", Email = "myquizuser7501@test.com", Password = "pass" };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            db.Quizzes.Add(new Quiz { ID = 7501, CreatorID = userId, Title = "My Quiz", Description = "Test", TimesPlayed = 0 });
            await db.SaveChangesAsync();

            var jwtService = seedScope.ServiceProvider.GetRequiredService<JwtService>();
            token = jwtService.GenerateToken(user);
        }

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/quizzes/my");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetMyQuizzes_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/quizzes/my");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

