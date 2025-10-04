using Serilog;
using EbayChatBot.API.Data;
using EbayChatBot.API.Services;
using Microsoft.EntityFrameworkCore;
using EbayChatBot.API.Hubs;
using Microsoft.AspNetCore.Identity;
using EbayChatBot.API.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add DbContext
builder.Services.AddDbContext<EbayChatDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Identity
builder.Services.AddIdentity<User, IdentityRole<int>>()
    .AddEntityFrameworkStores<EbayChatDbContext>()
    .AddDefaultTokenProviders();

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("Logs/log.txt", rollingInterval: RollingInterval.Day)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

// JWT Configuration
var jwtSettings = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,

        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!))
    };
});

// Add Controllers
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// Register services
builder.Services.AddScoped<AutomatedMessageService>();
builder.Services.AddHttpClient<EbayOAuthService>();
builder.Services.AddHttpClient<EbayOrderService>();
builder.Services.AddHttpClient<EbayMessageService>();
builder.Services.AddHttpClient<EbayItemService>();

// Register Token Service for JWT generation
builder.Services.AddScoped<TokenService>();

// SignalR
builder.Services.AddSignalR();

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Enable CORS
app.UseCors("AllowAll");

// Order is IMPORTANT: Authentication before Authorization
app.UseAuthentication();
app.UseAuthorization();

// Map Controllers & Hubs
app.MapControllers();
app.MapHub<ChatHub>("/chatHub");

app.Run();
