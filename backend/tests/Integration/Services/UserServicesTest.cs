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

public class UserServicesTests :IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    public UserServicesTests(CustomWebApplicationFactory factory) =>
        _factory = factory;

    [Fact]
    public async Task GetExistingUserByEmailTest()
    {
        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Users.Add(new User { Id = 1, Username = "testuser", Email = "test@test.com", Password = "password123"});
            await db.SaveChangesAsync();
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var testService = scope.ServiceProvider.GetRequiredService<UserService>();
            var resultUser = await testService.GetUserByEmailAsync("test@test.com");

            Assert.Equal("testuser", resultUser!.Username);
        }
    }

    [Fact]
    public async Task TryGettingNonexistantUserByEmailTest()
    {
        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Users.Add(new User { Id = 2, Username = "testuser1", Email = "test1@test.com", Password = "password123"});
            await db.SaveChangesAsync();
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var testService = scope.ServiceProvider.GetRequiredService<UserService>();
            var resultUser = await testService.GetUserByEmailAsync("t@test.com");

            Assert.Null(resultUser);
        }
    }

    [Fact]
    public async Task HttpPutUpdateUsernameTest()
    {
        int userId = 3;
        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = new User { Id = userId, Username = "oldname", Email = "old@test.com", Password = "password123"};
            db.Users.Add(user);
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

        var newName = new { username = "newname" };
        var response = await client.PutAsJsonAsync("/api/users/username", newName);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using (var checkScope = _factory.Services.CreateScope())
        {
            var db = checkScope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.True(db.Users.Any(u => u.Username  == "newname"));
        }
    }
}