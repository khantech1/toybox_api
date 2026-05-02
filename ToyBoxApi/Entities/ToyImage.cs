using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ToyBoxApi.Entities;

[Table("Toy_Images")]
public class ToyImage
{
    [Key]
    [Column("image_id")]
    public int ImageId { get; set; }

    [Column("toy_id")]
    public int ToyId { get; set; }

    [Required]
    [MaxLength(500)]
    [Column("image_url")]
    public string ImageUrl { get; set; } = string.Empty;

    [ForeignKey(nameof(ToyId))]
    public Toy? Toy { get; set; }
}
