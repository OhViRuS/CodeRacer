using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace CodeRacer.Server.Settings;

/// <summary>
/// Strongly typed JWT settings, bound from the "Jwt" configuration section.
/// The single source of truth for both issuing tokens (AuthenticationService)
/// and validating them (JWT bearer authentication in Program.cs).
/// Validated when the application starts, so a missing or weak key fails fast with instructions,
/// instead of the app running with a key that is public in the repository.
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    public const string MissingSecretKeyMessage =
        "Jwt:SecretKey is missing or shorter than 32 characters. It is only set for Development and Testing; "
        + "for any other environment supply it with the Jwt__SecretKey environment variable or "
        + "'dotnet user-secrets set \"Jwt:SecretKey\" \"<at least 32 characters>\"'.";

    [Required(ErrorMessage = MissingSecretKeyMessage)]
    [MinLength(32, ErrorMessage = MissingSecretKeyMessage)]
    public string SecretKey { get; set; } = string.Empty;

    [Required]
    public string Issuer { get; set; } = "CodeRacer";

    [Required]
    public string Audience { get; set; } = "CodeRacerApp";

    [Range(1, 24 * 60)]
    public int ExpirationMinutes { get; set; } = 60;

    public TimeSpan Expiration => TimeSpan.FromMinutes(ExpirationMinutes);

    public SymmetricSecurityKey CreateSigningKey() => new(Encoding.UTF8.GetBytes(SecretKey));

    public TokenValidationParameters CreateValidationParameters() => new()
    {
        ValidateIssuer = true,
        ValidIssuer = Issuer,
        ValidateAudience = true,
        ValidAudience = Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = CreateSigningKey(),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(1)
    };
}
