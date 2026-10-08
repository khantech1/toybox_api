using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ToyBoxApi.Entities;

/// <summary>A user's interest in another user's toy. Queue order is CreatedAt.</summary>
[Table("Toy_Priorities")]
[PrimaryKey(nameof(ToyId), nameof(UserId))]
public class ToyPriority
{
    [Column("toy_id")]
    public int ToyId { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(ToyId))]
    public Toy? Toy { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }
}
