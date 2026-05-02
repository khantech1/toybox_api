using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ToyBoxApi.Entities;

[Table("Categories")]
public class Category
{
    [Key]
    [Column("category_id")]
    public int CategoryId { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("category_name")]
    public string CategoryName { get; set; } = string.Empty;

    // Navigation
    public ICollection<Toy> ToysInCategory { get; set; } = new List<Toy>();
    public ICollection<Toy> ToysDesiredCategory { get; set; } = new List<Toy>();
}
