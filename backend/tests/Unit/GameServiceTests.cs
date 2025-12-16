using Xunit;
using backend.Services;
using backend.Models;
using System.Collections.Generic;

namespace tests.Unit;

public class GameServiceTests
{
    [Fact]
    public void CreateGame_GeneratesUniqueCode()
    {
        // Arrange
        var service = new GameService();
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

        // Act
        var game = service.CreateGame(1, 1, questions);

        // Assert
        Assert.NotNull(game);
        Assert.NotNull(game.Code);
        Assert.Equal(6, game.Code.Length);
        Assert.All(game.Code, c => Assert.True(char.IsLetter(c) && char.IsUpper(c)));
    }

    [Fact]
    public void CreateGame_SetsCorrectProperties()
    {
        // Arrange
        var service = new GameService();
        var hostUserId = 42;
        var quizId = 123;
        var questions = new List<QuizQuestion>
        {
            new QuizQuestion
            {
                QuestionText = "Test",
                Options = new List<string> { "A", "B", "C" },
                CorrectOptionIndex = 1,
                TimeLimit = 15
            }
        };

        // Act
        var game = service.CreateGame(hostUserId, quizId, questions);

        // Assert
        Assert.Equal(hostUserId, game.HostUserId);
        Assert.Equal(quizId, game.QuizId);
        Assert.Equal(questions, game.Questions);
        Assert.Equal(GamePhase.Lobby, game.Phase);
        Assert.Equal(-1, game.CurrentQuestionIndex);
    }

    [Fact]
    public void CreateGame_MultipleGames_HaveUniqueCodes()
    {
        // Arrange
        var service = new GameService();
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

        // Act
        var game1 = service.CreateGame(1, 1, questions);
        var game2 = service.CreateGame(2, 2, questions);
        var game3 = service.CreateGame(3, 3, questions);

        // Assert
        Assert.NotEqual(game1.Code, game2.Code);
        Assert.NotEqual(game2.Code, game3.Code);
        Assert.NotEqual(game1.Code, game3.Code);
    }

    [Fact]
    public void TryGetGame_ExistingGame_ReturnsTrue()
    {
        // Arrange
        var service = new GameService();
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
        var createdGame = service.CreateGame(1, 1, questions);

        // Act
        var result = service.TryGetGame(createdGame.Code, out var retrievedGame);

        // Assert
        Assert.True(result);
        Assert.NotNull(retrievedGame);
        Assert.Equal(createdGame.Code, retrievedGame.Code);
        Assert.Equal(createdGame.HostUserId, retrievedGame.HostUserId);
    }

    [Fact]
    public void TryGetGame_NonExistentGame_ReturnsFalse()
    {
        // Arrange
        var service = new GameService();

        // Act
        var result = service.TryGetGame("NONEXIST", out var game);

        // Assert
        Assert.False(result);
        Assert.Null(game);
    }

    [Fact]
    public void TryGetGame_CaseInsensitive_ReturnsGame()
    {
        // Arrange
        var service = new GameService();
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
        var createdGame = service.CreateGame(1, 1, questions);

        // Act - use lowercase
        var result = service.TryGetGame(createdGame.Code.ToLower(), out var retrievedGame);

        // Assert
        Assert.True(result);
        Assert.NotNull(retrievedGame);
    }

    [Fact]
    public void RemoveGame_ExistingGame_RemovesIt()
    {
        // Arrange
        var service = new GameService();
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
        var createdGame = service.CreateGame(1, 1, questions);

        // Act
        service.RemoveGame(createdGame.Code);
        var stillExists = service.TryGetGame(createdGame.Code, out _);

        // Assert
        Assert.False(stillExists);
    }

    [Fact]
    public void RemoveGame_NonExistentGame_DoesNotThrow()
    {
        // Arrange
        var service = new GameService();

        // Act & Assert - should not throw
        service.RemoveGame("NONEXIST");
    }

    [Fact]
    public void RemoveByConnection_PlayerInGame_RemovesPlayerAndReturnsGame()
    {
        // Arrange
        var service = new GameService();
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
        var createdGame = service.CreateGame(1, 1, questions);
        
        var connectionId = "conn123";
        var player = new GamePlayer
        {
            ConnectionId = connectionId,
            Username = "TestPlayer",
            UserId = null
        };
        createdGame.Players.TryAdd(connectionId, player);

        // Act
        var result = service.RemoveByConnection(connectionId, out var foundGame);

        // Assert
        Assert.True(result);
        Assert.NotNull(foundGame);
        Assert.Equal(createdGame.Code, foundGame.Code);
        Assert.False(createdGame.Players.ContainsKey(connectionId));
    }

    [Fact]
    public void RemoveByConnection_NonExistentConnection_ReturnsFalse()
    {
        // Arrange
        var service = new GameService();
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
        service.CreateGame(1, 1, questions);

        // Act
        var result = service.RemoveByConnection("nonexistent", out var foundGame);

        // Assert
        Assert.False(result);
        Assert.Null(foundGame);
    }

    [Fact]
    public void CreateGame_WithEmptyQuestions_CreatesGame()
    {
        // Arrange
        var service = new GameService();
        var questions = new List<QuizQuestion>();

        // Act
        var game = service.CreateGame(1, 1, questions);

        // Assert
        Assert.NotNull(game);
        Assert.Empty(game.Questions);
    }

    [Fact]
    public void CreateGame_WithMultipleQuestions_StoresAllQuestions()
    {
        // Arrange
        var service = new GameService();
        var questions = new List<QuizQuestion>
        {
            new QuizQuestion
            {
                QuestionText = "Question 1",
                Options = new List<string> { "A", "B" },
                CorrectOptionIndex = 0,
                TimeLimit = 10
            },
            new QuizQuestion
            {
                QuestionText = "Question 2",
                Options = new List<string> { "C", "D", "E" },
                CorrectOptionIndex = 1,
                TimeLimit = 15
            }
        };

        // Act
        var game = service.CreateGame(1, 1, questions);

        // Assert
        Assert.Equal(2, game.Questions.Count);
        Assert.Equal("Question 1", game.Questions[0].QuestionText);
        Assert.Equal("Question 2", game.Questions[1].QuestionText);
    }

    [Fact]
    public void GameService_SupportsMultipleConcurrentGames()
    {
        // Arrange
        var service = new GameService();
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

        // Act
        var game1 = service.CreateGame(1, 1, questions);
        var game2 = service.CreateGame(2, 2, questions);
        var game3 = service.CreateGame(3, 3, questions);

        // Assert - all games should be retrievable
        Assert.True(service.TryGetGame(game1.Code, out _));
        Assert.True(service.TryGetGame(game2.Code, out _));
        Assert.True(service.TryGetGame(game3.Code, out _));
    }
}

