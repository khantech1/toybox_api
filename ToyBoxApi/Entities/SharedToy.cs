using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ToyBoxApi.Entities;

[Table("Shared_Toys")]
[PrimaryKey(nameof(SharedWithUserId), nameof(ToyId))]
public class SharedToy
{
    [Column("shared_with_user_id")]
    public int SharedWithUserId { get; set; }

    [Column("toy_id")]
    public int ToyId { get; set; }

    [ForeignKey(nameof(SharedWithUserId))]
    public User? SharedWithUser { get; set; }

    [ForeignKey(nameof(ToyId))]
    public Toy? Toy { get; set; }
}
