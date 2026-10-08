using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ToyBoxApi.Entities;

[Table("Children")]
public class Child
{
    [Key]
    [Column("child_id")]
    public int ChildId { get; set; }

    [Column("parent_user_id")]
    public int ParentUserId { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("birth_date", TypeName = "date")]
    public DateOnly BirthDate { get; set; }

    /// <summary>Free text to help others pick a gift, e.g. "dinosaurs, puzzles".</summary>
    [MaxLength(200)]
    [Column("interests")]
    public string? Interests { get; set; }

    /// <summary>When false, only the parent sees this child.</summary>
    [Column("is_visible_to_contacts")]
    public bool IsVisibleToContacts { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(ParentUserId))]
    public User? Parent { get; set; }
}
