namespace CodeRacer.Server.Models;

/// <summary>
/// A signed JWT together with the moment it stops being valid.
/// Immutable: produced by IAuthenticationService and passed on unchanged.
/// </summary>
public record AuthenticationToken(string Token, DateTime ExpiresAt);
