using Xunit;
using backend.Services;
using backend.Models;
using System.Threading.Tasks;
using System;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using backend.Data;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace tests.Integration.Services;

public class UserStatServiceTests :IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    public UserStatServiceTests(CustomWebApplicationFactory factory) =>
        _factory = factory;

    [Fact]
    public async Task GetExistingUserStatsTest()
    {
        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Users.Add(new User { Id = 1, Username = "proplayer", Email = "stat@test.com", Password = "password123" });
            await db.SaveChangesAsync();
            db.UserStats.Add(new UserStats { UserId = 1, GamesPlayed = 5, GamesWon = 0, QuizzesCreated = 0, QuizPlays = 0 });
            await db.SaveChangesAsync();
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var testService = scope.ServiceProvider.GetRequiredService<UserStatsService>();
            var resultStats = await testService.GetUserStatsAsync(1);

            Assert.Equal(0, resultStats!.GamesWon);
            Assert.Equal(5, resultStats!.GamesPlayed);
        }
    }

    [Fact]
    public async Task EnsureStatsExistTest()
    {
        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Users.Add(new User { Id = 2, Username = "nonexistant", Email = "existat@test.com", Password = "password123" });
            await db.SaveChangesAsync();
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var testService = scope.ServiceProvider.GetRequiredService<UserStatsService>();
            var resultStats = await testService.EnsureAsync(2);

            Assert.Equal(0, resultStats!.GamesPlayed);
            Assert.Equal(0, resultStats!.QuizzesCreated);
        }
    }
}