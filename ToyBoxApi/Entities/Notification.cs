using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ToyBoxApi.Entities;

[Table("Notifications")]
public class Notification
{
    [Key]
    [Column("notification_id")]
    public int NotificationId { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }

    [Required]
    [MaxLength(40)]
    [Column("type")]
    public string Type { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [MaxLength(500)]
    [Column("body")]
    public string? Body { get; set; }

    // Plain ids (no FKs) so notifications survive deletes and avoid cascade paths.
    [Column("toy_id")]
    public int? ToyId { get; set; }

    [Column("request_id")]
    public int? RequestId { get; set; }

    [Column("gift_id")]
    public int? GiftId { get; set; }

    [Column("actor_user_id")]
    public int? ActorUserId { get; set; }

    [Column("is_read")]
    public bool IsRead { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }
}
