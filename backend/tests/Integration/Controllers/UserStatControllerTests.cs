using System.Net;
using System.Net.Http.Json;
using backend.DTOs;
using backend.Models;
using Microsoft.Extensions.DependencyInjection;
using backend.Data;
using Xunit;

namespace tests.Integration.Controllers;

public class UserStatControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public UserStatControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetUserStats_WithValidUserId_ReturnsStats()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = new User
        {
            Username = $"statuser_{Guid.NewGuid()}",
            Email = $"statuser_{Guid.NewGuid()}@test.com",
            Password = "hashedpass"
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var stats = new UserStats
        {
            UserId = user.Id,
            GamesPlayed = 10,
            GamesWon = 5,
            QuizzesCreated = 3,
            QuizPlays = 15
        };
        db.UserStats.Add(stats);
        await db.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync($"/api/userstats/users/{user.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<UserStatsDto>();
        Assert.NotNull(result);
        Assert.Equal(user.Id, result.UserId);
        Assert.Equal(10, result.GamesPlayed);
        Assert.Equal(5, result.GamesWon);
        Assert.Equal(3, result.QuizzesCreated);
        Assert.Equal(15, result.QuizPlays);
    }

    [Fact]
    public async Task GetUserStats_WithNonExistentUserId_ReturnsNotFound()
    {
        // Arrange
        int nonExistentUserId = 999999;

        // Act
        var response = await _client.GetAsync($"/api/userstats/users/{nonExistentUserId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("User stats not found", content);
    }

    [Fact]
    public async Task GetUserStats_WithZeroStats_ReturnsZeros()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = new User
        {
            Username = $"zerostatuser_{Guid.NewGuid()}",
            Email = $"zerostatuser_{Guid.NewGuid()}@test.com",
            Password = "hashedpass"
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var stats = new UserStats
        {
            UserId = user.Id,
            GamesPlayed = 0,
            GamesWon = 0,
            QuizzesCreated = 0,
            QuizPlays = 0
        };
        db.UserStats.Add(stats);
        await db.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync($"/api/userstats/users/{user.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<UserStatsDto>();
        Assert.NotNull(result);
        Assert.Equal(0, result.GamesPlayed);
        Assert.Equal(0, result.GamesWon);
        Assert.Equal(0, result.QuizzesCreated);
        Assert.Equal(0, result.QuizPlays);
    }

    [Fact]
    public async Task GetUserStats_WithMaxValues_ReturnsStats()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = new User
        {
            Username = $"maxstatuser_{Guid.NewGuid()}",
            Email = $"maxstatuser_{Guid.NewGuid()}@test.com",
            Password = "hashedpass"
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var stats = new UserStats
        {
            UserId = user.Id,
            GamesPlayed = 10000,
            GamesWon = 9999,
            QuizzesCreated = 500,
            QuizPlays = 50000
        };
        db.UserStats.Add(stats);
        await db.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync($"/api/userstats/users/{user.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<UserStatsDto>();
        Assert.NotNull(result);
        Assert.Equal(10000, result.GamesPlayed);
        Assert.Equal(9999, result.GamesWon);
        Assert.Equal(500, result.QuizzesCreated);
        Assert.Equal(50000, result.QuizPlays);
    }

    [Fact]
    public async Task GetUserStats_WithInvalidUserId_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/userstats/users/invalid");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetUserStats_WithNegativeUserId_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/userstats/users/-1");

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.NotFound || 
                    response.StatusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetUserStats_MultipleUsers_ReturnsCorrectStats()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var uniqueId = Guid.NewGuid();
        var user1 = new User
        {
            Username = $"multiuser1_{uniqueId}",
            Email = $"multiuser1_{uniqueId}@test.com",
            Password = "hashedpass"
        };
        var user2 = new User
        {
            Username = $"multiuser2_{uniqueId}",
            Email = $"multiuser2_{uniqueId}@test.com",
            Password = "hashedpass"
        };
        db.Users.AddRange(user1, user2);
        await db.SaveChangesAsync();

        var stats1 = new UserStats { UserId = user1.Id, GamesPlayed = 5, GamesWon = 2 };
        var stats2 = new UserStats { UserId = user2.Id, GamesPlayed = 10, GamesWon = 8 };
        db.UserStats.AddRange(stats1, stats2);
        await db.SaveChangesAsync();

        // Act
        var response1 = await _client.GetAsync($"/api/userstats/users/{user1.Id}");
        var response2 = await _client.GetAsync($"/api/userstats/users/{user2.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
        
        var result1 = await response1.Content.ReadFromJsonAsync<UserStatsDto>();
        var result2 = await response2.Content.ReadFromJsonAsync<UserStatsDto>();
        
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal(5, result1.GamesPlayed);
        Assert.Equal(2, result1.GamesWon);
        Assert.Equal(10, result2.GamesPlayed);
        Assert.Equal(8, result2.GamesWon);
    }
}

