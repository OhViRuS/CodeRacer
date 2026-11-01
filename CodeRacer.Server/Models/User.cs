using System.ComponentModel.DataAnnotations;

namespace CodeRacer.Server.Models;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(50)]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [MaxLength(320)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<RaceResult> RaceResults { get; set; } = new List<RaceResult>();
}
