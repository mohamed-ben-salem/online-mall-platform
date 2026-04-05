using System.Diagnostics.Contracts;
using Microsoft.EntityFrameworkCore;
using OnlineMall.Domain.Entities;
using OnlineMall.Domain.Interfaces;
using OnlineMall.Infrastructure.Persistence;

namespace OnlineMall.Infrastructure.Identity;

public class AuthRepository : IAuthRepository
{
    private readonly AppDbContext _context;

    public AuthRepository(AppDbContext context)
    {
        _context = context;
    }

    
    // Get Refresh Token
    public async Task<RefreshToken?> GetRefreshTokenAsync(string token)
    {
        return await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == token);
    }

    // Add Refresh Token
    public async Task AddRefreshTokenAsync(RefreshToken refreshToken)
    {
        await _context.RefreshTokens.AddAsync(refreshToken);
        await _context.SaveChangesAsync();
    }

    // Revoke Single Token
    public async Task RevokeRefreshTokenAsync(string token)
    {
        var refreshToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == token);

        if (refreshToken != null)
        {
            refreshToken.IsUsed = true;
            refreshToken.IsRevoked = true;
            await _context.SaveChangesAsync();
        }
    }

    // Revoke All User Tokens
    public async Task RevokeAllUserRefreshTokensAsync(string userId)
    {
        var tokens = await _context.RefreshTokens
            .Where(t => t.UserId == userId
                     && !t.IsRevoked)
            .ToListAsync();

        foreach (var token in tokens)
        {
            token.IsRevoked = true;
            token.IsUsed = true;
        }

        await _context.SaveChangesAsync();
    }
}