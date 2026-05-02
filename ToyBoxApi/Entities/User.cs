using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ToyBoxApi.Entities;

[Table("Users")]
public class User
{
    [Key]
    [Column("user_id")]
    public int UserId { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Column("password")]
    public string Password { get; set; } = string.Empty;

    [MaxLength(20)]
    [Column("phone_no")]
    public string? PhoneNo { get; set; }

    [MaxLength(255)]
    [Column("address")]
    public string? Address { get; set; }

    [MaxLength(500)]
    [Column("profile_pic")]
    public string? ProfilePic { get; set; }

    [Column("rating", TypeName = "decimal(4,2)")]
    public decimal? Rating { get; set; }

    // Navigation
    public ICollection<Toy> Toys { get; set; } = new List<Toy>();
    public ICollection<Contact> ContactsAsUser { get; set; } = new List<Contact>();
    public ICollection<Contact> ContactsAsContact { get; set; } = new List<Contact>();
    public ICollection<ExchangeRequest> InitiatedRequests { get; set; } = new List<ExchangeRequest>();
    public ICollection<Review> ReviewsGiven { get; set; } = new List<Review>();
    public ICollection<Review> ReviewsReceived { get; set; } = new List<Review>();
    public ICollection<SharedToy> SharedToys { get; set; } = new List<SharedToy>();
}
