using System.ComponentModel.DataAnnotations;
using ToyBoxApi.DTOs.Auth;

namespace ToyBoxApi.DTOs.Reviews;

public class ReviewDto
{
    public int ReviewId { get; set; }
    public int RequestId { get; set; }
    public int ReviewerUserId { get; set; }
    public int RevieweeUserId { get; set; }
    public int RatingScore { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public UserDto? Reviewer { get; set; }
    public UserDto? Reviewee { get; set; }
}

public class CreateReviewRequest
{
    [Required]
    public int RequestId { get; set; }

    [Required]
    public int RevieweeUserId { get; set; }

    [Required, Range(1, 10)]
    public int RatingScore { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }
}

public class ReviewResponse
{
    public ReviewDto Review { get; set; } = null!;
}
