namespace CodeRacer.Server.Exceptions;

/// <summary>
/// Thrown when a user cannot be saved because the username or email is already taken,
/// for example when two registrations with the same name arrive at the same time.
/// </summary>
public class DuplicateUserException : Exception
{
    public DuplicateUserException(Exception innerException)
        : base("A user with the same username or email already exists.", innerException)
    {
    }
}
