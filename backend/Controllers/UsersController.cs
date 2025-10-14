using Microsoft.AspNetCore.Mvc;
using backend.Services;
using backend.DTOs;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace backend.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly UserService _userService;
    private readonly JwtService _jwtService;

    public UsersController(UserService userService, JwtService jwtService)
    {
        _userService = userService;
        _jwtService = jwtService;
    }

    // Get all users
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _userService.GetAllUsersAsync();
        return Ok(users);
    }

    // Get user by ID
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        
        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }
        
        return Ok(user);
    }

    // Delete user by ID
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _userService.DeleteUserAsync(id);
        
        if (!deleted)
        {
            return NotFound(new { message = "User not found" });
        }
        
        return Ok(new { message = "User deleted successfully" });
    }

    // Update username
    [Authorize]
    [HttpPut("username")]
    public async Task<IActionResult> UpdateUsername([FromBody] UpdateUsernameRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (userId == null)
        {
            return Unauthorized(new { message = "Unauthorized" });
        }

        var (updatedUser, error) = await _userService.UpdateUsernameAsync(int.Parse(userId), request.Username);

        if (error != null)
        {
            return BadRequest(new { message = error });
        }

        var newToken = _jwtService.GenerateToken(updatedUser!);

        Response.Cookies.Append("authToken", newToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = false, //for development
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddHours(1)
        });

        return Ok(new 
        { 
            message = "Username updated successfully",
            user = new 
            {
                id = updatedUser!.Id,
                username = updatedUser.Username,
                email = updatedUser.Email
            },
            token = newToken
        });
    }

    // Update email
    [Authorize]
    [HttpPut("email")]
    public async Task<IActionResult> UpdateEmail([FromBody] UpdateEmailRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (userId == null)
        {
            return Unauthorized(new { message = "Unauthorized" });
        }

        var (updatedUser, error) = await _userService.UpdateEmailAsync(int.Parse(userId), request.Email);

        if (error != null)
        {
            return BadRequest(new { message = error });
        }

        var newToken = _jwtService.GenerateToken(updatedUser!);

        Response.Cookies.Append("authToken", newToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = false, //for development
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddHours(1)
        });

        return Ok(new 
        { 
            message = "Email updated successfully",
            user = new 
            {
                id = updatedUser!.Id,
                username = updatedUser.Username,
                email = updatedUser.Email
            },
            token = newToken
        });
    }

    // Update password
    [Authorize]
    [HttpPut("password")]
    public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (userId == null)
        {
            return Unauthorized(new { message = "Unauthorized" });
        }

        var (success, error) = await _userService.UpdatePasswordAsync(
            int.Parse(userId), 
            request.CurrentPassword, 
            request.NewPassword
        );

        if (error != null)
        {
            return BadRequest(new { message = error });
        }

        return Ok(new { message = "Password updated successfully" });
    }
}