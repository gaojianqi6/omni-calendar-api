using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OmniCalendar.Api.Application.Auth;
using OmniCalendar.Api.Application.Dashboard;
using OmniCalendar.Api.Application.Holidays;
using OmniCalendar.Api.Application.Tasks;
using OmniCalendar.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// EF Core + Npgsql (Supabase PostgreSQL)
builder.Services.AddDbContext<OmniCalendarDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("Database"));
});

// JWT Authentication (Clerk)
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

var clerkSection = builder.Configuration.GetSection("Clerk");
var clerkIssuer = clerkSection["Issuer"];
var clerkAudience = clerkSection["Audience"];

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = clerkIssuer;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = clerkIssuer,
            ValidateAudience = true,
            ValidAudience = clerkAudience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true
            // For Clerk, signing keys are resolved via the Authority metadata
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IHolidayService, HolidayService>();
builder.Services.AddHttpClient("calendarific");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

if (app.Environment.IsDevelopment())
{
    // Serves OpenAPI document at /openapi/v1.json
    app.MapOpenApi();
}

app.Run();
