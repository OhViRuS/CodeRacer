using CodeRacer.Server.Extensions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CodeRacer.Server.Data;

/// <summary>
/// Stores every DateTime as UTC and reads it back marked as UTC.
/// PostgreSQL 'timestamp with time zone' columns reject non-UTC DateTime values, so this
/// applies the rule once for all entities (and the query parameters compared with them),
/// instead of each endpoint having to remember it.
/// </summary>
public class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    value => value.AsUtc(),
    value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
