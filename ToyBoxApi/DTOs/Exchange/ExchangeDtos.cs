using System.ComponentModel.DataAnnotations;
using ToyBoxApi.DTOs.Toys;
using ToyBoxApi.DTOs.Auth;

namespace ToyBoxApi.DTOs.Exchange;

// ── Response DTOs ─────────────────────────────────────────────────────────────

public class ExchangeRequestDto
{
    public int RequestId { get; set; }
    public int InitiatorUserId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? Message { get; set; }
    public UserDto? Initiator { get; set; }
    public List<ExchangeRequestToyDto> Toys { get; set; } = new();
}

public class ExchangeRequestToyDto
{
    public int RequestId { get; set; }
    public int ToyId { get; set; }
    public string ExchangeRole { get; set; } = string.Empty;
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
    public int RequestedToyId { get; set; }

    [Required]
    public int OfferedToyId { get; set; }

    [MaxLength(500)]
    public string? Message { get; set; }
}
