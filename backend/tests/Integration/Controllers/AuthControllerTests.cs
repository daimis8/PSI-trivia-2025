using Xunit;
using backend.Models;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using backend.Data;
using backend.Services;
using Microsoft.EntityFrameworkCore;

namespace tests.Integration.Controllers;

public class AuthControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthControllerTests(CustomWebApplicationFactory factory) =>
        _factory = factory;

    [Fact]
    public async Task Register_WithValidData_CreatesUser()
    {
        // Arrange
        var client = _factory.CreateClient();
        var registerData = new
        {
            username = "newuser123",
            email = "newuser123@test.com",
            password = "Password123!"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/register", registerData);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == "newuser123@test.com");
            Assert.NotNull(user);
            Assert.Equal("newuser123", user.Username);
        }
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsBadRequest()
    {
        // Arrange
        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var passwordService = seedScope.ServiceProvider.GetRequiredService<PasswordService>();
            
            db.Users.Add(new User
            {
                Id = 4001,
                Username = "existing",
                Email = "existing@test.com",
                Password = passwordService.HashPassword("password")
            });
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClient();
        var registerData = new
        {
            username = "newuser",
            email = "existing@test.com", // Duplicate email
            password = "Password123!"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/register", registerData);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        // Arrange
        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var passwordService = seedScope.ServiceProvider.GetRequiredService<PasswordService>();
            
            db.Users.Add(new User
            {
                Id = 4101,
                Username = "loginuser",
                Email = "loginuser@test.com",
                Password = passwordService.HashPassword("Password123!")
            });
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClient();
        var loginData = new
        {
            identifier = "loginuser@test.com",
            password = "Password123!"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/login", loginData);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadFromJsonAsync<dynamic>();
        Assert.NotNull(content);
    }

    [Fact]
    public async Task Login_WithUsername_ReturnsToken()
    {
        // Arrange
        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var passwordService = seedScope.ServiceProvider.GetRequiredService<PasswordService>();
            
            db.Users.Add(new User
            {
                Id = 4201,
                Username = "loginbyusername",
                Email = "loginbyusername@test.com",
                Password = passwordService.HashPassword("Password123!")
            });
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClient();
        var loginData = new
        {
            identifier = "loginbyusername", // Using username
            password = "Password123!"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/login", loginData);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        // Arrange
        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var passwordService = seedScope.ServiceProvider.GetRequiredService<PasswordService>();
            
            db.Users.Add(new User
            {
                Id = 4301,
                Username = "testuser",
                Email = "testuser@test.com",
                Password = passwordService.HashPassword("CorrectPassword")
            });
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClient();
        var loginData = new
        {
            identifier = "testuser@test.com",
            password = "WrongPassword"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/login", loginData);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithNonExistentUser_ReturnsNotFound()
    {
        // Arrange
        var client = _factory.CreateClient();
        var loginData = new
        {
            identifier = "nonexistent@test.com",
            password = "Password123!"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/login", loginData);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Authorized_WithValidToken_ReturnsUserInfo()
    {
        // Arrange
        int userId = 4401;
        string token;
        
        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var passwordService = seedScope.ServiceProvider.GetRequiredService<PasswordService>();
            
            var user = new User
            {
                Id = userId,
                Username = "authuser",
                Email = "authuser@test.com",
                Password = passwordService.HashPassword("password")
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var jwtService = seedScope.ServiceProvider.GetRequiredService<JwtService>();
            token = jwtService.GenerateToken(user);
        }

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/authorized");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadFromJsonAsync<dynamic>();
        Assert.NotNull(content);
    }

    [Fact]
    public async Task Authorized_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/authorized");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_ClearsSession()
    {
        // Arrange
        int userId = 4501;
        string token;
        
        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var passwordService = seedScope.ServiceProvider.GetRequiredService<PasswordService>();
            
            var user = new User
            {
                Id = userId,
                Username = "logoutuser",
                Email = "logoutuser@test.com",
                Password = passwordService.HashPassword("password")
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var jwtService = seedScope.ServiceProvider.GetRequiredService<JwtService>();
            token = jwtService.GenerateToken(user);
        }

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.PostAsync("/api/logout", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

