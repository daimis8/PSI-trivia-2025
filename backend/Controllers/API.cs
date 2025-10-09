using Microsoft.AspNetCore.Mvc;
using backend.Services;
using backend.Models;
using System.Text.RegularExpressions;
using backend.DTOs;
namespace backend.Controllers;

[ApiController]
[Route("api")]
public class APIController : ControllerBase
{
    private readonly UserService _userService;

    public APIController(UserService userService)
    {
        _userService = userService;
    }

    private static readonly Regex EmailRegex = new Regex(
        @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
);

    // Get all users
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _userService.GetAllUsersAsync();
        return Ok(users);
    }

    // Create a new user
    [HttpPost("users")]
    public async Task<IActionResult> CreateUser([FromBody] User user)
    {
        var existingUser = await _userService.GetUserByEmailAsync(user.Email);
        if (existingUser != null)
        {
            return BadRequest(new { message = "Email already exists" });
        }

        var existingEmail = await _userService.GetUserByEmailAsync(user.Email);
        if (existingEmail != null)
        {
            return BadRequest(new { message = "Email already exists" });
        }

        if (user.Password.Length < 8)
        {
            return BadRequest(new { message = "Password must be at least 8 characters long" });
        }

        if (!EmailRegex.IsMatch(user.Email))
        {
            return BadRequest(new { message = "Invalid email format" });
        }

        var newUser = await _userService.AddUserAsync(user);
        return CreatedAtAction(nameof(GetUsers), new { id = newUser.Id }, newUser);
    }

    //login endpoint
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
        {
            return BadRequest(new { message = "Email and password are required" });
        }

        var user = await _userService.ValidateLoginAsync(request.Email, request.Password);
        
        if (user == null)
        {
            return Unauthorized(new { message = "Invalid email or password" });
        }
        
        return Ok(new 
        { 
            id = user.Id,
            email = user.Email,
            message = "Login successful"
        });
    }

    [HttpDelete("users/{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var deleted = await _userService.DeleteUserAsync(id);
        
        if (!deleted)
        {
            return NotFound(new { message = "User not found" });
        }
        
        return Ok(new { message = "User deleted successfully" });
    }
}