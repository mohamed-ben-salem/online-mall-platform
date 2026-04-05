using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Win32;
using OnlineMall.Domain.Interfaces;
using OnlineMall.Infrastructure.Identity;
using OnlineMall.Infrastructure.Persistence;
using StackExchange.Redis;
using System.Runtime.InteropServices;
using System.Text;
using static System.Net.WebRequestMethods;

namespace OnlineMall.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ─────────────────────────────────────
        // Database
        // ─────────────────────────────────────
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection")));

        // ─────────────────────────────────────
        // Identity
        // ─────────────────────────────────────
        services.AddIdentity<AppUser, IdentityRole>(options =>
        {
            // Password rules
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredLength = 8;

            // Email rules
            options.User.RequireUniqueEmail = true;

            // Lockout rules
            options.Lockout.DefaultLockoutTimeSpan =
                TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5;
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        // ─────────────────────────────────────
        // JWT Authentication
        // ─────────────────────────────────────
        var jwtSettings = configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"]!;
        var keyBytes = Encoding.UTF8.GetBytes(secretKey);
        var signingKey = new SymmetricSecurityKey(keyBytes);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme =
                JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme =
                JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme =
                JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters =
                new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = signingKey,
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidateAudience = true,
                    ValidAudience = jwtSettings["Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    // Add these:
                    RequireSignedTokens = true,
                    RequireExpirationTime = true,
                };
        });

        // ─────────────────────────────────────
        // Services & Repositories
        // ─────────────────────────────────────
        services.AddScoped<IAuthRepository, AuthRepository>();
        services.AddScoped<JwtTokenGenerator>();
        services.AddScoped<AuthService>();

        return services;
    }
}