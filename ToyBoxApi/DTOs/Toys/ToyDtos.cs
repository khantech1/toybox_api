using System.ComponentModel.DataAnnotations;

namespace ToyBoxApi.DTOs.Toys;

// ── Response DTOs ─────────────────────────────────────────────────────────────

public class ToyDto
{
    public int ToyId { get; set; }
    public int OwnerUserId { get; set; }
    public string ToyName { get; set; } = string.Empty;
    public string? ToyDescription { get; set; }
    public int? CategoryId { get; set; }
    public int? DesiredCategoryId { get; set; }
    public int? ConditionStatus { get; set; }
    public decimal? Value { get; set; }
    public bool IsListed { get; set; }
    public int CurrentHolderUserId { get; set; }
    public bool IsOnLoan { get; set; }
    public ToyOwnerDto? Owner { get; set; }
    public ToyOwnerDto? CurrentHolder { get; set; }
    public CategoryDto? Category { get; set; }
    public CategoryDto? DesiredCategory { get; set; }
    public List<ToyImageDto> Images { get; set; } = new();

    // Populated on detail / catalog views only.
    public int? PriorityCount { get; set; }
    public bool? IsPrioritizedByMe { get; set; }
    public bool? HasPendingGift { get; set; }
}

public class OwnershipHistoryDto
{
    public int HistoryId { get; set; }
    public string Type { get; set; } = string.Empty;
    public ToyOwnerDto? User { get; set; }
    public int? RequestId { get; set; }
    public int? GiftId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
}

public class PriorityQueueEntryDto
{
    public int Position { get; set; }
    public ToyOwnerDto User { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    /// <summary>True when the toy owner has this user in their contacts (i.e. can gift to them).</summary>
    public bool IsContact { get; set; }
}

public class ToyOwnerDto
{
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ProfilePic { get; set; }
    public decimal? Rating { get; set; }
    public string? Address { get; set; }
}

public class CategoryDto
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
}

public class ToyImageDto
{
    public int ImageId { get; set; }
    public int ToyId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
}

// ── Request DTOs ──────────────────────────────────────────────────────────────

public class CreateToyRequest
{
    [Required, MaxLength(150)]
    public string ToyName { get; set; } = string.Empty;

    public string? ToyDescription { get; set; }

    public int? CategoryId { get; set; }

    public int? DesiredCategoryId { get; set; }

    [Range(1, 10)]
    public int? ConditionStatus { get; set; }

    [Range(0, 999999)]
    public decimal? Value { get; set; }

    public bool VisibleToAll { get; set; } = true;

    public List<int>? VisibleToUserIds { get; set; }

    /// <summary>false = just add to my toys, true = also list for exchange.</summary>
    public bool IsListed { get; set; } = true;
}

public class SetListingRequest
{
    [Required]
    public bool IsListed { get; set; }
}

public class UpdateToyRequest
{
    [MaxLength(150)]
    public string? ToyName { get; set; }

    public string? ToyDescription { get; set; }

    public int? CategoryId { get; set; }

    public int? DesiredCategoryId { get; set; }

    [Range(1, 10)]
    public int? ConditionStatus { get; set; }

    [Range(0, 999999)]
    public decimal? Value { get; set; }

    public bool? VisibleToAll { get; set; }

    public List<int>? VisibleToUserIds { get; set; }

    public bool? IsListed { get; set; }
}

public class ToyListResponse
{
    public List<ToyDto> Toys { get; set; } = new();
}

public class SingleToyResponse
{
    public ToyDto Toy { get; set; } = null!;
}
