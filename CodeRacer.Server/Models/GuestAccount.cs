namespace CodeRacer.Server.Models;

/// <summary>
/// The shared account that race results of players who are not signed in are saved under.
/// Uses the same fixed Id as the earlier guest design (PR #27), so data stays compatible.
/// Nobody can sign in as the guest: its password hash is not a valid bcrypt hash, and
/// UserService also refuses the guest explicitly and reserves its username and email
/// (a real account holding either would make creating the guest fail).
/// </summary>
public static class GuestAccount
{
    public static readonly Guid Id = new("11111111-1111-1111-1111-111111111111");

    public const string UserName = "Guest";

    public const string Email = "guest@coderacer.local";

    public const string PasswordHash = "GUEST_ACCOUNT";

    public static bool IsReservedUserName(string userName) =>
        string.Equals(userName.Trim(), UserName, StringComparison.OrdinalIgnoreCase);

    public static bool IsReservedEmail(string email) =>
        string.Equals(email.Trim(), Email, StringComparison.OrdinalIgnoreCase);
}
