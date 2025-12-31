using Microsoft.EntityFrameworkCore;
using Pxl8.ControlApi.Data;
using Pxl8.ControlApi.Middleware;
using Pxl8.ControlApi.Security;
using Pxl8.ControlApi.Services.Budget;
using Pxl8.ControlApi.Services.Policy;
using Pxl8.ControlApi.Services.Usage;

var builder = WebApplication.CreateBuilder(args);

// Database
var connectionString = builder.Configuration.GetConnectionString("ControlDb")
    ?? "Host=localhost;Database=pxl8_control;Username=pxl8_control;Password=dev_password";

builder.Services.AddDbContext<ControlDbContext>(options =>
    options.UseNpgsql(connectionString));

// Security
builder.Services.AddSingleton<InterPlaneHmacService>();

// Services
builder.Services.AddScoped<IBudgetAllocatorService, BudgetAllocatorService>();
builder.Services.AddScoped<IPolicySnapshotPublisher, PolicySnapshotPublisher>();
builder.Services.AddScoped<IUsageReportProcessor, UsageReportProcessor>();

// Controllers
builder.Services.AddControllers();

// OpenAPI
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// HMAC authentication middleware (must be before MapControllers)
app.UseMiddleware<HmacAuthenticationMiddleware>();

app.MapControllers();

app.Run();
