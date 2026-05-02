using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToyBoxApi.DTOs.Reviews;
using ToyBoxApi.Helpers;
using ToyBoxApi.Services;

namespace ToyBoxApi.Controllers;

[ApiController]
[Route("api/reviews")]
[Authorize]
public class ReviewsController : ControllerBase
{
    private readonly IReviewService _reviewService;

    public ReviewsController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    /// <summary>Submit a review for a completed exchange.</summary>
    /// <remarks>POST /api/reviews</remarks>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateReviewRequest request)
    {
        try
        {
            var reviewerUserId = User.GetUserId();
            var review = await _reviewService.CreateAsync(reviewerUserId, request);
            return StatusCode(201, new { review });
        }
        catch (KeyNotFoundException ex)        { return NotFound(new { message = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return Forbid(); }
        catch (InvalidOperationException ex)   { return Conflict(new { message = ex.Message }); }
    }

    /// <summary>Get all reviews received by a specific user.</summary>
    /// <remarks>GET /api/reviews/user/{userId}</remarks>
    [HttpGet("user/{userId:int}")]
    public async Task<IActionResult> GetForUser(int userId)
    {
        var reviews = await _reviewService.GetForUserAsync(userId);
        return Ok(reviews);
    }
}
