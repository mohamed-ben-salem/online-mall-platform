using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OnlineMall.Application.Auth.DTOs;
using OnlineMall.Domain.Entities;
using OnlineMall.Domain.Enums;
using OnlineMall.Domain.Interfaces;
using OnlineMall.Infrastructure.Persistence;

namespace OnlineMall.Infrastructure.Identity;

public class AuthService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly JwtTokenGenerator _jwtTokenGenerator;
    private readonly IAuthRepository _authRepository;
    private readonly AppDbContext _context;

    public AuthService(
        UserManager<AppUser> userManager,
        JwtTokenGenerator jwtTokenGenerator,
        IAuthRepository authRepository,
        AppDbContext context)
    {
        _userManager = userManager;
        _jwtTokenGenerator = jwtTokenGenerator;
        _authRepository = authRepository;
        _context = context;
    }

    // ─────────────────────────────────────
    // Register
    // ─────────────────────────────────────
    public async Task<(bool Success, string Message, AuthResponse? Data)>
        RegisterAsync(RegisterRequest request)
    {
        // Check if email already exists
        var existingUser = await _userManager
            .FindByEmailAsync(request.Email);

        if (existingUser != null)
            return (false, "Email already registered", null);

        // Check passwords match
        if (request.Password != request.ConfirmPassword)
            return (false, "Passwords do not match", null);

        // Create new user
        var user = new AppUser
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            UserName = request.Email,
            PhoneNumber = request.PhoneNumber,
            Role = UserRole.Buyer,
            CreatedAt = DateTime.UtcNow
        };

        // Save user with hashed password
        var result = await _userManager
            .CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            var errors = result.Errors
                .Select(e => e.Description)
                .ToList();
            return (false, "Registration failed", null);
        }

        // Add role to user
        await _userManager.AddToRoleAsync(user, UserRole.Buyer.ToString());

        // Generate tokens
        var authResponse = await GenerateAuthResponseAsync(user);

        return (true, "Registration successful", authResponse);
    }

    // ─────────────────────────────────────
    // Login
    // ─────────────────────────────────────
    public async Task<(bool Success, string Message, AuthResponse? Data)>
        LoginAsync(LoginRequest request)
    {
        // Find user by email
        var user = await _userManager
            .FindByEmailAsync(request.Email);

        if (user == null)
            return (false, "Invalid email or password", null);

        // Check if user is active
        if (!user.IsActive)
            return (false, "Account is deactivated", null);

        // Verify password
        var passwordValid = await _userManager
            .CheckPasswordAsync(user, request.Password);

        if (!passwordValid)
            return (false, "Invalid email or password", null);

        // Generate tokens
        var authResponse = await GenerateAuthResponseAsync(user);

        return (true, "Login successful", authResponse);
    }

    // ─────────────────────────────────────
    // Refresh Token
    // ─────────────────────────────────────
    public async Task<(bool Success, string Message, AuthResponse? Data)>
        RefreshTokenAsync(string refreshToken)
    {
        // Find refresh token in database
        var storedToken = await _authRepository
            .GetRefreshTokenAsync(refreshToken);

        if (storedToken == null)
            return (false, "Invalid refresh token", null);

        if (storedToken.IsRevoked)
            return (false, "Refresh token has been revoked", null);

        if (storedToken.IsUsed)
            return (false, "Refresh token has already been used", null);

        if (storedToken.ExpiresAt < DateTime.UtcNow)
            return (false, "Refresh token has expired", null);

        // Mark token as used
        await _authRepository.RevokeRefreshTokenAsync(refreshToken);

        // Find user
        var user = await _userManager
            .FindByIdAsync(storedToken.UserId);

        if (user == null)
            return (false, "User not found", null);

        // Generate new tokens
        var authResponse = await GenerateAuthResponseAsync(user);

        return (true, "Token refreshed successfully", authResponse);
    }

    // ─────────────────────────────────────
    // Logout
    // ─────────────────────────────────────
    public async Task<(bool Success, string Message)>
        LogoutAsync(string userId)
    {
        await _authRepository
            .RevokeAllUserRefreshTokensAsync(userId);

        return (true, "Logged out successfully");
    }

    // ─────────────────────────────────────
    // Private — Generate Auth Response
    // ─────────────────────────────────────
    private async Task<AuthResponse> GenerateAuthResponseAsync(AppUser user)
    {
        // Generate access token
        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user);

        // Generate refresh token
        var refreshTokenValue = _jwtTokenGenerator.GenerateRefreshToken();

        // Save refresh token to database
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = refreshTokenValue,
            UserId = user.Id,
            ExpiresAt = _jwtTokenGenerator.GetRefreshTokenExpiry(),
            CreatedAt = DateTime.UtcNow
        };

        await _authRepository.AddRefreshTokenAsync(refreshToken);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenValue,
            AccessTokenExpiry = _jwtTokenGenerator.GetAccessTokenExpiry(),
            RefreshTokenExpiry = refreshToken.ExpiresAt,
            User = new UserDto
            {
                Id = Guid.Parse(user.Id),
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email!,
                Role = user.Role.ToString()
            }
        };
    }
}