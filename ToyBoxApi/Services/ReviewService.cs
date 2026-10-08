using Microsoft.EntityFrameworkCore;
using ToyBoxApi.Data;
using ToyBoxApi.DTOs.Reviews;
using ToyBoxApi.Entities;

namespace ToyBoxApi.Services;

public interface IReviewService
{
    Task<ReviewDto> CreateAsync(int reviewerUserId, CreateReviewRequest request);
    Task<List<ReviewDto>> GetForUserAsync(int userId);
}

public class ReviewService : IReviewService
{
    private readonly AppDbContext _db;

    public ReviewService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ReviewDto> CreateAsync(int reviewerUserId, CreateReviewRequest request)
    {
        // Verify the exchange request exists and is completed or accepted
        var exchangeRequest = await _db.ExchangeRequests
            .FirstOrDefaultAsync(r => r.RequestId == request.RequestId)
            ?? throw new KeyNotFoundException("Exchange request not found.");

        if (exchangeRequest.Status is not ("completed" or "accepted" or "returned"))
            throw new InvalidOperationException("You can only review completed exchanges.");

        var parties = new[] { exchangeRequest.InitiatorUserId, exchangeRequest.ReceiverUserId };

        if (!parties.Contains(reviewerUserId))
            throw new UnauthorizedAccessException("You did not participate in this exchange.");

        if (!parties.Contains(request.RevieweeUserId))
            throw new InvalidOperationException("You can only review the other party of this exchange.");

        // Prevent reviewing yourself
        if (request.RevieweeUserId == reviewerUserId)
            throw new InvalidOperationException("You cannot review yourself.");

        // Check for duplicate review
        var alreadyReviewed = await _db.Reviews.AnyAsync(r =>
            r.RequestId == request.RequestId &&
            r.ReviewerUserId == reviewerUserId);

        if (alreadyReviewed)
            throw new InvalidOperationException("You have already reviewed this exchange.");

        var review = new Review
        {
            RequestId      = request.RequestId,
            ReviewerUserId = reviewerUserId,
            RevieweeUserId = request.RevieweeUserId,
            RatingScore    = request.RatingScore,
            Description    = request.Description?.Trim(),
            CreatedAt      = DateTime.UtcNow,
        };

        _db.Reviews.Add(review);
        await _db.SaveChangesAsync();

        // Recalculate reviewee's aggregate rating
        await RecalculateRatingAsync(request.RevieweeUserId);

        // Load with navigation for response
        var full = await _db.Reviews
            .Include(r => r.Reviewer)
            .Include(r => r.Reviewee)
            .FirstAsync(r => r.ReviewId == review.ReviewId);

        return MapToDto(full);
    }

    public async Task<List<ReviewDto>> GetForUserAsync(int userId)
    {
        var reviews = await _db.Reviews
            .Include(r => r.Reviewer)
            .Include(r => r.Reviewee)
            .Where(r => r.RevieweeUserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return reviews.Select(MapToDto).ToList();
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Recalculates and saves the average rating for a user based on all reviews received.
    /// </summary>
    private async Task RecalculateRatingAsync(int userId)
    {
        var avgRating = await _db.Reviews
            .Where(r => r.RevieweeUserId == userId)
            .AverageAsync(r => (double?)r.RatingScore);

        var user = await _db.Users.FindAsync(userId);
        if (user is not null)
        {
            user.Rating = avgRating.HasValue
                ? Math.Round((decimal)avgRating.Value, 2)
                : null;
            await _db.SaveChangesAsync();
        }
    }

    // ── Mapper ────────────────────────────────────────────────────────────────
    private static ReviewDto MapToDto(Review r) => new()
    {
        ReviewId       = r.ReviewId,
        RequestId      = r.RequestId,
        ReviewerUserId = r.ReviewerUserId,
        RevieweeUserId = r.RevieweeUserId,
        RatingScore    = r.RatingScore,
        Description    = r.Description,
        CreatedAt      = r.CreatedAt,
        Reviewer  = r.Reviewer is null  ? null : AuthService.MapToDto(r.Reviewer),
        Reviewee  = r.Reviewee is null  ? null : AuthService.MapToDto(r.Reviewee),
    };
}
