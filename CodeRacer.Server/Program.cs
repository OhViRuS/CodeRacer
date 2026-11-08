using CodeRacer.Server.Data;
using CodeRacer.Server.Interfaces;
using CodeRacer.Server.Repositories;
using CodeRacer.Server.Services;
using CodeRacer.Server.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddDbContext<CodeRacerDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("CodeRacer")));

// Register repositories (scoped: new instance per HTTP request)
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRaceResultRepository, RaceResultRepository>();
builder.Services.AddScoped<ISnippetRecordRepository, SnippetRecordRepository>();

// Register services (scoped: new instance per HTTP request)
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IUserStatisticsService, UserStatisticsService>();
builder.Services.AddScoped<IRaceResultService, RaceResultService>();

// The system clock; tests replace it with a fixed point in time.
builder.Services.AddSingleton(TimeProvider.System);

// JWT settings: bound once, validated at startup, used both to issue and to validate tokens.
builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((bearerOptions, jwtOptions) =>
        bearerOptions.TokenValidationParameters = jwtOptions.Value.CreateValidationParameters());

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
