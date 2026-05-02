using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ToyBoxApi.Entities;

[Table("Exchange_Requests")]
public class ExchangeRequest
{
    [Key]
    [Column("request_id")]
    public int RequestId { get; set; }

    [Column("initiator_user_id")]
    public int InitiatorUserId { get; set; }

    /// <summary>pending | accepted | declined | completed</summary>
    [Required]
    [MaxLength(20)]
    [Column("status")]
    public string Status { get; set; } = "pending";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(500)]
    [Column("message")]
    public string? Message { get; set; }

    [ForeignKey(nameof(InitiatorUserId))]
    public User? Initiator { get; set; }

    public ICollection<ExchangeRequestToy> Toys { get; set; } = new List<ExchangeRequestToy>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}
