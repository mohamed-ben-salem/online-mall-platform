using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineMall.Application.Auth.DTOs;
using OnlineMall.Infrastructure.Identity;
using OnlineMall.Shared.Responses;
using System.Security.Claims;

namespace OnlineMall.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        AuthService authService,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    // ─────────────────────────────────────
    // POST api/auth/register
    // ─────────────────────────────────────
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResponse>>>
        Register([FromBody] RegisterRequest request)
    {
        _logger.LogInformation(
            "Register attempt for email: {Email}", request.Email);

        var (success, message, data) =
            await _authService.RegisterAsync(request);

        if (!success)
        {
            _logger.LogWarning(
                "Registration failed for {Email}: {Message}",
                request.Email, message);

            return BadRequest(
                ApiResponse<AuthResponse>.Fail(message));
        }

        _logger.LogInformation(
            "User registered successfully: {Email}", request.Email);

        return Ok(
            ApiResponse<AuthResponse>.Ok(data!, message));
    }

    // ─────────────────────────────────────
    // POST api/auth/login
    // ─────────────────────────────────────
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResponse>>>
        Login([FromBody] LoginRequest request)
    {
        _logger.LogInformation(
            "Login attempt for email: {Email}", request.Email);

        var (success, message, data) =
            await _authService.LoginAsync(request);

        if (!success)
        {
            _logger.LogWarning(
                "Login failed for {Email}: {Message}",
                request.Email, message);

            return Unauthorized(
                ApiResponse<AuthResponse>.Fail(message));
        }

        _logger.LogInformation(
            "User logged in successfully: {Email}", request.Email);

        return Ok(
            ApiResponse<AuthResponse>.Ok(data!, message));
    }

    // ─────────────────────────────────────
    // POST api/auth/refresh
    // ─────────────────────────────────────
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResponse>>>
        RefreshToken([FromBody] string refreshToken)
    {
        var (success, message, data) =
            await _authService.RefreshTokenAsync(refreshToken);

        if (!success)
            return Unauthorized(
                ApiResponse<AuthResponse>.Fail(message));

        return Ok(
            ApiResponse<AuthResponse>.Ok(data!, message));
    }

    // ─────────────────────────────────────
    // POST api/auth/logout
    // ─────────────────────────────────────
    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<bool>>>
        Logout()
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
            return Unauthorized(
                ApiResponse<bool>.Fail("User not found"));

        var (success, message) =
            await _authService.LogoutAsync(userId);

        _logger.LogInformation(
            "User logged out: {UserId}", userId);

        return Ok(ApiResponse<bool>.Ok(success, message));
    }

    // ─────────────────────────────────────
    // GET api/auth/me
    // ─────────────────────────────────────
    [HttpGet("me")]
    [Authorize]
    public ActionResult<ApiResponse<UserDto>> GetCurrentUser()
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);
        var email = User.FindFirstValue(
            ClaimTypes.Email);
        var role = User.FindFirstValue(
            ClaimTypes.Role);
        var firstName = User.FindFirstValue("firstName");
        var lastName = User.FindFirstValue("lastName");

        if (string.IsNullOrEmpty(userId))
            return Unauthorized(
                ApiResponse<UserDto>.Fail("User not found"));

        var userDto = new UserDto
        {
            Id = Guid.Parse(userId),
            Email = email ?? string.Empty,
            Role = role ?? string.Empty,
            FirstName = firstName ?? string.Empty,
            LastName = lastName ?? string.Empty
        };

        return Ok(ApiResponse<UserDto>.Ok(userDto));
    }
}