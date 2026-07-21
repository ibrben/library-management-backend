using System.Text;
using System.Text.Json.Serialization;
using LibraryManagement.Api.Authentication;
using LibraryManagement.Api.Health;
using LibraryManagement.Api.Middleware;
using LibraryManagement.Business;
using LibraryManagement.Business.Authentication;
using LibraryManagement.DataAccess;
using LibraryManagement.DataAccess.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required.");
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("Jwt configuration is required.");
jwtOptions.Validate();

var corsOriginsValue = builder.Configuration["Cors:AllowedOrigins"];
var corsOrigins = !string.IsNullOrWhiteSpace(corsOriginsValue)
    ? corsOriginsValue.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    : builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
corsOrigins = corsOrigins
    .Select(origin => origin == "*" ? origin : origin.TrimEnd('/'))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();
foreach (var origin in corsOrigins.Where(origin => origin != "*"))
{
    if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri) ||
        (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
        uri.AbsolutePath != "/" ||
        !string.IsNullOrEmpty(uri.Query) ||
        !string.IsNullOrEmpty(uri.Fragment))
    {
        throw new InvalidOperationException(
            $"Cors:AllowedOrigins contains invalid origin '{origin}'. Use an HTTP(S) origin without a path, query, or fragment.");
    }
}

if (corsOrigins.Contains("*") && corsOrigins.Length > 1)
{
    throw new InvalidOperationException("Cors:AllowedOrigins cannot combine '*' with explicit origins.");
}

builder.Services.AddSingleton(jwtOptions);
builder.Services.AddDbContext<LibraryDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddBusiness();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<AdministratorSeeder>();
builder.Services.AddScoped<DevelopmentDataSeeder>();
builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("postgresql");
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
{
    if (corsOrigins.Contains("*"))
    {
        policy.AllowAnyOrigin();
    }
    else if (corsOrigins.Length > 0)
    {
        policy.WithOrigins(corsOrigins);
    }

    policy.AllowAnyHeader().AllowAnyMethod();
}));
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
    .ConfigureApiBehaviorOptions(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .ToDictionary(
                entry => entry.Key,
                entry => entry.Value!.Errors.Select(error => error.ErrorMessage).ToArray());
        return new BadRequestObjectResult(new
        {
            success = false,
            message = "Request validation failed.",
            errors
        });
    };
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Library Management API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Enter: Bearer {your JWT}",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
        }] = []
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    success = false,
                    message = "Authentication is required.",
                    errors = new Dictionary<string, string[]>()
                });
            },
            OnForbidden = async context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    success = false,
                    message = "You do not have permission to perform this action.",
                    errors = new Dictionary<string, string[]>()
                });
            }
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("InventoryManagement", policy =>
        policy.RequireRole(UserRole.Administrator.ToString(), UserRole.Librarian.ToString()));
});

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();
if (builder.Configuration.GetValue("Http:UseHttpsRedirection", false))
{
    app.UseHttpsRedirection();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health");
app.MapControllers();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
    if (builder.Configuration.GetValue("Database:ApplyMigrationsOnStartup", false))
    {
        await dbContext.Database.MigrateAsync();
    }

    await scope.ServiceProvider.GetRequiredService<AdministratorSeeder>().SeedAsync();
    await scope.ServiceProvider.GetRequiredService<DevelopmentDataSeeder>().SeedAsync();
}

await app.RunAsync();

public partial class Program;
