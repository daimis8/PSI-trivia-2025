using backend.Services;
using backend.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy.WithOrigins("http://localhost:5173") // Vite's default port
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
});

// Build connection string from environment variables
var host = Environment.GetEnvironmentVariable("PGSQL_HOST") ?? "localhost";
var port = Environment.GetEnvironmentVariable("PGSQL_PORT") ?? "5432";
var database = Environment.GetEnvironmentVariable("PGSQL_DATABASE") ?? "psi_trivia";
var username = Environment.GetEnvironmentVariable("PGSQL_USERNAME") ?? "postgres";
var password = Environment.GetEnvironmentVariable("PGSQL_PASSWORD") ?? "postgres";

var connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password}";

// Configure DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString)
);

// Register services with scoped lifetime (appropriate for DbContext)
builder.Services.AddScoped<UserService>();
builder.Services.AddSingleton<JwtService>();
builder.Services.AddSingleton<PasswordService>();
builder.Services.AddScoped<QuizService>();
builder.Services.AddSingleton<GameService>();
builder.Services.AddSignalR();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)
            )
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (context.Request.Cookies.ContainsKey("authToken"))
                {
                    context.Token = context.Request.Cookies["authToken"];
                }
                return Task.CompletedTask;
            }
        };
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapHub<backend.Hubs.GameHub>("/hubs/game");

app.Run();