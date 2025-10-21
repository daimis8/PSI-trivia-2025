using Microsoft.AspNetCore.Mvc;
using backend.Services;
using backend.Models;
using backend.DTOs;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace backend.Controllers;

[ApiController]
[Route("api")]
public class AuthController : ControllerBase
{
    private readonly UserService _userService;
    private readonly JwtService _jwtService;
    private readonly PasswordService _passwordService;
    private static readonly Regex EmailRegex = new Regex(
        @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    public AuthController(UserService userService, JwtService jwtService, PasswordService passwordService)
    {
        _userService = userService;
        _jwtService = jwtService;
        _passwordService = passwordService;
    }

    // Register endpoint
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] User user)
    {
        var existingUserByEmail = await _userService.GetUserByEmailAsync(user.Email);
        if (existingUserByEmail != null)
        {
            return BadRequest(new { message = "Email already exists" });
        }

        if (user.Password.Length < 8)
        {
            return BadRequest(new { message = "Password must be at least 8 characters long" });
        }

        var existingUserByUsername = await _userService.GetUserByUsernameAsync(user.Username);
        if (existingUserByUsername != null)
        {
            return BadRequest(new { message = "Username already exists" });
        }

        if (!EmailRegex.IsMatch(user.Email))
        {
            return BadRequest(new { message = "Invalid email format" });
        }

        user.Password = _passwordService.HashPassword(user.Password);

        var newUser = await _userService.AddUserAsync(user);
        
        return Ok(new 
        { 
            message = "Registration successful"
        });
    }

    // Login endpoint
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password) || string.IsNullOrEmpty(request.Username))
        {
            return BadRequest(new { message = "Email and password are required" });
        }

        var user = await _userService.ValidateLoginAsync(request.Email, request.Password, request.Username);
        
        if (user == null)
        {
            return Unauthorized(new { message = "Invalid email, password or username" });
        }

        var token = _jwtService.GenerateToken(user);

        Response.Cookies.Append("authToken", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = false, // for development false
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddHours(1)
        });
        
        return Ok(new 
        { 
            token = token,
            user = new 
            {
                id = user.Id,
                username = user.Username,
                email = user.Email
            },
            message = "Login successful"
        });
    }

    // logout endpoint
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("authToken");
        return Ok(new { message = "Logged out successfully" });
    }

    // authorized or no
    [Authorize]
    [HttpGet("authorized")]
    public async Task<IActionResult> Authorized()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        var username = User.FindFirst(ClaimTypes.Name)?.Value;

        if (userId == null || email == null)
        {
            return Unauthorized(new { message = "Unauthorized" });
        }

        var user = await _userService.GetUserByIdAsync(int.Parse(userId));

        if (user == null)
        {
            Response.Cookies.Delete("authToken");
            return Unauthorized(new { authenticated = false, message = "User not found" });
        }

        return Ok(new { 
            authenticated = true,
            user = new 
            {
                id = int.Parse(userId),
                username = username,
                email = email
            },
            message = "Authorized" 
        });
    }
}