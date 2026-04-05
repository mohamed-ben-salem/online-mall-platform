using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using OnlineMall.Domain.Entities;
using OnlineMall.Infrastructure.Identity;

namespace OnlineMall.Infrastructure.Identity;

public class JwtTokenGenerator
{
    private readonly IConfiguration _configuration;

    public JwtTokenGenerator(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    // ─────────────────────────────────────
    // Generate JWT Access Token
    // ─────────────────────────────────────
    public string GenerateAccessToken(AppUser user)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"]!;
        var issuer = jwtSettings["Issuer"]!;
        var audience = jwtSettings["Audience"]!;
        var expiryMinutes = int.Parse(jwtSettings["ExpiryMinutes"]!);

        // Use EXACT same key creation as validator
        var keyBytes = Encoding.UTF8.GetBytes(secretKey);
        // NOSONAR - Key is loaded from configuration, not hardcoded
        var key = new SymmetricSecurityKey(keyBytes);
        var credentials = new SigningCredentials(
            key, SecurityAlgorithms.HmacSha256);


        // Claims = info stored inside the token
        var claims = new List<Claim>
        {
            new Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email, user.Email!),
            new Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("firstName", user.FirstName),
            new Claim("lastName", user.LastName),
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(expiryMinutes),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = credentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    // ─────────────────────────────────────
    // Generate Refresh Token
    // ─────────────────────────────────────
    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    // ─────────────────────────────────────
    // Get Token Expiry
    // ─────────────────────────────────────
    public DateTime GetAccessTokenExpiry()
    {
        var expiryMinutes = int.Parse(
            _configuration["JwtSettings:ExpiryMinutes"]!);
        return DateTime.UtcNow.AddMinutes(expiryMinutes);
    }

    public DateTime GetRefreshTokenExpiry()
    {
        var expiryDays = int.Parse(
            _configuration["JwtSettings:RefreshTokenExpiryDays"]!);
        return DateTime.UtcNow.AddDays(expiryDays);
    }
}