using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Pxl8.ControlApi.Data;
using Pxl8.ControlApi.Middleware;
using Pxl8.ControlApi.Security;
using Pxl8.ControlApi.Services.Auth;
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

// JWT Authentication
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret configuration is required");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "pxl8-control-api";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "pxl8-control-api";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew = TimeSpan.FromMinutes(5)
        };
    });

builder.Services.AddAuthorization();

// Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IBudgetAllocatorService, BudgetAllocatorService>();
builder.Services.AddScoped<IPolicySnapshotPublisher, PolicySnapshotPublisher>();
builder.Services.AddScoped<IUsageReportProcessor, UsageReportProcessor>();

// Controllers
builder.Services.AddControllers();

// OpenAPI
builder.Services.AddOpenApi();

var app = builder.Build();

// Auto-run EF Core migrations on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ControlDbContext>();
    await dbContext.Database.MigrateAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// HMAC authentication middleware (must be before MapControllers)
app.UseMiddleware<HmacAuthenticationMiddleware>();

// JWT authentication/authorization (after HMAC, before MapControllers)
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
