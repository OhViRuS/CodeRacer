namespace CodeRacer.Server.Extensions;

public static class DateTimeExtensions
{
    /// <summary>
    /// Returns the same moment as a UTC DateTime. Dates without a time zone
    /// (e.g. "2026-01-15" from a query string) are taken to already be UTC.
    /// PostgreSQL only accepts UTC values for timestamp with time zone columns.
    /// </summary>
    public static DateTime AsUtc(this DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
