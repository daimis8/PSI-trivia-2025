using Xunit;
using backend.Services;
using backend.Models;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using backend.Data;

namespace tests.Integration.Services;

public class UserStatsServiceExtendedTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public UserStatsServiceExtendedTests(CustomWebApplicationFactory factory) =>
        _factory = factory;

    [Fact]
    public async Task IncrementGameStats_WithWin_IncrementsPlayedAndWon()
    {
        // Arrange
        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Users.Add(new User { Id = 1001, Username = "winner", Email = "winner@test.com", Password = "pass" });
            await db.SaveChangesAsync();
            db.UserStats.Add(new UserStats { UserId = 1001, GamesPlayed = 5, GamesWon = 2, QuizzesCreated = 0, QuizPlays = 0 });
            await db.SaveChangesAsync();
        }

        // Act
        using (var scope = _factory.Services.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<UserStatsService>();
            await service.IncrementGameStatsAsync(1001, won: true);
        }

        // Assert
        using (var checkScope = _factory.Services.CreateScope())
        {
            var db = checkScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var stats = await db.UserStats.FindAsync(1001);
            Assert.NotNull(stats);
            Assert.Equal(6, stats.GamesPlayed);
            Assert.Equal(3, stats.GamesWon);
        }
    }

    [Fact]
    public async Task IncrementGameStats_WithoutWin_IncrementsOnlyPlayed()
    {
        // Arrange
        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Users.Add(new User { Id = 1002, Username = "loser", Email = "loser@test.com", Password = "pass" });
            await db.SaveChangesAsync();
            db.UserStats.Add(new UserStats { UserId = 1002, GamesPlayed = 5, GamesWon = 2, QuizzesCreated = 0, QuizPlays = 0 });
            await db.SaveChangesAsync();
        }

        // Act
        using (var scope = _factory.Services.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<UserStatsService>();
            await service.IncrementGameStatsAsync(1002, won: false);
        }

        // Assert
        using (var checkScope = _factory.Services.CreateScope())
        {
            var db = checkScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var stats = await db.UserStats.FindAsync(1002);
            Assert.NotNull(stats);
            Assert.Equal(6, stats.GamesPlayed);
            Assert.Equal(2, stats.GamesWon); // Unchanged
        }
    }

    [Fact]
    public async Task ChangeQuizNo_IncreasesCount()
    {
        // Arrange
        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Users.Add(new User { Id = 1003, Username = "creator", Email = "creator@test.com", Password = "pass" });
            await db.SaveChangesAsync();
            db.UserStats.Add(new UserStats { UserId = 1003, GamesPlayed = 0, GamesWon = 0, QuizzesCreated = 5, QuizPlays = 0 });
            await db.SaveChangesAsync();
        }

        // Act
        using (var scope = _factory.Services.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<UserStatsService>();
            await service.ChangeQuizNoAsync(1003, 3);
        }

        // Assert
        using (var checkScope = _factory.Services.CreateScope())
        {
            var db = checkScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var stats = await db.UserStats.FindAsync(1003);
            Assert.NotNull(stats);
            Assert.Equal(8, stats.QuizzesCreated);
        }
    }

    [Fact]
    public async Task ChangeQuizNo_DecreasesCount()
    {
        // Arrange
        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Users.Add(new User { Id = 1004, Username = "deleter", Email = "deleter@test.com", Password = "pass" });
            await db.SaveChangesAsync();
            db.UserStats.Add(new UserStats { UserId = 1004, GamesPlayed = 0, GamesWon = 0, QuizzesCreated = 5, QuizPlays = 0 });
            await db.SaveChangesAsync();
        }

        // Act
        using (var scope = _factory.Services.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<UserStatsService>();
            await service.ChangeQuizNoAsync(1004, -2);
        }

        // Assert
        using (var checkScope = _factory.Services.CreateScope())
        {
            var db = checkScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var stats = await db.UserStats.FindAsync(1004);
            Assert.NotNull(stats);
            Assert.Equal(3, stats.QuizzesCreated);
        }
    }

    [Fact]
    public async Task IncrementQuizPlays_IncrementsCount()
    {
        // Arrange
        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Users.Add(new User { Id = 1005, Username = "player", Email = "player@test.com", Password = "pass" });
            await db.SaveChangesAsync();
            db.UserStats.Add(new UserStats { UserId = 1005, GamesPlayed = 0, GamesWon = 0, QuizzesCreated = 0, QuizPlays = 10 });
            await db.SaveChangesAsync();
        }

        // Act
        using (var scope = _factory.Services.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<UserStatsService>();
            await service.IncrementQuizPlaysAsync(1005);
        }

        // Assert
        using (var checkScope = _factory.Services.CreateScope())
        {
            var db = checkScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var stats = await db.UserStats.FindAsync(1005);
            Assert.NotNull(stats);
            Assert.Equal(11, stats.QuizPlays);
        }
    }

    [Fact]
    public async Task RecordGameStats_UpdatesMultipleUsers()
    {
        // Arrange
        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var users = new List<User>
            {
                new User { Id = 1006, Username = "p1", Email = "p1@test.com", Password = "pass" },
                new User { Id = 1007, Username = "p2", Email = "p2@test.com", Password = "pass" },
                new User { Id = 1008, Username = "p3", Email = "p3@test.com", Password = "pass" }
            };
            db.Users.AddRange(users);
            await db.SaveChangesAsync();

            var stats = new List<UserStats>
            {
                new UserStats { UserId = 1006, GamesPlayed = 0, GamesWon = 0, QuizzesCreated = 0, QuizPlays = 0 },
                new UserStats { UserId = 1007, GamesPlayed = 0, GamesWon = 0, QuizzesCreated = 0, QuizPlays = 0 },
                new UserStats { UserId = 1008, GamesPlayed = 0, GamesWon = 0, QuizzesCreated = 0, QuizPlays = 0 }
            };
            db.UserStats.AddRange(stats);
            await db.SaveChangesAsync();
        }

        // Act
        using (var scope = _factory.Services.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<UserStatsService>();
            var playerIds = new[] { 1006, 1007, 1008 };
            var winnerIds = new[] { 1006 }; // Only player 1006 wins
            await service.RecordGameStatsAsync(playerIds, winnerIds);
        }

        // Assert
        using (var checkScope = _factory.Services.CreateScope())
        {
            var db = checkScope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            var stats1006 = await db.UserStats.FindAsync(1006);
            Assert.Equal(1, stats1006!.GamesPlayed);
            Assert.Equal(1, stats1006.GamesWon);

            var stats1007 = await db.UserStats.FindAsync(1007);
            Assert.Equal(1, stats1007!.GamesPlayed);
            Assert.Equal(0, stats1007.GamesWon);

            var stats1008 = await db.UserStats.FindAsync(1008);
            Assert.Equal(1, stats1008!.GamesPlayed);
            Assert.Equal(0, stats1008.GamesWon);
        }
    }

    [Fact]
    public async Task DeleteUserStats_RemovesStats()
    {
        // Arrange
        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Users.Add(new User { Id = 1009, Username = "toDelete", Email = "delete@test.com", Password = "pass" });
            await db.SaveChangesAsync();
            db.UserStats.Add(new UserStats { UserId = 1009, GamesPlayed = 10, GamesWon = 5, QuizzesCreated = 2, QuizPlays = 20 });
            await db.SaveChangesAsync();
        }

        // Act
        bool result;
        using (var scope = _factory.Services.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<UserStatsService>();
            result = await service.DeleteUserStatsAsync(1009);
        }

        // Assert
        Assert.True(result);
        using (var checkScope = _factory.Services.CreateScope())
        {
            var db = checkScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var stats = await db.UserStats.FindAsync(1009);
            Assert.Null(stats);
        }
    }

    [Fact]
    public async Task DeleteUserStats_NonExistent_ReturnsFalse()
    {
        // Act
        bool result;
        using (var scope = _factory.Services.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<UserStatsService>();
            result = await service.DeleteUserStatsAsync(99999);
        }

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetTopPlayers_ReturnsCorrectOrder()
    {
        // Arrange
        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            var users = new List<User>
            {
                new User { Id = 1010, Username = "Top", Email = "top@test.com", Password = "pass" },
                new User { Id = 1011, Username = "Mid", Email = "mid@test.com", Password = "pass" },
                new User { Id = 1012, Username = "Low", Email = "low@test.com", Password = "pass" }
            };
            db.Users.AddRange(users);
            await db.SaveChangesAsync();

            var stats = new List<UserStats>
            {
                new UserStats { UserId = 1010, GamesPlayed = 100, GamesWon = 50, QuizzesCreated = 0, QuizPlays = 0 },
                new UserStats { UserId = 1011, GamesPlayed = 50, GamesWon = 25, QuizzesCreated = 0, QuizPlays = 0 },
                new UserStats { UserId = 1012, GamesPlayed = 25, GamesWon = 10, QuizzesCreated = 0, QuizPlays = 0 }
            };
            db.UserStats.AddRange(stats);
            await db.SaveChangesAsync();
        }

        // Act
        List<(int UserId, string Username, int GamesWon)> topPlayers;
        using (var scope = _factory.Services.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<UserStatsService>();
            topPlayers = await service.GetTopPlayersAsync(10);
        }

        // Assert
        Assert.NotEmpty(topPlayers);
        var ourPlayers = topPlayers.Where(p => p.UserId >= 1010 && p.UserId <= 1012).ToList();
        Assert.Equal(3, ourPlayers.Count);
        
        // Verify order
        for (int i = 0; i < ourPlayers.Count - 1; i++)
        {
            Assert.True(ourPlayers[i].GamesWon >= ourPlayers[i + 1].GamesWon,
                $"Players should be ordered by wins: {ourPlayers[i].GamesWon} >= {ourPlayers[i + 1].GamesWon}");
        }
    }

    [Fact]
    public async Task GetTopPlayers_RespectsLimit()
    {
        // Arrange
        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            for (int i = 1100; i < 1110; i++)
            {
                db.Users.Add(new User { Id = i, Username = $"User{i}", Email = $"user{i}@test.com", Password = "pass" });
            }
            await db.SaveChangesAsync();

            for (int i = 1100; i < 1110; i++)
            {
                db.UserStats.Add(new UserStats { UserId = i, GamesPlayed = i, GamesWon = i - 1100, QuizzesCreated = 0, QuizPlays = 0 });
            }
            await db.SaveChangesAsync();
        }

        // Act
        List<(int UserId, string Username, int GamesWon)> topPlayers;
        using (var scope = _factory.Services.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<UserStatsService>();
            topPlayers = await service.GetTopPlayersAsync(3);
        }

        // Assert
        Assert.True(topPlayers.Count <= 3);
    }
}

