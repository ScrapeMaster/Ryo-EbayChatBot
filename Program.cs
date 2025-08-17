using Serilog;
using EbayChatBot.API.Data; // Update this to your actual namespace
using EbayChatBot.API.Services;
using Microsoft.EntityFrameworkCore;
using EbayChatBot.API.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container

// Add DbContext (using SQL Server)
builder.Services.AddDbContext<EbayChatDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("Logs/log.txt", rollingInterval: RollingInterval.Day)
    .Enrich.FromLogContext()
    .CreateLogger();

// Add Controllers
builder.Services.AddControllers();
builder.Host.UseSerilog();

// Enable Swagger for API testing
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure CORS (adjust as needed for frontend integration)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy.AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod());
});

builder.Services.AddScoped<AutomatedMessageService>();
builder.Services.AddHttpClient<EbayOAuthService>();
builder.Services.AddHttpClient<EbayOrderService>();
builder.Services.AddHttpClient<EbayMessageService>();
builder.Services.AddHttpClient<EbayItemService>();
builder.Services.AddSignalR();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.UseHttpsRedirection();

// Enable CORS
app.UseCors("AllowAll");
app.UseRouting();
app.UseAuthorization();

// Map endpoints to controllers
app.MapControllers();

app.UseEndpoints(endpoints =>
{
    _ = endpoints.MapHub<ChatHub>("/chatHub");
    _ = endpoints.MapControllers();
});

app.Run();
