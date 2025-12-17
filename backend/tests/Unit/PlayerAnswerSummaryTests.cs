using backend.Models;
using Xunit;

namespace tests.Unit;

public class PlayerAnswerSummaryTests
{
    [Fact]
    public void PlayerAnswerSummary_CanBeCreated_WithRequiredProperties()
    {
        // Arrange & Act
        var summary = new PlayerAnswerSummary
        {
            Username = "TestPlayer",
            Correct = true,
            Points = 100,
            TimeMs = 1500
        };

        // Assert
        Assert.NotNull(summary);
        Assert.Equal("TestPlayer", summary.Username);
        Assert.True(summary.Correct);
        Assert.Equal(100, summary.Points);
        Assert.Equal(1500, summary.TimeMs);
    }

    [Fact]
    public void PlayerAnswerSummary_CorrectAnswer_HasCorrectProperties()
    {
        // Arrange & Act
        var summary = new PlayerAnswerSummary
        {
            Username = "Winner",
            Correct = true,
            Points = 1000,
            TimeMs = 500
        };

        // Assert
        Assert.Equal("Winner", summary.Username);
        Assert.True(summary.Correct);
        Assert.Equal(1000, summary.Points);
        Assert.Equal(500, summary.TimeMs);
    }

    [Fact]
    public void PlayerAnswerSummary_IncorrectAnswer_HasZeroPoints()
    {
        // Arrange & Act
        var summary = new PlayerAnswerSummary
        {
            Username = "Player1",
            Correct = false,
            Points = 0,
            TimeMs = 2000
        };

        // Assert
        Assert.Equal("Player1", summary.Username);
        Assert.False(summary.Correct);
        Assert.Equal(0, summary.Points);
        Assert.Equal(2000, summary.TimeMs);
    }

    [Fact]
    public void PlayerAnswerSummary_WithFastAnswer_HasLowTimeMs()
    {
        // Arrange & Act
        var summary = new PlayerAnswerSummary
        {
            Username = "SpeedyPlayer",
            Correct = true,
            Points = 1500,
            TimeMs = 100
        };

        // Assert
        Assert.Equal("SpeedyPlayer", summary.Username);
        Assert.True(summary.Correct);
        Assert.Equal(1500, summary.Points);
        Assert.Equal(100, summary.TimeMs);
        Assert.True(summary.TimeMs < 1000);
    }

    [Fact]
    public void PlayerAnswerSummary_WithSlowAnswer_HasHighTimeMs()
    {
        // Arrange & Act
        var summary = new PlayerAnswerSummary
        {
            Username = "SlowPlayer",
            Correct = true,
            Points = 500,
            TimeMs = 5000
        };

        // Assert
        Assert.Equal("SlowPlayer", summary.Username);
        Assert.True(summary.Correct);
        Assert.Equal(500, summary.Points);
        Assert.Equal(5000, summary.TimeMs);
        Assert.True(summary.TimeMs > 1000); // Slow answer (more than 1 second)
    }

    [Fact]
    public void PlayerAnswerSummary_WithLongUsername_StoresCorrectly()
    {
        // Arrange
        var longUsername = "VeryLongUsernameWithManyCharacters123456";

        // Act
        var summary = new PlayerAnswerSummary
        {
            Username = longUsername,
            Correct = true,
            Points = 750,
            TimeMs = 1200
        };

        // Assert
        Assert.Equal(longUsername, summary.Username);
        Assert.Equal(40, summary.Username.Length);
    }

    [Fact]
    public void PlayerAnswerSummary_WithMaxPoints_StoresCorrectly()
    {
        // Arrange & Act
        var summary = new PlayerAnswerSummary
        {
            Username = "ProPlayer",
            Correct = true,
            Points = int.MaxValue,
            TimeMs = 1
        };

        // Assert
        Assert.Equal("ProPlayer", summary.Username);
        Assert.True(summary.Correct);
        Assert.Equal(int.MaxValue, summary.Points);
        Assert.Equal(1, summary.TimeMs);
    }

    [Fact]
    public void PlayerAnswerSummary_WithMaxTimeMs_StoresCorrectly()
    {
        // Arrange & Act
        var summary = new PlayerAnswerSummary
        {
            Username = "SlowPoke",
            Correct = false,
            Points = 0,
            TimeMs = long.MaxValue
        };

        // Assert
        Assert.Equal("SlowPoke", summary.Username);
        Assert.False(summary.Correct);
        Assert.Equal(0, summary.Points);
        Assert.Equal(long.MaxValue, summary.TimeMs);
    }

    [Fact]
    public void PlayerAnswerSummary_PropertiesCanBeModified()
    {
        // Arrange
        var summary = new PlayerAnswerSummary
        {
            Username = "Player1",
            Correct = false,
            Points = 0,
            TimeMs = 1000
        };

        // Act
        summary.Username = "Player2";
        summary.Correct = true;
        summary.Points = 1000;
        summary.TimeMs = 500;

        // Assert
        Assert.Equal("Player2", summary.Username);
        Assert.True(summary.Correct);
        Assert.Equal(1000, summary.Points);
        Assert.Equal(500, summary.TimeMs);
    }

    [Fact]
    public void PlayerAnswerSummary_WithSpecialCharacters_InUsername()
    {
        // Arrange & Act
        var summary = new PlayerAnswerSummary
        {
            Username = "Player@#$%",
            Correct = true,
            Points = 850,
            TimeMs = 1800
        };

        // Assert
        Assert.Equal("Player@#$%", summary.Username);
        Assert.Contains("@", summary.Username);
        Assert.Contains("#", summary.Username);
        Assert.Contains("$", summary.Username);
    }

    [Fact]
    public void PlayerAnswerSummary_WithZeroTimeMs_IsValid()
    {
        // Arrange & Act
        var summary = new PlayerAnswerSummary
        {
            Username = "InstantPlayer",
            Correct = true,
            Points = 2000,
            TimeMs = 0
        };

        // Assert
        Assert.Equal("InstantPlayer", summary.Username);
        Assert.True(summary.Correct);
        Assert.Equal(2000, summary.Points);
        Assert.Equal(0, summary.TimeMs);
    }

    [Fact]
    public void PlayerAnswerSummary_MultipleInstances_AreIndependent()
    {
        // Arrange & Act
        var summary1 = new PlayerAnswerSummary
        {
            Username = "Player1",
            Correct = true,
            Points = 500,
            TimeMs = 1000
        };

        var summary2 = new PlayerAnswerSummary
        {
            Username = "Player2",
            Correct = false,
            Points = 0,
            TimeMs = 2000
        };

        // Assert
        Assert.NotEqual(summary1.Username, summary2.Username);
        Assert.NotEqual(summary1.Correct, summary2.Correct);
        Assert.NotEqual(summary1.Points, summary2.Points);
        Assert.NotEqual(summary1.TimeMs, summary2.TimeMs);
    }
}

