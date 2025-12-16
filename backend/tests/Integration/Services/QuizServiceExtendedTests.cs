using Xunit;
using backend.Services;
using backend.Models;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using backend.Data;
using backend.Exceptions;

namespace tests.Integration.Services;

public class QuizServiceExtendedTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public QuizServiceExtendedTests(CustomWebApplicationFactory factory) =>
        _factory = factory;

    [Fact]
    public async Task GetAllQuizzes_ReturnsAllQuizzes()
    {
        // Arrange
        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var creator = new User { Id = 2001, Username = "creator2001", Email = "creator2001@test.com", Password = "pass" };
            db.Users.Add(creator);
            await db.SaveChangesAsync();

            var quizzes = new List<Quiz>
            {
                new Quiz { ID = 2001, CreatorID = 2001, Title = "Quiz 1", Description = "Test", TimesPlayed = 0 },
                new Quiz { ID = 2002, CreatorID = 2001, Title = "Quiz 2", Description = "Test", TimesPlayed = 0 }
            };
            db.Quizzes.AddRange(quizzes);
            await db.SaveChangesAsync();
        }

        // Act
        List<Quiz> allQuizzes;
        using (var scope = _factory.Services.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<QuizService>();
            allQuizzes = await service.GetAllQuizzesAsync();
        }

        // Assert
        Assert.NotEmpty(allQuizzes);
        var ourQuizzes = allQuizzes.Where(q => q.ID >= 2001 && q.ID <= 2002).ToList();
        Assert.Equal(2, ourQuizzes.Count);
    }

    [Fact]
    public async Task GetQuizzesByUserId_ReturnsOnlyUserQuizzes()
    {
        // Arrange
        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            var users = new List<User>
            {
                new User { Id = 2101, Username = "user2101", Email = "user2101@test.com", Password = "pass" },
                new User { Id = 2102, Username = "user2102", Email = "user2102@test.com", Password = "pass" }
            };
            db.Users.AddRange(users);
            await db.SaveChangesAsync();

            var quizzes = new List<Quiz>
            {
                new Quiz { ID = 2101, CreatorID = 2101, Title = "User1 Quiz", Description = "Test", TimesPlayed = 0 },
                new Quiz { ID = 2102, CreatorID = 2102, Title = "User2 Quiz", Description = "Test", TimesPlayed = 0 }
            };
            db.Quizzes.AddRange(quizzes);
            await db.SaveChangesAsync();
        }

        // Act
        List<Quiz> userQuizzes;
        using (var scope = _factory.Services.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<QuizService>();
            userQuizzes = await service.GetQuizzesByUserIdAsync("2101");
        }

        // Assert
        var ourQuizzes = userQuizzes.Where(q => q.ID >= 2101 && q.ID <= 2102).ToList();
        Assert.Single(ourQuizzes);
        Assert.Equal("User1 Quiz", ourQuizzes[0].Title);
        Assert.Equal(2101, ourQuizzes[0].CreatorID);
    }

    [Fact]
    public async Task GetQuizById_ReturnsCorrectQuiz()
    {
        // Arrange
        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var creator = new User { Id = 2201, Username = "creator2201", Email = "creator2201@test.com", Password = "pass" };
            db.Users.Add(creator);
            await db.SaveChangesAsync();

            db.Quizzes.Add(new Quiz { ID = 2201, CreatorID = 2201, Title = "Specific Quiz", Description = "Test", TimesPlayed = 0 });
            await db.SaveChangesAsync();
        }

        // Act
        Quiz? quiz;
        using (var scope = _factory.Services.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<QuizService>();
            quiz = await service.GetQuizByIdAsync(2201);
        }

        // Assert
        Assert.NotNull(quiz);
        Assert.Equal("Specific Quiz", quiz.Title);
        Assert.Equal(2201, quiz.ID);
    }

    [Fact]
    public async Task GetQuizById_NonExistent_ReturnsNull()
    {
        // Act
        Quiz? quiz;
        using (var scope = _factory.Services.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<QuizService>();
            quiz = await service.GetQuizByIdAsync(99999);
        }

        // Assert
        Assert.Null(quiz);
    }

    [Fact]
    public async Task DeleteQuiz_RemovesQuiz()
    {
        // Arrange
        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var creator = new User { Id = 2301, Username = "creator2301", Email = "creator2301@test.com", Password = "pass" };
            db.Users.Add(creator);
            await db.SaveChangesAsync();

            db.Quizzes.Add(new Quiz { ID = 2301, CreatorID = 2301, Title = "To Delete", Description = "Test", TimesPlayed = 0 });
            await db.SaveChangesAsync();
        }

        // Act
        bool result;
        using (var scope = _factory.Services.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<QuizService>();
            result = await service.DeleteQuizAsync(2301);
        }

        // Assert
        Assert.True(result);
        using (var checkScope = _factory.Services.CreateScope())
        {
            var db = checkScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var quiz = await db.Quizzes.FindAsync(2301);
            Assert.Null(quiz);
        }
    }

    [Fact]
    public async Task DeleteQuiz_NonExistent_ReturnsFalse()
    {
        // Act
        bool result;
        using (var scope = _factory.Services.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<QuizService>();
            result = await service.DeleteQuizAsync(99999);
        }

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IncrementQuizPlays_IncrementsCounter()
    {
        // Arrange
        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var creator = new User { Id = 2401, Username = "creator2401", Email = "creator2401@test.com", Password = "pass" };
            db.Users.Add(creator);
            await db.SaveChangesAsync();

            db.Quizzes.Add(new Quiz { ID = 2401, CreatorID = 2401, Title = "Popular Quiz", Description = "Test", TimesPlayed = 5 });
            await db.SaveChangesAsync();
        }

        // Act
        using (var scope = _factory.Services.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<QuizService>();
            await service.IncrementQuizPlaysAsync(2401);
        }

        // Assert
        using (var checkScope = _factory.Services.CreateScope())
        {
            var db = checkScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var quiz = await db.Quizzes.FindAsync(2401);
            Assert.NotNull(quiz);
            Assert.Equal(6, quiz.TimesPlayed);
        }
    }

    [Fact]
    public async Task IncrementQuizPlays_NonExistent_ReturnsFalse()
    {
        // Act
        bool result;
        using (var scope = _factory.Services.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<QuizService>();
            result = await service.IncrementQuizPlaysAsync(99999);
        }

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task UpdateQuiz_UpdatesTitle()
    {
        // Arrange
        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var creator = new User { Id = 2501, Username = "creator2501", Email = "creator2501@test.com", Password = "pass" };
            db.Users.Add(creator);
            await db.SaveChangesAsync();

            db.Quizzes.Add(new Quiz { ID = 2501, CreatorID = 2501, Title = "Old Title", Description = "Old Desc", TimesPlayed = 0 });
            await db.SaveChangesAsync();
        }

        // Act
        using (var scope = _factory.Services.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<QuizService>();
            var updatedQuiz = new Quiz
            {
                ID = 2501,
                CreatorID = 2501,
                Title = "New Title",
                Description = "New Desc",
                TimesPlayed = 0,
                Questions = new List<QuizQuestion>()
            };
            await service.UpdateQuizAsync(2501, updatedQuiz);
        }

        // Assert
        using (var checkScope = _factory.Services.CreateScope())
        {
            var db = checkScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var quiz = await db.Quizzes.FindAsync(2501);
            Assert.NotNull(quiz);
            Assert.Equal("New Title", quiz.Title);
            Assert.Equal("New Desc", quiz.Description);
        }
    }

    [Fact]
    public async Task UpdateQuiz_NonExistent_ReturnsNull()
    {
        // Act
        Quiz? result;
        using (var scope = _factory.Services.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<QuizService>();
            var quiz = new Quiz
            {
                ID = 99999,
                CreatorID = 1,
                Title = "Test",
                Description = "Test",
                TimesPlayed = 0,
                Questions = new List<QuizQuestion>()
            };
            result = await service.UpdateQuizAsync(99999, quiz);
        }

        // Assert
        Assert.Null(result);
    }


    [Fact]
    public async Task GetTopQuizzes_RespectsLimit()
    {
        // Arrange
        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var creator = new User { Id = 2701, Username = "creator2701", Email = "creator2701@test.com", Password = "pass" };
            db.Users.Add(creator);
            await db.SaveChangesAsync();

            for (int i = 2701; i < 2710; i++)
            {
                db.Quizzes.Add(new Quiz { ID = i, CreatorID = 2701, Title = $"Quiz {i}", Description = "Test", TimesPlayed = i });
            }
            await db.SaveChangesAsync();
        }

        // Act
        List<(int QuizId, string Title, string CreatorUsername, int TimesPlayed)> topQuizzes;
        using (var scope = _factory.Services.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<QuizService>();
            topQuizzes = await service.GetTopQuizzesAsync(3);
        }

        // Assert
        Assert.True(topQuizzes.Count <= 3);
    }

    [Fact]
    public async Task ValidateQuiz_TitleTooLong_ThrowsException()
    {
        // Arrange
        var quiz = new Quiz
        {
            ID = 1,
            CreatorID = 1,
            Title = new string('A', 101), // 101 characters
            Description = "Valid description",
            TimesPlayed = 0,
            Questions = new List<QuizQuestion>()
        };

        // Act & Assert
        using (var scope = _factory.Services.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<QuizService>();
            var exception = await Assert.ThrowsAsync<QuizValidationException>(
                async () => await service.CreateQuizAsync(quiz)
            );
            Assert.Contains("cannot exceed 100 characters", exception.Message);
        }
    }

    [Fact]
    public async Task ValidateQuiz_TooManyQuestions_ThrowsException()
    {
        // Arrange
        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var creator = new User { Id = 2801, Username = "creator2801", Email = "creator2801@test.com", Password = "pass" };
            db.Users.Add(creator);
            await db.SaveChangesAsync();
        }

        var questions = new List<QuizQuestion>();
        for (int i = 0; i < 51; i++)
        {
            questions.Add(new QuizQuestion
            {
                QuestionText = $"Question {i}",
                Options = new List<string> { "A", "B" },
                CorrectOptionIndex = 0,
                TimeLimit = 10
            });
        }

        var quiz = new Quiz
        {
            CreatorID = 2801,
            Title = "Too Many Questions",
            Description = "Test",
            TimesPlayed = 0,
            Questions = questions
        };

        // Act & Assert
        using (var scope = _factory.Services.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<QuizService>();
            var exception = await Assert.ThrowsAsync<QuizValidationException>(
                async () => await service.CreateQuizAsync(quiz)
            );
            Assert.Contains("cannot have more than 50 questions", exception.Message);
        }
    }

    [Fact]
    public async Task ValidateQuiz_QuestionWithTooFewOptions_ThrowsException()
    {
        // Arrange
        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var creator = new User { Id = 2901, Username = "creator2901", Email = "creator2901@test.com", Password = "pass" };
            db.Users.Add(creator);
            await db.SaveChangesAsync();
        }

        var quiz = new Quiz
        {
            CreatorID = 2901,
            Title = "Bad Quiz",
            Description = "Test",
            TimesPlayed = 0,
            Questions = new List<QuizQuestion>
            {
                new QuizQuestion
                {
                    QuestionText = "Bad Question",
                    Options = new List<string> { "Only One" }, // Too few options
                    CorrectOptionIndex = 0,
                    TimeLimit = 10
                }
            }
        };

        // Act & Assert
        using (var scope = _factory.Services.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<QuizService>();
            var exception = await Assert.ThrowsAsync<QuizValidationException>(
                async () => await service.CreateQuizAsync(quiz)
            );
            Assert.Contains("Must have at least 2 options", exception.Message);
        }
    }

    [Fact]
    public async Task ValidateQuiz_InvalidCorrectOptionIndex_ThrowsException()
    {
        // Arrange
        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var creator = new User { Id = 3001, Username = "creator3001", Email = "creator3001@test.com", Password = "pass" };
            db.Users.Add(creator);
            await db.SaveChangesAsync();
        }

        var quiz = new Quiz
        {
            CreatorID = 3001,
            Title = "Bad Quiz",
            Description = "Test",
            TimesPlayed = 0,
            Questions = new List<QuizQuestion>
            {
                new QuizQuestion
                {
                    QuestionText = "Question",
                    Options = new List<string> { "A", "B", "C" },
                    CorrectOptionIndex = 5, // Out of range
                    TimeLimit = 10
                }
            }
        };

        // Act & Assert
        using (var scope = _factory.Services.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<QuizService>();
            var exception = await Assert.ThrowsAsync<QuizValidationException>(
                async () => await service.CreateQuizAsync(quiz)
            );
            Assert.Contains("out of range", exception.Message);
        }
    }
}

