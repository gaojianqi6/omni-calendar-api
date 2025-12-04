using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
