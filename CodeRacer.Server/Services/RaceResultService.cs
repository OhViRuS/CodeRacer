using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CodeRacer.Server.Data;
using CodeRacer.Server.Interfaces;
using CodeRacer.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace CodeRacer.Server.Services;

public class RaceResultService : IRaceResultService
{
    public static readonly Guid DefaultGuestUserId = new("11111111-1111-1111-1111-111111111111");

    private readonly CodeRacerDbContext _context;
    private readonly ICodeSnippetProvider _snippetProvider;

    public RaceResultService(CodeRacerDbContext context, ICodeSnippetProvider snippetProvider)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _snippetProvider = snippetProvider ?? throw new ArgumentNullException(nameof(snippetProvider));
    }

    public async Task<RaceResultResponse> SaveResultAsync(CreateRaceResultRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        Guid effectiveUserId = await EnsureUserExistsAsync(request.UserId, cancellationToken);
        Guid effectiveSnippetId = await EnsureSnippetExistsAsync(request.CodeSnippetId, cancellationToken);

        var entity = new RaceResult
        {
            Id = Guid.NewGuid(),
            UserId = effectiveUserId,
            CodeSnippetId = effectiveSnippetId,
            CompletionTime = request.CompletionTime,
            TypingSpeed = request.TypingSpeed,
            Accuracy = request.Accuracy,
            Mistakes = request.Mistakes,
            CreatedAt = DateTime.UtcNow
        };

        await _context.RaceResults.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return RaceResultResponse.FromEntity(entity);
    }

    public async Task<IReadOnlyList<RaceResultResponse>> GetHistoryAsync(Guid? userId = null, int limit = 20, CancellationToken cancellationToken = default)
    {
        int clampedLimit = Math.Clamp(limit, 1, 100);

        IQueryable<RaceResult> query = _context.RaceResults.AsNoTracking();

        if (userId.HasValue && userId.Value != Guid.Empty)
        {
            query = query.Where(r => r.UserId == userId.Value);
        }

        List<RaceResult> results = await query
            .OrderByDescending(r => r.CreatedAt)
            .Take(clampedLimit)
            .ToListAsync(cancellationToken);

        return results.Select(RaceResultResponse.FromEntity).ToList();
    }

    public async Task<UserStatsResponse?> GetUserStatsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        List<RaceResult> userResults = await _context.RaceResults.AsNoTracking()
            .Where(r => r.UserId == userId)
            .ToListAsync(cancellationToken);

        if (userResults.Count == 0)
        {
            return new UserStatsResponse
            {
                UserId = userId,
                TotalRaces = 0,
                BestTypingSpeed = 0,
                AverageTypingSpeed = 0,
                AverageAccuracy = 100,
                TotalMistakes = 0,
                TotalTimeSpent = TimeSpan.Zero
            };
        }

        long totalTicks = userResults.Sum(r => r.CompletionTime.Ticks);

        return new UserStatsResponse
        {
            UserId = userId,
            TotalRaces = userResults.Count,
            BestTypingSpeed = Math.Round(userResults.Max(r => r.TypingSpeed), 1),
            AverageTypingSpeed = Math.Round(userResults.Average(r => r.TypingSpeed), 1),
            AverageAccuracy = Math.Round(userResults.Average(r => r.Accuracy), 1),
            TotalMistakes = userResults.Sum(r => r.Mistakes),
            TotalTimeSpent = TimeSpan.FromTicks(totalTicks)
        };
    }

    private async Task<Guid> EnsureUserExistsAsync(Guid? userId, CancellationToken cancellationToken)
    {
        if (userId.HasValue && userId.Value != Guid.Empty)
        {
            bool exists = await _context.Users.AnyAsync(u => u.Id == userId.Value, cancellationToken);
            if (exists)
            {
                return userId.Value;
            }
        }

        User? guestUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == DefaultGuestUserId, cancellationToken);
        if (guestUser == null)
        {
            guestUser = new User
            {
                Id = DefaultGuestUserId,
                UserName = "Guest",
                Email = "guest@coderacer.local",
                PasswordHash = "GUEST_ACCOUNT",
                CreatedAt = DateTime.UtcNow
            };

            await _context.Users.AddAsync(guestUser, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return DefaultGuestUserId;
    }

    private async Task<Guid> EnsureSnippetExistsAsync(Guid? snippetId, CancellationToken cancellationToken)
    {
        if (snippetId.HasValue && snippetId.Value != Guid.Empty)
        {
            bool existsInDb = await _context.CodeSnippets.AnyAsync(s => s.Id == snippetId.Value, cancellationToken);
            if (existsInDb)
            {
                return snippetId.Value;
            }

            CodeSnippet? fileSnippet = _snippetProvider.GetById(snippetId.Value);
            if (fileSnippet != null)
            {
                var newSnippet = new CodeSnippet
                {
                    Id = fileSnippet.Id,
                    CodeText = fileSnippet.CodeText,
                    Language = fileSnippet.Language,
                    Difficulty = fileSnippet.Difficulty,
                    ProgrammingConcept = fileSnippet.ProgrammingConcept
                };

                await _context.CodeSnippets.AddAsync(newSnippet, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                return newSnippet.Id;
            }
        }

        CodeSnippet? firstDbSnippet = await _context.CodeSnippets.FirstOrDefaultAsync(cancellationToken);
        if (firstDbSnippet != null)
        {
            return firstDbSnippet.Id;
        }

        IReadOnlyList<CodeSnippet> predefined = _snippetProvider.GetSnippets();
        CodeSnippet fallbackSource = predefined.Count > 0
            ? predefined[0]
            : new CodeSnippet
            {
                Id = Guid.NewGuid(),
                CodeText = "Console.WriteLine(\"CodeRacer\");",
                Language = ProgrammingLanguage.CSharp,
                Difficulty = Difficulty.Easy,
                ProgrammingConcept = "Basics"
            };

        var fallbackSnippet = new CodeSnippet
        {
            Id = fallbackSource.Id,
            CodeText = fallbackSource.CodeText,
            Language = fallbackSource.Language,
            Difficulty = fallbackSource.Difficulty,
            ProgrammingConcept = fallbackSource.ProgrammingConcept
        };

        await _context.CodeSnippets.AddAsync(fallbackSnippet, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return fallbackSnippet.Id;
    }
}
