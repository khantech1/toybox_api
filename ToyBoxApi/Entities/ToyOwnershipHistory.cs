using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ToyBoxApi.Entities;

/// <summary>
/// One row per period a user had a toy. Ownership periods have type
/// created | exchange | gift (UserId = owner); loan periods have type
/// loan (UserId = temporary holder). EndedAt is null for the open period.
/// </summary>
[Table("Toy_Ownership_History")]
public class ToyOwnershipHistory
{
    [Key]
    [Column("history_id")]
    public int HistoryId { get; set; }

    [Column("toy_id")]
    public int ToyId { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("type")]
    public string Type { get; set; } = HistoryTypes.Created;

    [Column("request_id")]
    public int? RequestId { get; set; }

    [Column("gift_id")]
    public int? GiftId { get; set; }

    [Column("started_at")]
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    [Column("ended_at")]
    public DateTime? EndedAt { get; set; }

    [ForeignKey(nameof(ToyId))]
    public Toy? Toy { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }
}

public static class HistoryTypes
{
    public const string Created  = "created";
    public const string Exchange = "exchange";
    public const string Gift     = "gift";
    public const string Loan     = "loan";
}
