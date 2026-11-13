using ECommerce.Application.Interfaces;
using ECommerce.Application.Interfaces.Services;
using ECommerce.Application.Services;
using ECommerce.Infrastructure.Authentication;
using ECommerce.Infrastructure.Persistence;
using ECommerce.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults("user-service");
builder.Services.AddUserPersistence(builder.Configuration);

builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserProfileService, UserProfileService>();

var app = builder.Build();

var passwordHasher = app.Services.GetRequiredService<IPasswordHasher>();
await app.Services.InitializeUserDatabaseAsync(app.Configuration, passwordHasher, app.Logger);

app.UseServiceDefaults();

app.Run();

public sealed class UserServiceMarker;
