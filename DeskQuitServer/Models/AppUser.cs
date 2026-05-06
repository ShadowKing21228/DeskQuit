using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DeskQuitServer.Models;

[Table("app_user")]
public class AppUser
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("email")]
    public required string Email { get; set; }

    [Column("password_hash")]
    public required string PasswordHash { get; set; }

    [Column("password_salt")]
    public required string PasswordSalt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}
