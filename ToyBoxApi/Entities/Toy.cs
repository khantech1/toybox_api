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

    // Navigation
    [ForeignKey(nameof(OwnerUserId))]
    public User? Owner { get; set; }

    [ForeignKey(nameof(CategoryId))]
    public Category? Category { get; set; }

    [ForeignKey(nameof(DesiredCategoryId))]
    public Category? DesiredCategory { get; set; }

    public ICollection<ToyImage> Images { get; set; } = new List<ToyImage>();
    public ICollection<SharedToy> SharedWith { get; set; } = new List<SharedToy>();
    public ICollection<ExchangeRequestToy> ExchangeRequestToys { get; set; } = new List<ExchangeRequestToy>();
}
