using System.ComponentModel.DataAnnotations;
using ToyBoxApi.DTOs.Toys;
using ToyBoxApi.DTOs.Auth;

namespace ToyBoxApi.DTOs.Exchange;

// ── Response DTOs ─────────────────────────────────────────────────────────────

public class ExchangeRequestDto
{
    public int RequestId { get; set; }
    public int InitiatorUserId { get; set; }
    public int? ReceiverUserId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ExchangeType { get; set; } = "permanent";
    public DateTime CreatedAt { get; set; }
    public string? Message { get; set; }
    public DateTime? ReturnDueAt { get; set; }
    public DateTime? LoanStartedAt { get; set; }
    public DateTime? ReturnedAt { get; set; }
    public bool ReturnConfirmedByInitiator { get; set; }
    public bool ReturnConfirmedByReceiver { get; set; }
    public bool IsOverdue { get; set; }
    public UserDto? Initiator { get; set; }
    public UserDto? Receiver { get; set; }
    public List<ExchangeRequestToyDto> Toys { get; set; } = new();

    /// <summary>Sum of value_at_request for all "requested" toys.</summary>
    public decimal RequestedTotalValue { get; set; }

    /// <summary>Sum of value_at_request for all "offered" toys.</summary>
    public decimal OfferedTotalValue { get; set; }
}

public class ExchangeRequestToyDto
{
    public int RequestId { get; set; }
    public int ToyId { get; set; }
    public string ExchangeRole { get; set; } = string.Empty;
    public decimal? ValueAtRequest { get; set; }
    public ToyDto? Toy { get; set; }
}

public class ExchangeRequestResponse
{
    public ExchangeRequestDto ExchangeRequest { get; set; } = null!;
}

// ── Request DTOs ──────────────────────────────────────────────────────────────

public class CreateExchangeRequest
{
    [Required]
    [MinLength(1)]
    public List<int> RequestedToyIds { get; set; } = new();

    [Required]
    [MinLength(1)]
    public List<int> OfferedToyIds { get; set; } = new();

    [MaxLength(500)]
    public string? Message { get; set; }

    /// <summary>permanent | temporary</summary>
    public string ExchangeType { get; set; } = "permanent";

    /// <summary>Required for temporary exchanges (UTC).</summary>
    public DateTime? ReturnDueAt { get; set; }
}
