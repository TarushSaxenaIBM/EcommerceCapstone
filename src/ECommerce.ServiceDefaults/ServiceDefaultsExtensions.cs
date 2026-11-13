using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using ECommerce.Application.Common;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

namespace ECommerce.ServiceDefaults;

public static class ServiceDefaultsExtensions
{
    /// <summary>
    /// Registers the HTTP concerns shared by every IBM ecommerce microservice:
    /// controllers, consistent validation errors, JWT authentication, Swagger,
    /// CORS, health checks and the standard middleware pipeline.
    /// </summary>
    public static IServiceCollection AddServiceDefaults(
        this WebApplicationBuilder builder,
        string serviceName)
    {
        var configuration = builder.Configuration;
        var isDevelopment = builder.Environment.IsDevelopment();
        var jwtSecret = ServiceSecrets.ResolveJwtSecret(configuration, isDevelopment);
        var internalApiKey = ServiceSecrets.ResolveInternalApiKey(configuration, isDevelopment);

        builder.Services.AddSingleton(new ServiceInfo(serviceName));
        builder.Services.AddSingleton(new InternalApiKey(internalApiKey));

        builder.Services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        builder.Services.PostConfigure<JwtSettings>(settings =>
        {
            settings.Secret = jwtSecret;
            settings.Issuer = string.IsNullOrWhiteSpace(settings.Issuer) ? "ECommerce" : settings.Issuer;
            settings.Audience = string.IsNullOrWhiteSpace(settings.Audience) ? "ECommerce.Client" : settings.Audience;
        });

        builder.Services.AddControllers()
            .AddJsonOptions(options =>
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        builder.Services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var problem = new ValidationProblemDetails(context.ModelState)
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Validation failed",
                    Detail = "One or more fields are invalid."
                };

                return new BadRequestObjectResult(problem);
            };
        });

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = $"IBM ecommerce {serviceName} API",
                Version = "v1",
                Description = "IBM ecommerce microservice. All money values are in Indian rupees (INR)."
            });

            foreach (var xmlFile in Directory.GetFiles(AppContext.BaseDirectory, "*.xml"))
            {
                try
                {
                    options.IncludeXmlComments(xmlFile, includeControllerXmlComments: true);
                }
                catch (Exception)
                {
                    // A malformed documentation file must not stop the service.
                }
            }

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "JWT access token returned by the User Service login endpoint."
            });

            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
            });
        });

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<Microsoft.Extensions.Options.IOptions<JwtSettings>>((options, jwt) =>
            {
                var settings = jwt.Value;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = settings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = settings.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Secret)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(2)
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];

                        if (!string.IsNullOrEmpty(accessToken) &&
                            context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    }
                };
            });

        builder.Services.AddAuthorization();

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("Frontend", policy =>
            {
                var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                    ?? new[]
                    {
                        "http://localhost:5173",
                        "http://127.0.0.1:5173",
                        "http://localhost:4173"
                    };

                policy.WithOrigins(origins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        builder.Services.AddHealthChecks();

        return builder.Services;
    }

    /// <summary>
    /// Adds the shared middleware pipeline, Swagger UI in development, health
    /// endpoint and controller mapping.
    /// </summary>
    public static WebApplication UseServiceDefaults(this WebApplication app)
    {
        app.UseMiddleware<ExceptionHandlingMiddleware>();

        var internalKey = app.Services.GetRequiredService<InternalApiKey>();
        app.UseMiddleware<InternalApiKeyMiddleware>(internalKey.Value);

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
                options.RoutePrefix = "swagger";
            });
        }

        app.UseCors("Frontend");
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapGet("/health", (ServiceInfo info) => Results.Ok(new
        {
            status = "Healthy",
            service = info.Name,
            timestamp = DateTimeOffset.UtcNow
        }));

        app.MapHealthChecks("/health/ready");
        app.MapControllers();

        return app;
    }
}

public sealed record ServiceInfo(string Name);

public sealed record InternalApiKey(string Value);
