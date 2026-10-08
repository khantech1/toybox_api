using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ToyBoxApi.Entities;

[Table("Toy_Gifts")]
public class ToyGift
{
    [Key]
    [Column("gift_id")]
    public int GiftId { get; set; }

    [Column("toy_id")]
    public int ToyId { get; set; }

    [Column("from_user_id")]
    public int FromUserId { get; set; }

    [Column("to_user_id")]
    public int ToUserId { get; set; }

    /// <summary>pending | accepted | declined | cancelled</summary>
    [Required]
    [MaxLength(20)]
    [Column("status")]
    public string Status { get; set; } = "pending";

    [MaxLength(500)]
    [Column("message")]
    public string? Message { get; set; }

    // Birthday gift for the recipient's child. Plain id (no FK) to avoid a second
    // cascade path from Users; the name is snapshotted for display.
    [Column("child_id")]
    public int? ChildId { get; set; }

    [MaxLength(100)]
    [Column("child_name")]
    public string? ChildName { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("responded_at")]
    public DateTime? RespondedAt { get; set; }

    [ForeignKey(nameof(ToyId))]
    public Toy? Toy { get; set; }

    [ForeignKey(nameof(FromUserId))]
    public User? FromUser { get; set; }

    [ForeignKey(nameof(ToUserId))]
    public User? ToUser { get; set; }
}
