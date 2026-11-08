namespace CodeRacer.Server.Tests.TestDoubles;

/// <summary>
/// A clock stuck at one point in time, so tests can assert exact timestamps
/// instead of depending on when they happen to run.
/// </summary>
public sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public static readonly DateTimeOffset DefaultNow = new(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);

    public FixedTimeProvider()
        : this(DefaultNow)
    {
    }

    public override DateTimeOffset GetUtcNow() => now;
}
