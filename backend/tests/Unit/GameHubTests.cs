using Xunit;
using backend.Hubs;
using backend.Services;
using backend.Models;
using backend.DTOs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace tests.Unit;

public class GameHubTests
{
    private GameService CreateGameService()
    {
        return new GameService();
    }

    private QuizService CreateQuizService()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        
        var options = new DbContextOptionsBuilder<backend.Data.AppDbContext>()
            .UseSqlite(connection)
            .Options;
        
        var context = new backend.Data.AppDbContext(options);
        context.Database.EnsureCreated();
        return new QuizService(context);
    }

    private UserStatsService CreateUserStatsService()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        
        var options = new DbContextOptionsBuilder<backend.Data.AppDbContext>()
            .UseSqlite(connection)
            .Options;
        
        var context = new backend.Data.AppDbContext(options);
        context.Database.EnsureCreated();
        return new UserStatsService(context);
    }

    private Mock<IHubContext<GameHub>> CreateMockHubContext()
    {
        var mockHubContext = new Mock<IHubContext<GameHub>>();
        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        var mockGroupManager = new Mock<IGroupManager>();
        
        mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);
        mockHubContext.Setup(h => h.Groups).Returns(mockGroupManager.Object);
        mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(mockClientProxy.Object);
        
        return mockHubContext;
    }

    private GameHub CreateHub(
        GameService? gameService = null,
        QuizService? quizService = null,
        Mock<IHubContext<GameHub>>? hubContext = null,
        UserStatsService? userStatsService = null,
        ClaimsPrincipal? user = null,
        string? connectionId = null)
    {
        var hub = new GameHub(
            gameService ?? CreateGameService(),
            quizService ?? CreateQuizService(),
            hubContext?.Object ?? CreateMockHubContext().Object,
            userStatsService ?? CreateUserStatsService()
        );

        // Mock the Hub Context
        var mockContext = new Mock<HubCallerContext>();
        mockContext.Setup(c => c.ConnectionId).Returns(connectionId ?? "test-connection-id");
        mockContext.Setup(c => c.User).Returns(user ?? new ClaimsPrincipal());

        var mockClients = new Mock<IHubCallerClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        var mockSingleClientProxy = new Mock<ISingleClientProxy>();
        var mockGroupManager = new Mock<IGroupManager>();

        mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(mockClientProxy.Object);
        mockClients.Setup(c => c.Caller).Returns(mockSingleClientProxy.Object);

        // Set the Context property using reflection since it's protected
        var contextProperty = typeof(Hub).GetProperty("Context");
        contextProperty?.SetValue(hub, mockContext.Object);

        var clientsProperty = typeof(Hub).GetProperty("Clients");
        clientsProperty?.SetValue(hub, mockClients.Object);

        var groupsProperty = typeof(Hub).GetProperty("Groups");
        groupsProperty?.SetValue(hub, mockGroupManager.Object);

        return hub;
    }

    private ClaimsPrincipal CreateUserPrincipal(int userId, string username)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, username)
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
    }

    [Fact]
    public async Task JoinAsHost_ValidGame_AddsHostToGame()
    {
        // Arrange
        var gameService = CreateGameService();
        var game = gameService.CreateGame(1, 1, new List<QuizQuestion>());
        var code = game.Code;

        var user = CreateUserPrincipal(1, "HostUser");
        var hub = CreateHub(gameService: gameService, user: user, connectionId: "host-conn");

        // Act
        await hub.JoinAsHost(code);

        // Assert
        gameService.TryGetGame(code, out var updatedGame);
        Assert.Equal("host-conn", updatedGame!.HostConnectionId);
    }

    [Fact]
    public async Task JoinAsHost_NonExistentGame_ThrowsException()
    {
        // Arrange
        var gameService = CreateGameService();
        var user = CreateUserPrincipal(1, "HostUser");
        var hub = CreateHub(gameService: gameService, user: user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HubException>(
            async () => await hub.JoinAsHost("NONEXIST")
        );
        Assert.Contains("not found", exception.Message.ToLower());
    }

    [Fact]
    public async Task JoinAsHost_NotGameHost_ThrowsForbidden()
    {
        // Arrange
        var gameService = CreateGameService();
        var game = gameService.CreateGame(1, 1, new List<QuizQuestion>());
        var code = game.Code;

        var user = CreateUserPrincipal(2, "WrongUser"); // Different user ID
        var hub = CreateHub(gameService: gameService, user: user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HubException>(
            async () => await hub.JoinAsHost(code)
        );
        Assert.Contains("forbidden", exception.Message.ToLower());
    }

    [Fact]
    public async Task JoinAsPlayer_ValidGame_AddsPlayerToGame()
    {
        // Arrange
        var gameService = CreateGameService();
        var game = gameService.CreateGame(1, 1, new List<QuizQuestion>());
        var code = game.Code;

        var hub = CreateHub(gameService: gameService, connectionId: "player-conn");

        // Act
        await hub.JoinAsPlayer(code, "TestPlayer");

        // Assert
        gameService.TryGetGame(code, out var updatedGame);
        Assert.True(updatedGame!.Players.ContainsKey("player-conn"));
        Assert.Equal("TestPlayer", updatedGame.Players["player-conn"].Username);
    }

    [Fact]
    public async Task JoinAsPlayer_GameAlreadyStarted_ThrowsException()
    {
        // Arrange
        var gameService = CreateGameService();
        var game = gameService.CreateGame(1, 1, new List<QuizQuestion>());
        game.Phase = GamePhase.Question; // Already started
        var code = game.Code;

        var hub = CreateHub(gameService: gameService);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HubException>(
            async () => await hub.JoinAsPlayer(code, "TestPlayer")
        );
        Assert.Contains("already started", exception.Message.ToLower());
    }

    [Fact]
    public async Task JoinAsPlayer_EmptyUsername_UsesDefaultName()
    {
        // Arrange
        var gameService = CreateGameService();
        var game = gameService.CreateGame(1, 1, new List<QuizQuestion>());
        var code = game.Code;

        var user = CreateUserPrincipal(5, "AuthenticatedUser");
        var hub = CreateHub(gameService: gameService, user: user, connectionId: "player-conn");

        // Act
        await hub.JoinAsPlayer(code, "");

        // Assert
        gameService.TryGetGame(code, out var updatedGame);
        Assert.True(updatedGame!.Players.ContainsKey("player-conn"));
        Assert.Equal("AuthenticatedUser", updatedGame.Players["player-conn"].Username);
    }

    [Fact]
    public async Task StartGame_ValidGame_StartsGame()
    {
        // Arrange
        var gameService = CreateGameService();
        var questions = new List<QuizQuestion>
        {
            new QuizQuestion
            {
                QuestionText = "Test Question",
                Options = new List<string> { "A", "B", "C" },
                CorrectOptionIndex = 0,
                TimeLimit = 10
            }
        };
        var game = gameService.CreateGame(1, 1, questions);
        var code = game.Code;

        var user = CreateUserPrincipal(1, "HostUser");
        var hub = CreateHub(gameService: gameService, user: user);

        // Act
        await hub.StartGame(code);

        // Assert
        gameService.TryGetGame(code, out var updatedGame);
        Assert.Equal(GamePhase.Question, updatedGame!.Phase);
        Assert.Equal(0, updatedGame.CurrentQuestionIndex);
    }

    [Fact]
    public async Task StartGame_NoQuestions_ThrowsException()
    {
        // Arrange
        var gameService = CreateGameService();
        var game = gameService.CreateGame(1, 1, new List<QuizQuestion>());
        var code = game.Code;

        var user = CreateUserPrincipal(1, "HostUser");
        var hub = CreateHub(gameService: gameService, user: user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HubException>(
            async () => await hub.StartGame(code)
        );
        Assert.Contains("no questions", exception.Message.ToLower());
    }

    [Fact]
    public async Task StartGame_NotHost_ThrowsForbidden()
    {
        // Arrange
        var gameService = CreateGameService();
        var questions = new List<QuizQuestion>
        {
            new QuizQuestion
            {
                QuestionText = "Test",
                Options = new List<string> { "A", "B" },
                CorrectOptionIndex = 0,
                TimeLimit = 10
            }
        };
        var game = gameService.CreateGame(1, 1, questions);
        var code = game.Code;

        var user = CreateUserPrincipal(2, "NotHost");
        var hub = CreateHub(gameService: gameService, user: user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HubException>(
            async () => await hub.StartGame(code)
        );
        Assert.Contains("forbidden", exception.Message.ToLower());
    }

    [Fact]
    public async Task SubmitAnswer_CorrectAnswer_UpdatesPlayerScore()
    {
        // Arrange
        var gameService = CreateGameService();
        var questions = new List<QuizQuestion>
        {
            new QuizQuestion
            {
                QuestionText = "Test Question",
                Options = new List<string> { "Correct", "Wrong1", "Wrong2" },
                CorrectOptionIndex = 0,
                TimeLimit = 10
            }
        };
        var game = gameService.CreateGame(1, 1, questions);
        game.Phase = GamePhase.Question;
        game.CurrentQuestionIndex = 0;
        game.QuestionStartTime = DateTimeOffset.UtcNow;
        game.QuestionEndTime = DateTimeOffset.UtcNow.AddSeconds(10);

        var player = new GamePlayer
        {
            ConnectionId = "player-conn",
            Username = "TestPlayer",
            TotalScore = 0
        };
        game.Players["player-conn"] = player;
        var code = game.Code;

        var hub = CreateHub(gameService: gameService, connectionId: "player-conn");

        // Act
        await hub.SubmitAnswer(code, 0); // Submit correct answer

        // Assert
        gameService.TryGetGame(code, out var updatedGame);
        var updatedPlayer = updatedGame!.Players["player-conn"];
        Assert.True(updatedPlayer.HasAnsweredCurrent);
        Assert.Equal(0, updatedPlayer.SelectedOptionIndex);
        Assert.True(updatedPlayer.TotalScore > 0); // Should have scored points
    }

    [Fact]
    public async Task SubmitAnswer_WrongAnswer_NoPoints()
    {
        // Arrange
        var gameService = CreateGameService();
        var questions = new List<QuizQuestion>
        {
            new QuizQuestion
            {
                QuestionText = "Test Question",
                Options = new List<string> { "Correct", "Wrong1", "Wrong2" },
                CorrectOptionIndex = 0,
                TimeLimit = 10
            }
        };
        var game = gameService.CreateGame(1, 1, questions);
        game.Phase = GamePhase.Question;
        game.CurrentQuestionIndex = 0;
        game.QuestionStartTime = DateTimeOffset.UtcNow;
        game.QuestionEndTime = DateTimeOffset.UtcNow.AddSeconds(10);

        var player = new GamePlayer
        {
            ConnectionId = "player-conn",
            Username = "TestPlayer",
            TotalScore = 0
        };
        game.Players["player-conn"] = player;
        var code = game.Code;

        var hub = CreateHub(gameService: gameService, connectionId: "player-conn");

        // Act
        await hub.SubmitAnswer(code, 1); // Submit wrong answer

        // Assert
        gameService.TryGetGame(code, out var updatedGame);
        var updatedPlayer = updatedGame!.Players["player-conn"];
        Assert.True(updatedPlayer.HasAnsweredCurrent);
        Assert.Equal(1, updatedPlayer.SelectedOptionIndex);
        Assert.Equal(0, updatedPlayer.TotalScore); // No points for wrong answer
    }

    [Fact]
    public async Task SubmitAnswer_AlreadyAnswered_IgnoresSecondAnswer()
    {
        // Arrange
        var gameService = CreateGameService();
        var questions = new List<QuizQuestion>
        {
            new QuizQuestion
            {
                QuestionText = "Test",
                Options = new List<string> { "A", "B" },
                CorrectOptionIndex = 0,
                TimeLimit = 10
            }
        };
        var game = gameService.CreateGame(1, 1, questions);
        game.Phase = GamePhase.Question;
        game.CurrentQuestionIndex = 0;
        game.QuestionStartTime = DateTimeOffset.UtcNow;
        game.QuestionEndTime = DateTimeOffset.UtcNow.AddSeconds(10);

        var player = new GamePlayer
        {
            ConnectionId = "player-conn",
            Username = "TestPlayer",
            TotalScore = 0,
            HasAnsweredCurrent = true, // Already answered
            SelectedOptionIndex = 0
        };
        game.Players["player-conn"] = player;
        var code = game.Code;

        var hub = CreateHub(gameService: gameService, connectionId: "player-conn");

        // Act
        await hub.SubmitAnswer(code, 1);

        // Assert
        gameService.TryGetGame(code, out var updatedGame);
        var updatedPlayer = updatedGame!.Players["player-conn"];
        Assert.Equal(0, updatedPlayer.SelectedOptionIndex); // Should still be the first answer
    }

    [Fact]
    public async Task NextQuestion_LastQuestion_EndsGame()
    {
        // Arrange
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        
        var options = new DbContextOptionsBuilder<backend.Data.AppDbContext>()
            .UseSqlite(connection)
            .Options;
        
        var context = new backend.Data.AppDbContext(options);
        context.Database.EnsureCreated();

        // Create test user and quiz in database
        var user = new backend.Models.User
        {
            Username = "HostUser",
            Email = "host@test.com",
            Password = "hashedpassword"
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var quiz = new backend.Models.Quiz
        {
            Title = "Test Quiz",
            CreatorID = user.Id,
            Questions = new List<backend.Models.QuizQuestion>
            {
                new backend.Models.QuizQuestion
                {
                    QuestionText = "Only Question",
                    Options = new List<string> { "A", "B" },
                    CorrectOptionIndex = 0,
                    TimeLimit = 10
                }
            }
        };
        context.Quizzes.Add(quiz);
        await context.SaveChangesAsync();

        var gameService = CreateGameService();
        var quizService = new QuizService(context);
        var userStatsService = new UserStatsService(context);

        var game = gameService.CreateGame(user.Id, quiz.ID, quiz.Questions.ToList());
        game.Phase = GamePhase.Leaderboard;
        game.CurrentQuestionIndex = 0; // On last question
        var code = game.Code;

        var userPrincipal = CreateUserPrincipal(user.Id, user.Username);
        var hub = CreateHub(
            gameService: gameService,
            quizService: quizService,
            userStatsService: userStatsService,
            user: userPrincipal);

        // Act
        await hub.NextQuestion(code);

        // Assert
        gameService.TryGetGame(code, out var updatedGame);
        Assert.Equal(GamePhase.Ended, updatedGame!.Phase);
        Assert.True(updatedGame.StatsRecorded);
        
        connection.Close();
    }

    [Fact]
    public async Task OnDisconnectedAsync_HostLeaves_EndsGame()
    {
        // Arrange
        var gameService = CreateGameService();
        var game = gameService.CreateGame(1, 1, new List<QuizQuestion>());
        game.Phase = GamePhase.Question;
        game.HostConnectionId = "host-conn";
        
        // Add host as a player so RemoveByConnection can find them
        var hostPlayer = new GamePlayer
        {
            ConnectionId = "host-conn",
            Username = "Host",
            TotalScore = 0
        };
        game.Players["host-conn"] = hostPlayer;
        var code = game.Code;

        var hub = CreateHub(gameService: gameService, connectionId: "host-conn");

        // Act
        await hub.OnDisconnectedAsync(null);

        // Assert
        var exists = gameService.TryGetGame(code, out var updatedGame);
        // Game should have been removed
        Assert.False(exists);
    }

    [Fact]
    public async Task OnDisconnectedAsync_PlayerLeaves_RemovesPlayerOnly()
    {
        // Arrange
        var gameService = CreateGameService();
        var game = gameService.CreateGame(1, 1, new List<QuizQuestion>());
        game.Phase = GamePhase.Lobby;
        game.HostConnectionId = "host-conn";
        
        var player = new GamePlayer
        {
            ConnectionId = "player-conn",
            Username = "TestPlayer",
            TotalScore = 0
        };
        game.Players["player-conn"] = player;
        var code = game.Code;

        var hub = CreateHub(gameService: gameService, connectionId: "player-conn");

        // Act
        await hub.OnDisconnectedAsync(null);

        // Assert
        var exists = gameService.TryGetGame(code, out var updatedGame);
        // Game should still exist
        Assert.True(exists);
        Assert.NotEqual(GamePhase.Ended, updatedGame!.Phase);
        // Player should be removed
        Assert.False(updatedGame.Players.ContainsKey("player-conn"));
    }

    [Fact]
    public async Task GroupName_CodeFormatting_WorksCorrectly()
    {
        // This tests that game codes work regardless of case
        // Arrange
        var gameService = CreateGameService();
        var game = gameService.CreateGame(1, 1, new List<QuizQuestion>());
        var code = game.Code.ToLower(); // Use lowercase

        var user = CreateUserPrincipal(1, "HostUser");
        var hub = CreateHub(gameService: gameService, user: user, connectionId: "host-conn");

        // Act - should work with lowercase code
        await hub.JoinAsHost(code);

        // Assert - connection should be established
        gameService.TryGetGame(code, out var updatedGame);
        Assert.Equal("host-conn", updatedGame!.HostConnectionId);
    }

    [Fact]
    public async Task SkipQuestion_ValidGame_EndsQuestion()
    {
        // Arrange
        var gameService = CreateGameService();
        var questions = new List<QuizQuestion>
        {
            new QuizQuestion
            {
                QuestionText = "Question to skip",
                Options = new List<string> { "A", "B" },
                CorrectOptionIndex = 0,
                TimeLimit = 10
            }
        };
        var game = gameService.CreateGame(1, 1, questions);
        game.Phase = GamePhase.Question;
        game.CurrentQuestionIndex = 0;
        game.QuestionStartTime = DateTimeOffset.UtcNow;
        game.QuestionEndTime = DateTimeOffset.UtcNow.AddSeconds(10);
        var code = game.Code;

        var user = CreateUserPrincipal(1, "HostUser");
        var hub = CreateHub(gameService: gameService, user: user);

        // Act
        await hub.SkipQuestion(code);

        // Assert
        gameService.TryGetGame(code, out var updatedGame);
        Assert.Equal(GamePhase.Leaderboard, updatedGame!.Phase);
    }

    [Fact]
    public async Task SkipQuestion_NotHost_ThrowsForbidden()
    {
        // Arrange
        var gameService = CreateGameService();
        var questions = new List<QuizQuestion>
        {
            new QuizQuestion
            {
                QuestionText = "Test",
                Options = new List<string> { "A", "B" },
                CorrectOptionIndex = 0,
                TimeLimit = 10
            }
        };
        var game = gameService.CreateGame(1, 1, questions);
        game.Phase = GamePhase.Question;
        game.CurrentQuestionIndex = 0;
        var code = game.Code;

        var user = CreateUserPrincipal(2, "NotHost");
        var hub = CreateHub(gameService: gameService, user: user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HubException>(
            async () => await hub.SkipQuestion(code)
        );
        Assert.Contains("forbidden", exception.Message.ToLower());
    }

    [Fact]
    public async Task SkipQuestion_GameNotFound_ThrowsException()
    {
        // Arrange
        var gameService = CreateGameService();
        var user = CreateUserPrincipal(1, "HostUser");
        var hub = CreateHub(gameService: gameService, user: user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HubException>(
            async () => await hub.SkipQuestion("NOTFOUND")
        );
        Assert.Contains("not found", exception.Message.ToLower());
    }

    [Fact]
    public async Task SubmitAnswer_AfterTimeExpired_IgnoresAnswer()
    {
        // Arrange
        var gameService = CreateGameService();
        var questions = new List<QuizQuestion>
        {
            new QuizQuestion
            {
                QuestionText = "Test",
                Options = new List<string> { "A", "B" },
                CorrectOptionIndex = 0,
                TimeLimit = 10
            }
        };
        var game = gameService.CreateGame(1, 1, questions);
        game.Phase = GamePhase.Question;
        game.CurrentQuestionIndex = 0;
        game.QuestionStartTime = DateTimeOffset.UtcNow.AddSeconds(-20);
        game.QuestionEndTime = DateTimeOffset.UtcNow.AddSeconds(-10); // Already expired

        var player = new GamePlayer
        {
            ConnectionId = "player-conn",
            Username = "TestPlayer",
            TotalScore = 0
        };
        game.Players["player-conn"] = player;
        var code = game.Code;

        var hub = CreateHub(gameService: gameService, connectionId: "player-conn");

        // Act
        await hub.SubmitAnswer(code, 0);

        // Assert - player should not have answered
        gameService.TryGetGame(code, out var updatedGame);
        var updatedPlayer = updatedGame!.Players["player-conn"];
        Assert.False(updatedPlayer.HasAnsweredCurrent);
    }

    [Fact]
    public async Task SubmitAnswer_NotInQuestionPhase_IsIgnored()
    {
        // Arrange
        var gameService = CreateGameService();
        var game = gameService.CreateGame(1, 1, new List<QuizQuestion>());
        game.Phase = GamePhase.Lobby; // Not in Question phase

        var player = new GamePlayer
        {
            ConnectionId = "player-conn",
            Username = "TestPlayer",
            TotalScore = 0
        };
        game.Players["player-conn"] = player;
        var code = game.Code;

        var hub = CreateHub(gameService: gameService, connectionId: "player-conn");

        // Act
        await hub.SubmitAnswer(code, 0);

        // Assert - nothing should happen
        gameService.TryGetGame(code, out var updatedGame);
        Assert.Equal(GamePhase.Lobby, updatedGame!.Phase);
    }

    [Fact]
    public async Task SubmitAnswer_PlayerNotInGame_IsIgnored()
    {
        // Arrange
        var gameService = CreateGameService();
        var questions = new List<QuizQuestion>
        {
            new QuizQuestion
            {
                QuestionText = "Test",
                Options = new List<string> { "A", "B" },
                CorrectOptionIndex = 0,
                TimeLimit = 10
            }
        };
        var game = gameService.CreateGame(1, 1, questions);
        game.Phase = GamePhase.Question;
        game.CurrentQuestionIndex = 0;
        game.QuestionStartTime = DateTimeOffset.UtcNow;
        game.QuestionEndTime = DateTimeOffset.UtcNow.AddSeconds(10);
        var code = game.Code;

        // Don't add player to game
        var hub = CreateHub(gameService: gameService, connectionId: "unknown-conn");

        // Act - should not crash
        await hub.SubmitAnswer(code, 0);

        // Assert - game should be unchanged
        gameService.TryGetGame(code, out var updatedGame);
        Assert.Empty(updatedGame!.Players);
    }

    [Fact]
    public async Task JoinAsPlayer_WithAuthenticatedUser_UsesUserId()
    {
        // Arrange
        var gameService = CreateGameService();
        var game = gameService.CreateGame(1, 1, new List<QuizQuestion>());
        var code = game.Code;

        var user = CreateUserPrincipal(42, "AuthUser");
        var hub = CreateHub(gameService: gameService, user: user, connectionId: "player-conn");

        // Act
        await hub.JoinAsPlayer(code, "CustomName");

        // Assert
        gameService.TryGetGame(code, out var updatedGame);
        var player = updatedGame!.Players["player-conn"];
        Assert.Equal(42, player.UserId);
        Assert.Equal("CustomName", player.Username);
    }

    [Fact]
    public async Task JoinAsPlayer_WithoutUsername_UsesConnectionIdSuffix()
    {
        // Arrange
        var gameService = CreateGameService();
        var game = gameService.CreateGame(1, 1, new List<QuizQuestion>());
        var code = game.Code;

        var hub = CreateHub(gameService: gameService, connectionId: "conn-abcd1234");

        // Act
        await hub.JoinAsPlayer(code, "");

        // Assert
        gameService.TryGetGame(code, out var updatedGame);
        var player = updatedGame!.Players["conn-abcd1234"];
        Assert.Contains("Player-", player.Username);
    }

    [Fact]
    public async Task NextQuestion_NotHost_ThrowsForbidden()
    {
        // Arrange
        var gameService = CreateGameService();
        var questions = new List<QuizQuestion>
        {
            new QuizQuestion
            {
                QuestionText = "Test",
                Options = new List<string> { "A", "B" },
                CorrectOptionIndex = 0,
                TimeLimit = 10
            }
        };
        var game = gameService.CreateGame(1, 1, questions);
        game.Phase = GamePhase.Leaderboard;
        var code = game.Code;

        var user = CreateUserPrincipal(2, "NotHost");
        var hub = CreateHub(gameService: gameService, user: user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HubException>(
            async () => await hub.NextQuestion(code)
        );
        Assert.Contains("forbidden", exception.Message.ToLower());
    }

    [Fact]
    public async Task NextQuestion_GameNotFound_ThrowsException()
    {
        // Arrange
        var gameService = CreateGameService();
        var user = CreateUserPrincipal(1, "HostUser");
        var hub = CreateHub(gameService: gameService, user: user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HubException>(
            async () => await hub.NextQuestion("NOTFOUND")
        );
        Assert.Contains("not found", exception.Message.ToLower());
    }
}

