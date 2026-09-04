using EmployeeManagementAPI.Data;
using EmployeeManagementAPI.Services;
using EmployeeManagementAPI.Exceptions;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ==========================================================
// CONTROLLERS
// ==========================================================

builder.Services.AddControllers();

// ==========================================================
// GLOBAL EXCEPTION HANDLING
// ==========================================================

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// ==========================================================
// SERVICES
// ==========================================================

builder.Services.AddScoped<IEmployeeService, EmployeeService>();

builder.Services.AddScoped<IPasswordService, PasswordService>();

builder.Services.AddScoped<IAuthService, AuthService>();

// ==========================================================
// DATABASE
// ==========================================================

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString(
            "DefaultConnection"
        )
    ));

// ==========================================================
// JWT SETTINGS
// ==========================================================

var jwtSettings =
    builder.Configuration.GetSection("Jwt");

var jwtKey =
    jwtSettings["Key"];

if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException(
        "JWT Key is not configured.");
}

// ==========================================================
// JWT AUTHENTICATION
// ==========================================================

builder.Services
    .AddAuthentication(
        JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,

                ValidateAudience = true,

                ValidateLifetime = true,

                ValidateIssuerSigningKey = true,

                ValidIssuer =
                    jwtSettings["Issuer"],

                ValidAudience =
                    jwtSettings["Audience"],

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtKey)
                    ),

                ClockSkew =
                    TimeSpan.Zero
            };
    });

// ==========================================================
// AUTHORIZATION
// ==========================================================

builder.Services.AddAuthorization();

// ==========================================================
// SWAGGER
// IMPORTANT: MUST BE BEFORE builder.Build()
// ==========================================================

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Type =
                SecuritySchemeType.Http,

            Scheme =
                "bearer",

            BearerFormat =
                "JWT",

            Description =
                "Enter your JWT token."
        });

    options.AddSecurityRequirement(
        document =>
            new OpenApiSecurityRequirement
            {
                [
                    new OpenApiSecuritySchemeReference(
                        "Bearer",
                        document)
                ] = []
            });
});

// ==========================================================
// BUILD APPLICATION
// ==========================================================

var app = builder.Build();

// ==========================================================
// GLOBAL EXCEPTION HANDLER
// ==========================================================

app.UseExceptionHandler();

// ==========================================================
// SEED DEFAULT ADMIN USER
// ==========================================================

using (var scope = app.Services.CreateScope())
{
    var context =
        scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

    var passwordService =
        scope.ServiceProvider
            .GetRequiredService<IPasswordService>();

    var configuration =
        scope.ServiceProvider
            .GetRequiredService<IConfiguration>();

    var adminUsername =
        configuration["Admin:Username"]
        ?? "admin";

    var adminPassword =
        configuration["Admin:Password"];

    if (string.IsNullOrWhiteSpace(adminPassword))
    {
        throw new InvalidOperationException(
            "Admin password is not configured.");
    }

    var adminExists =
        await context.Users
            .AnyAsync(
                u => u.Username == adminUsername);

    if (!adminExists)
    {
        var adminUser =
            new EmployeeManagementAPI.Models.User
            {
                Username =
                    adminUsername,

                PasswordHash =
                    passwordService.HashPassword(
                        adminPassword),

                Role =
                    "Admin"
            };

        context.Users.Add(adminUser);

        await context.SaveChangesAsync();
    }
}

// ==========================================================
// SWAGGER UI
// ==========================================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI();
}

// ==========================================================
// HTTPS
// ==========================================================

app.UseHttpsRedirection();

// ==========================================================
// AUTHENTICATION
// MUST COME BEFORE AUTHORIZATION
// ==========================================================

app.UseAuthentication();

app.UseAuthorization();

// ==========================================================
// CONTROLLERS
// ==========================================================

app.MapControllers();

// ==========================================================
// RUN
// ==========================================================

app.Run();