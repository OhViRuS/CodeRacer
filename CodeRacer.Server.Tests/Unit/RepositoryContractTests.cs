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

    #region UserRepository Contract Tests

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
    public async Task RaceResultRepository_SaveAndRetrieveByUserId()
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
