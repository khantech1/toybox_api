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

    /// <summary>Owner of the requested toys at the time the request was made.</summary>
    [Column("receiver_user_id")]
    public int? ReceiverUserId { get; set; }

    /// <summary>pending | declined | completed | active_loan | returned</summary>
    [Required]
    [MaxLength(20)]
    [Column("status")]
    public string Status { get; set; } = "pending";

    /// <summary>permanent | temporary</summary>
    [Required]
    [MaxLength(20)]
    [Column("exchange_type")]
    public string ExchangeType { get; set; } = "permanent";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(500)]
    [Column("message")]
    public string? Message { get; set; }

    [Column("return_due_at")]
    public DateTime? ReturnDueAt { get; set; }

    [Column("loan_started_at")]
    public DateTime? LoanStartedAt { get; set; }

    [Column("returned_at")]
    public DateTime? ReturnedAt { get; set; }

    [Column("return_confirmed_by_initiator")]
    public bool ReturnConfirmedByInitiator { get; set; }

    [Column("return_confirmed_by_receiver")]
    public bool ReturnConfirmedByReceiver { get; set; }

    [Column("due_soon_notified_at")]
    public DateTime? DueSoonNotifiedAt { get; set; }

    [Column("overdue_notified_at")]
    public DateTime? OverdueNotifiedAt { get; set; }

    [ForeignKey(nameof(InitiatorUserId))]
    public User? Initiator { get; set; }

    [ForeignKey(nameof(ReceiverUserId))]
    public User? Receiver { get; set; }

    public ICollection<ExchangeRequestToy> Toys { get; set; } = new List<ExchangeRequestToy>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}

public static class ExchangeStatuses
{
    public const string Pending    = "pending";
    public const string Declined   = "declined";
    public const string Completed  = "completed";
    public const string ActiveLoan = "active_loan";
    public const string Returned   = "returned";
}
