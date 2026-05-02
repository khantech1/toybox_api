using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ToyBoxApi.Entities;

[Table("Contacts")]
[PrimaryKey(nameof(UserId), nameof(ContactId))]
public class Contact
{
    [Column("user_id")]
    public int UserId { get; set; }

    [Column("contact_id")]
    public int ContactId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    [ForeignKey(nameof(ContactId))]
    public User? ContactUser { get; set; }
}
