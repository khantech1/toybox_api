using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ToyBoxApi.Entities;

[Table("Toys")]
public class Toy
{
    [Key]
    [Column("toy_id")]
    public int ToyId { get; set; }

    [Column("owner_user_id")]
    public int OwnerUserId { get; set; }

    /// <summary>Who physically has the toy. Differs from the owner only during a temporary exchange.</summary>
    [Column("current_holder_user_id")]
    public int CurrentHolderUserId { get; set; }

    [Required]
    [MaxLength(150)]
    [Column("toy_name")]
    public string ToyName { get; set; } = string.Empty;

    [Column("toy_description")]
    public string? ToyDescription { get; set; }

    [Column("category_id")]
    public int? CategoryId { get; set; }

    [Column("desired_category_id")]
    public int? DesiredCategoryId { get; set; }

    /// <summary>Condition rating 1–10</summary>
    [Column("condition_status")]
    public int? ConditionStatus { get; set; }

    [Column("value", TypeName = "decimal(10,2)")]
    public decimal? Value { get; set; }

    /// <summary>Listed for exchange in the catalog. Owned-but-unlisted toys only appear in the owner's toys.</summary>
    [Column("is_listed")]
    public bool IsListed { get; set; } = true;

    [Timestamp]
    [Column("row_version")]
    public byte[] RowVersion { get; set; } = null!;

    [NotMapped]
    public bool IsOnLoan => CurrentHolderUserId != OwnerUserId;

    // Navigation
    [ForeignKey(nameof(OwnerUserId))]
    public User? Owner { get; set; }

    [ForeignKey(nameof(CurrentHolderUserId))]
    public User? CurrentHolder { get; set; }

    [ForeignKey(nameof(CategoryId))]
    public Category? Category { get; set; }

    [ForeignKey(nameof(DesiredCategoryId))]
    public Category? DesiredCategory { get; set; }

    public ICollection<ToyImage> Images { get; set; } = new List<ToyImage>();
    public ICollection<SharedToy> SharedWith { get; set; } = new List<SharedToy>();
    public ICollection<ExchangeRequestToy> ExchangeRequestToys { get; set; } = new List<ExchangeRequestToy>();
    public ICollection<ToyOwnershipHistory> History { get; set; } = new List<ToyOwnershipHistory>();
    public ICollection<ToyPriority> Priorities { get; set; } = new List<ToyPriority>();
    public ICollection<ToyGift> Gifts { get; set; } = new List<ToyGift>();
}
