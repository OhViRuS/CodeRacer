using CodeRacer.Server.Data;
using CodeRacer.Server.Models;
using CodeRacer.Server.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CodeRacer.Server.Tests.Unit;

/// <summary>
/// Repository contract tests.
/// Ensures that repositories behave correctly according to their documented contracts.
/// Tests with real EF Core in-memory database.
/// </summary>
public class RepositoryContractTests
{
    private static CodeRacerDbContext CreateTestDbContext()
    {
        var options = new DbContextOptionsBuilder<CodeRacerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new CodeRacerDbContext(options);
    }

    #region SnippetRecordRepository Contract Tests

    private static CodeSnippet CreateSnippet() =>
        new() { Id = Guid.NewGuid(), CodeText = "print(1)", Language = ProgrammingLanguage.Python, ProgrammingConcept = "Output" };

    [Fact]
    public async Task SnippetRecordRepository_EnsureExistsAsync_InsertsCopyWithoutTrackingIt()
    {
        // Arrange
        var context = CreateTestDbContext();
        var repository = new SnippetRecordRepository(context);
        var snippet = CreateSnippet();

        // Act
        await repository.EnsureExistsAsync(snippet, CancellationToken.None);

        // Assert
        var stored = await context.CodeSnippets.AsNoTracking().SingleAsync();
        Assert.Equal(snippet.Id, stored.Id);
        Assert.Equal(snippet.CodeText, stored.CodeText);
        Assert.Equal(snippet.Language, stored.Language);
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task SnippetRecordRepository_EnsureExistsAsync_CalledTwice_InsertsOnce()
    {
        // Arrange
        var context = CreateTestDbContext();
        var repository = new SnippetRecordRepository(context);
        var snippet = CreateSnippet();

        // Act
        await repository.EnsureExistsAsync(snippet, CancellationToken.None);
        await repository.EnsureExistsAsync(snippet, CancellationToken.None);

        // Assert
        Assert.Equal(1, await context.CodeSnippets.CountAsync());
    }

    #endregion

    #region UserRepository Contract Tests

    [Fact]
    public async Task UserRepository_EnsureGuestUserAsync_CalledTwice_CreatesGuestOnce()
    {
        // Arrange
        var context = CreateTestDbContext();
        var repository = new UserRepository(context);

        // Act
        await repository.EnsureGuestUserAsync(CancellationToken.None);
        await repository.EnsureGuestUserAsync(CancellationToken.None);

        // Assert
        var guest = await context.Users.AsNoTracking().SingleAsync();
        Assert.Equal(GuestAccount.Id, guest.Id);
        Assert.Equal(GuestAccount.UserName, guest.UserName);
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task UserRepository_UpdateAsync_DetachedInstance_PersistsChanges()
    {
        // Arrange
        var context = CreateTestDbContext();
        var repository = new UserRepository(context);
        var user = await repository.AddAsync(
            new User { UserName = "before", Email = "before@example.com", PasswordHash = "hash" },
            CancellationToken.None);
        context.ChangeTracker.Clear();

        var updated = new User
        {
            Id = user.Id,
            UserName = "after",
            Email = "after@example.com",
            PasswordHash = "hash",
            CreatedAt = user.CreatedAt
        };

        // Act
        var result = await repository.UpdateAsync(updated, CancellationToken.None);

        // Assert
        Assert.True(result);
        var stored = await repository.GetByIdAsync(user.Id, CancellationToken.None);
        Assert.Equal("after", stored!.UserName);
    }

    [Fact]
    public async Task UserRepository_UpdateAsync_MissingEntity_ReturnsFalse()
    {
        var repository = new UserRepository(CreateTestDbContext());

        var result = await repository.UpdateAsync(
            new User { UserName = "ghost", Email = "ghost@example.com", PasswordHash = "hash" },
            CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task UserRepository_AddAsync_AssignsIdAndReturnsUser()
    {
        // Arrange
        var context = CreateTestDbContext();
        var repository = new UserRepository(context);
        var user = new User { UserName = "testuser", Email = "test@example.com", PasswordHash = "hash" };

        // Act
        var result = await repository.AddAsync(user, CancellationToken.None);

        // Assert
        Assert.NotEqual(Guid.Empty, result.Id);
        var stored = await repository.GetByIdAsync(result.Id, CancellationToken.None);
        Assert.NotNull(stored);
        Assert.Equal(result.Id, stored.Id);
    }

    [Fact]
    public async Task UserRepository_FindByUserNameAsync_IsCaseInsensitive()
    {
        // Arrange
        var context = CreateTestDbContext();
        var repository = new UserRepository(context);
        var user = new User { UserName = "TestUser", Email = "test@example.com", PasswordHash = "hash" };
        await repository.AddAsync(user, CancellationToken.None);

        // Act
        var found1 = await repository.FindByUserNameAsync("testuser", CancellationToken.None);
        var found2 = await repository.FindByUserNameAsync("TESTUSER", CancellationToken.None);
        var found3 = await repository.FindByUserNameAsync("TestUser", CancellationToken.None);

        // Assert
        Assert.NotNull(found1);
        Assert.NotNull(found2);
        Assert.NotNull(found3);
        Assert.Equal(user.Id, found1.Id);
    }

    [Fact]
    public async Task UserRepository_FindByEmailAsync_IsCaseInsensitive()
    {
        // Arrange
        var context = CreateTestDbContext();
        var repository = new UserRepository(context);
        var user = new User { UserName = "testuser", Email = "Test@Example.COM", PasswordHash = "hash" };
        await repository.AddAsync(user, CancellationToken.None);

        // Act
        var found1 = await repository.FindByEmailAsync("test@example.com", CancellationToken.None);
        var found2 = await repository.FindByEmailAsync("TEST@EXAMPLE.COM", CancellationToken.None);

        // Assert
        Assert.NotNull(found1);
        Assert.NotNull(found2);
        Assert.Equal(user.Id, found1.Id);
    }

    [Fact]
    public async Task UserRepository_UserNameExistsAsync_ReturnsTrueOnlyWhenExists()
    {
        // Arrange
        var context = CreateTestDbContext();
        var repository = new UserRepository(context);
        var user = new User { UserName = "existinguser", Email = "test@example.com", PasswordHash = "hash" };
        await repository.AddAsync(user, CancellationToken.None);

        // Act
        var exists = await repository.UserNameExistsAsync("existinguser", CancellationToken.None);
        var notExists = await repository.UserNameExistsAsync("nonexistent", CancellationToken.None);

        // Assert
        Assert.True(exists);
        Assert.False(notExists);
    }

    [Fact]
    public async Task UserRepository_EmailExistsAsync_ReturnsTrueOnlyWhenExists()
    {
        // Arrange
        var context = CreateTestDbContext();
        var repository = new UserRepository(context);
        var user = new User { UserName = "testuser", Email = "existing@example.com", PasswordHash = "hash" };
        await repository.AddAsync(user, CancellationToken.None);

        // Act
        var exists = await repository.EmailExistsAsync("existing@example.com", CancellationToken.None);
        var notExists = await repository.EmailExistsAsync("nonexistent@example.com", CancellationToken.None);

        // Assert
        Assert.True(exists);
        Assert.False(notExists);
    }

    #endregion

    #region RaceResultRepository Contract Tests

    [Fact]
    public async Task RaceResultRepository_GetByUserIdAsync_ReturnsOnlyThatUsersResults()
    {
        // Arrange
        var context = CreateTestDbContext();
        var repository = new RaceResultRepository(context);
        var userId = Guid.NewGuid();
        var result1 = new RaceResult(TimeSpan.FromSeconds(30), 100, 95) { UserId = userId, CodeSnippetId = Guid.NewGuid() };
        var result2 = new RaceResult(TimeSpan.FromSeconds(25), 110, 96) { UserId = userId, CodeSnippetId = Guid.NewGuid() };
        var result3 = new RaceResult(TimeSpan.FromSeconds(40), 90, 94) { UserId = Guid.NewGuid(), CodeSnippetId = Guid.NewGuid() };

        // Act
        await repository.AddAsync(result1, CancellationToken.None);
        await repository.AddAsync(result2, CancellationToken.None);
        await repository.AddAsync(result3, CancellationToken.None);

        var userResults = await repository.GetByUserIdAsync(userId, CancellationToken.None);

        // Assert
        Assert.Equal(2, userResults.Count);
        Assert.All(userResults, r => Assert.Equal(userId, r.UserId));
    }

    [Fact]
    public async Task RaceResultRepository_GetRecentByUserIdAsync_ReturnsLimitedCount()
    {
        // Arrange
        var context = CreateTestDbContext();
        var repository = new RaceResultRepository(context);
        var userId = Guid.NewGuid();

        for (int i = 0; i < 10; i++)
        {
            await repository.AddAsync(
                new RaceResult(TimeSpan.FromSeconds(30 + i), 100 - i, 95 + (i % 2))
                {
                    UserId = userId,
                    CodeSnippetId = Guid.NewGuid(),
                    CreatedAt = DateTime.UtcNow.AddMinutes(-i)
                },
                CancellationToken.None
            );
        }

        // Act
        var recent3 = await repository.GetRecentByUserIdAsync(userId, 3, CancellationToken.None);
        var recent5 = await repository.GetRecentByUserIdAsync(userId, 5, CancellationToken.None);

        // Assert
        Assert.Equal(3, recent3.Count);
        Assert.Equal(5, recent5.Count);
    }

    [Fact]
    public async Task RaceResultRepository_GetByDateRangeAsync_FiltersByDateRange()
    {
        // Arrange
        var context = CreateTestDbContext();
        var repository = new RaceResultRepository(context);
        var userId = Guid.NewGuid();
        var baseDate = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);

        var before = new RaceResult(TimeSpan.FromSeconds(30), 100, 95)
        {
            UserId = userId,
            CodeSnippetId = Guid.NewGuid(),
            CreatedAt = baseDate.AddDays(-5)
        };

        var within1 = new RaceResult(TimeSpan.FromSeconds(28), 105, 96)
        {
            UserId = userId,
            CodeSnippetId = Guid.NewGuid(),
            CreatedAt = baseDate.AddDays(-2)
        };

        var within2 = new RaceResult(TimeSpan.FromSeconds(25), 110, 97)
        {
            UserId = userId,
            CodeSnippetId = Guid.NewGuid(),
            CreatedAt = baseDate.AddDays(1)
        };

        var after = new RaceResult(TimeSpan.FromSeconds(35), 95, 94)
        {
            UserId = userId,
            CodeSnippetId = Guid.NewGuid(),
            CreatedAt = baseDate.AddDays(5)
        };

        await repository.AddAsync(before, CancellationToken.None);
        await repository.AddAsync(within1, CancellationToken.None);
        await repository.AddAsync(within2, CancellationToken.None);
        await repository.AddAsync(after, CancellationToken.None);

        // Act
        var inRange = await repository.GetByUserIdAndDateRangeAsync(
            userId,
            baseDate.AddDays(-3),
            baseDate.AddDays(3),
            CancellationToken.None
        );

        // Assert
        Assert.Equal(2, inRange.Count);
        Assert.All(inRange, r => Assert.True(r.CreatedAt >= baseDate.AddDays(-3) && r.CreatedAt <= baseDate.AddDays(3)));
    }

    [Fact]
    public async Task RaceResultRepository_GetByUserIdAsync_ReturnsNewestFirst()
    {
        // Arrange
        var context = CreateTestDbContext();
        var repository = new RaceResultRepository(context);
        var userId = Guid.NewGuid();
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        foreach (var daysAgo in new[] { 3, 1, 2 })
        {
            await repository.AddAsync(
                new RaceResult(TimeSpan.FromSeconds(30), 100, 95) { UserId = userId, CreatedAt = start.AddDays(-daysAgo) },
                CancellationToken.None);
        }

        // Act
        var results = await repository.GetByUserIdAsync(userId, CancellationToken.None);

        // Assert
        Assert.Equal(new[] { start.AddDays(-1), start.AddDays(-2), start.AddDays(-3) }, results.Select(r => r.CreatedAt));
    }

    [Fact]
    public async Task FindOrderedDescendingAsync_OtherKeyType_SortsByThatKeyAndTakesTop()
    {
        // Arrange - the generic method sorted by typing speed (double) instead of a date
        var context = CreateTestDbContext();
        var repository = new TopSpeedRaceResultRepository(context);
        foreach (var speed in new[] { 80.0, 120.0, 95.0, 150.0 })
        {
            await repository.AddAsync(
                new RaceResult(TimeSpan.FromSeconds(30), speed, 95) { UserId = Guid.NewGuid() },
                CancellationToken.None);
        }

        // Act
        var top = await repository.GetTopBySpeedAsync(2, CancellationToken.None);

        // Assert
        Assert.Equal(new[] { 150.0, 120.0 }, top.Select(r => r.TypingSpeed));
    }

    [Fact]
    public async Task FindOrderedDescendingAsync_CancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var repository = new TopSpeedRaceResultRepository(CreateTestDbContext());
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => repository.GetTopBySpeedAsync(2, cts.Token));
    }

    /// <summary>Exposes the protected generic method with a non-date sort key, the way a leaderboard would use it.</summary>
    private sealed class TopSpeedRaceResultRepository(CodeRacerDbContext context) : RaceResultRepository(context)
    {
        public Task<IReadOnlyList<RaceResult>> GetTopBySpeedAsync(int count, CancellationToken cancellationToken) =>
            FindOrderedDescendingAsync(r => true, r => r.TypingSpeed, count, cancellationToken);
    }

    #endregion

    #region Cancellation Token Tests

    [Fact]
    public async Task UserRepository_CancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var context = CreateTestDbContext();
        var repository = new UserRepository(context);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => repository.GetAllAsync(cts.Token)
        );
    }

    [Fact]
    public async Task RaceResultRepository_CancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var context = CreateTestDbContext();
        var repository = new RaceResultRepository(context);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => repository.GetByUserIdAsync(Guid.NewGuid(), cts.Token)
        );
    }

    #endregion
}
