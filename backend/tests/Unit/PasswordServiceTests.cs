using Xunit;
using backend.Services;

namespace tests.Unit;

public class PasswordServiceTests
{
    private readonly PasswordService _passwordService;

    public PasswordServiceTests()
    {
        _passwordService = new PasswordService();
    }

    [Fact]
    public void HashPassword_CreatesNonEmptyHash()
    {
        // Arrange
        var password = "testPassword123";

        // Act
        var hash = _passwordService.HashPassword(password);

        // Assert
        Assert.NotNull(hash);
        Assert.NotEmpty(hash);
        Assert.NotEqual(password, hash);
    }

    [Fact]
    public void HashPassword_CreatesDifferentHashesForSamePassword()
    {
        // Arrange
        var password = "testPassword123";

        // Act
        var hash1 = _passwordService.HashPassword(password);
        var hash2 = _passwordService.HashPassword(password);

        // Assert
        Assert.NotEqual(hash1, hash2); // BCrypt includes salt, so hashes differ
    }

    [Fact]
    public void VerifyPassword_WithCorrectPassword_ReturnsTrue()
    {
        // Arrange
        var password = "correctPassword";
        var hash = _passwordService.HashPassword(password);

        // Act
        var result = _passwordService.VerifyPassword(password, hash);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void VerifyPassword_WithIncorrectPassword_ReturnsFalse()
    {
        // Arrange
        var correctPassword = "correctPassword";
        var incorrectPassword = "wrongPassword";
        var hash = _passwordService.HashPassword(correctPassword);

        // Act
        var result = _passwordService.VerifyPassword(incorrectPassword, hash);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void VerifyPassword_WithEmptyPassword_ReturnsFalse()
    {
        // Arrange
        var password = "testPassword";
        var hash = _passwordService.HashPassword(password);

        // Act
        var result = _passwordService.VerifyPassword("", hash);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("short")]
    [InlineData("averagePasswordLength123")]
    [InlineData("VeryLongPasswordWithSpecialCharacters!@#$%^&*()_+")]
    public void HashPassword_HandlesVariousPasswordLengths(string password)
    {
        // Act
        var hash = _passwordService.HashPassword(password);
        var isValid = _passwordService.VerifyPassword(password, hash);

        // Assert
        Assert.NotNull(hash);
        Assert.True(isValid);
    }
}

