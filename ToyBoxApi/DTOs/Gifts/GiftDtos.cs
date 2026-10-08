using System.ComponentModel.DataAnnotations;
using ToyBoxApi.DTOs.Toys;

namespace ToyBoxApi.DTOs.Gifts;

public class GiftDto
{
    public int GiftId { get; set; }
    public int ToyId { get; set; }
    public int FromUserId { get; set; }
    public int ToUserId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Message { get; set; }
    public int? ChildId { get; set; }
    public string? ChildName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
    public ToyDto? Toy { get; set; }
    public ToyOwnerDto? FromUser { get; set; }
    public ToyOwnerDto? ToUser { get; set; }
}

public class CreateGiftRequest
{
    [Required]
    public int ToyId { get; set; }

    [Required]
    public int ToUserId { get; set; }

    [MaxLength(500)]
    public string? Message { get; set; }

    /// <summary>Optional: a birthday gift for one of the recipient's children.</summary>
    public int? ChildId { get; set; }
}
