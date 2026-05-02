using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ToyBoxApi.Entities;

[Table("Reviews")]
public class Review
{
    [Key]
    [Column("review_id")]
    public int ReviewId { get; set; }

    [Column("request_id")]
    public int RequestId { get; set; }

    [Column("reviewer_user_id")]
    public int ReviewerUserId { get; set; }

    [Column("reviewee_user_id")]
    public int RevieweeUserId { get; set; }

    [Range(1, 10)]
    [Column("rating_score")]
    public int RatingScore { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(RequestId))]
    public ExchangeRequest? ExchangeRequest { get; set; }

    [ForeignKey(nameof(ReviewerUserId))]
    public User? Reviewer { get; set; }

    [ForeignKey(nameof(RevieweeUserId))]
    public User? Reviewee { get; set; }
}
