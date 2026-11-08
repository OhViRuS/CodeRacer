using System.Text;
using CodeRacer.Server.Data;
using CodeRacer.Server.Interfaces;
using CodeRacer.Server.Repositories;
using CodeRacer.Server.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddDbContext<CodeRacerDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("CodeRacer")));

// Register repositories (scoped: new instance per HTTP request)
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRaceResultRepository, RaceResultRepository>();

// Register services (scoped: new instance per HTTP request)
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRaceResultService, RaceResultService>();

var jwtSecretKey = builder.Configuration["Jwt:SecretKey"];
if (string.IsNullOrWhiteSpace(jwtSecretKey))
{
    throw new InvalidOperationException("JWT SecretKey is not configured (Jwt:SecretKey).");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "CodeRacer",
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "CodeRacerApp",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddSingleton<FileSnippetProvider>();
builder.Services.AddSingleton<ICodeSnippetRepository>(sp => sp.GetRequiredService<FileSnippetProvider>());
builder.Services.AddSingleton<ICodeSnippetProvider>(sp => sp.GetRequiredService<FileSnippetProvider>());
builder.Services.AddSingleton<ISourceFileImporter, SourceFileImporter>();
builder.Services.AddSingleton<ISourceSnippetExtractor, SourceSnippetExtractor>();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var provider = scope.ServiceProvider.GetRequiredService<ICodeSnippetProvider>();
    provider.GetSnippets();
}

app.UseDefaultFiles();
app.MapStaticAssets();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "CodeRacer API v1");
    });
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("/index.html");

app.Run();
