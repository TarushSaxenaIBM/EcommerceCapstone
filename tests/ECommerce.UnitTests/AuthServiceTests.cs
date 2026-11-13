using ECommerce.Application.Common;
using ECommerce.Application.Exceptions;
using ECommerce.Application.Services;
using ECommerce.Infrastructure.Authentication;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ECommerce.UnitTests;

public class AuthServiceTests
{
    private static AuthService CreateService(TestDatabase db) =>
        new(
            db.Users,
            db.UserUnitOfWork,
            new PasswordHasher(),
            new JwtTokenService(Options.Create(new JwtSettings
            {
                Issuer = "tests",
                Audience = "tests",
                Secret = new string('s', 64),
                ExpiryMinutes = 30
            })),
            NullLogger<AuthService>.Instance);

    [Fact]
    public async Task Register_NormalizesEmailAndReturnsToken()
    {
        await using var db = await TestDatabase.CreateAsync();
        var service = CreateService(db);

        var result = await service.RegisterAsync(new()
        {
            FirstName = "  Ana ",
            LastName = " Lima ",
            Email = "  ANA@Example.COM ",
            Password = "Passw0rd!"
        });

        Assert.Equal("ana@example.com", result.User.Email);
        Assert.Equal("Ana", result.User.FirstName);
        Assert.NotEmpty(result.Token);
        Assert.True(result.ExpiresAt > DateTime.UtcNow.AddMinutes(25));
    }

    [Fact]
    public async Task Register_WithExistingEmail_ThrowsConflict()
    {
        await using var db = await TestDatabase.CreateAsync();
        var service = CreateService(db);
        var request = new ECommerce.Application.DTOs.Auth.RegisterRequest
        {
            FirstName = "Ana",
            LastName = "Lima",
            Email = "ana@example.com",
            Password = "Passw0rd!"
        };

        await service.RegisterAsync(request);

        await Assert.ThrowsAsync<ConflictException>(() => service.RegisterAsync(request));
    }

    [Fact]
    public async Task Login_WithWrongPassword_ThrowsUnauthorized()
    {
        await using var db = await TestDatabase.CreateAsync();
        var service = CreateService(db);

        await service.RegisterAsync(new()
        {
            FirstName = "Ana",
            LastName = "Lima",
            Email = "ana@example.com",
            Password = "Passw0rd!"
        });

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            service.LoginAsync(new() { Email = "ana@example.com", Password = "wrong" }));
    }

    [Fact]
    public async Task Login_WithCorrectPassword_ReturnsSameUser()
    {
        await using var db = await TestDatabase.CreateAsync();
        var service = CreateService(db);

        var registered = await service.RegisterAsync(new()
        {
            FirstName = "Ana",
            LastName = "Lima",
            Email = "ana@example.com",
            Password = "Passw0rd!"
        });

        var login = await service.LoginAsync(new() { Email = "ANA@example.com", Password = "Passw0rd!" });

        Assert.Equal(registered.User.Id, login.User.Id);
        Assert.NotEqual(registered.Token, login.Token);
    }
}
