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
using Microsoft.VisualBasic;
using backend.Exceptions;

namespace tests.Integration.Services;

public class QuizServiceTests :IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    public QuizServiceTests(CustomWebApplicationFactory factory) =>
        _factory = factory;

    [Fact]
    public async Task CreateQuizTest()
    {
        int userId = 1;
        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = new User { Id = userId, Username = "quizcreator", Email = "qc@test.com", Password = "password123" };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            db.UserStats.Add(new UserStats { UserId  = userId, GamesPlayed = 0, GamesWon = 0, QuizzesCreated = 0, QuizPlays = 0 });
            await db.SaveChangesAsync();
        }

        string token;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var seededUser = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            var jwt = scope.ServiceProvider.GetRequiredService<JwtService>();
            token = jwt.GenerateToken(seededUser!);
        }

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var payload = new {Title = "Test quiz", Description = "Created for testing"};

        var response = await client.PostAsJsonAsync("/api/quizzes/my", payload);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.True(response.Headers.Location != null, "Expected Location header pointing to the new resource");

        using (var checkScope = _factory.Services.CreateScope())
        {
            var db = checkScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var createdQuiz = await db.Quizzes.FirstOrDefaultAsync(q => q.Title == "Test quiz" && q.CreatorID == userId);
            Assert.NotNull(createdQuiz);
            var stats = await db.UserStats.FirstOrDefaultAsync(s => s.UserId == userId);
            Assert.Equal(1, stats!.QuizzesCreated);
        }
    }

    [Fact]
    public async Task ValidationTest()
    {
        using (var seedScope = _factory.Services.CreateScope())
        {
            using (var scope = _factory.Services.CreateScope())
            {
                var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
                var quizService = scope.ServiceProvider.GetRequiredService<QuizService>();
                var badQuiz = new Quiz {ID = 2, CreatorID = 2, Title = "", Description = "Desc", TimesPlayed = 0, Questions = new List<QuizQuestion>()};
                var validationCheck = await Assert.ThrowsAsync<QuizValidationException>(
                    async () => await quizService.CreateQuizAsync(badQuiz)
                );

                Assert.Equal("Quiz title cannot be empty or whitespace.", validationCheck.Message);
            }
        }
    }
}
