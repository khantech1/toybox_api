using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToyBoxApi.DTOs.Toys;
using ToyBoxApi.Helpers;
using ToyBoxApi.Services;

namespace ToyBoxApi.Controllers;

// Errors (not found / forbidden / invalid) are mapped by ExceptionMiddleware.
[ApiController]
[Route("api/toys")]
[Authorize]
public class ToysController : ControllerBase
{
    private readonly IToyService _toyService;

    public ToysController(IToyService toyService)
    {
        _toyService = toyService;
    }

    /// <summary>
    /// Catalog: listed toys shared with the current user. Supports search and category filters.
    /// </summary>
    /// <remarks>GET /api/toys?search=train&amp;category_id=3</remarks>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery(Name = "category_id")] int? categoryId,
        [FromQuery(Name = "age_group")] string? ageGroup)
    {
        var toys = await _toyService.GetAllAsync(User.GetUserId(), search, categoryId, ageGroup);
        return Ok(toys);
    }

    /// <summary>The current user's toys (owned, listed or not, plus toys they are borrowing).</summary>
    /// <remarks>GET /api/toys/my-toys</remarks>
    [HttpGet("my-toys")]
    public async Task<IActionResult> GetMyToys()
    {
        var toys = await _toyService.GetMyToysAsync(User.GetUserId());
        return Ok(toys);
    }

    /// <summary>Get a single toy by ID.</summary>
    /// <remarks>GET /api/toys/{toyId}</remarks>
    [HttpGet("{toyId:int}")]
    public async Task<IActionResult> GetById(int toyId)
    {
        var toy = await _toyService.GetByIdAsync(toyId, User.GetUserId());
        return Ok(toy);
    }

    /// <summary>Create a toy. Set is_listed=false to only add it to your toys.</summary>
    /// <remarks>POST /api/toys</remarks>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateToyRequest request)
    {
        var toy = await _toyService.CreateAsync(User.GetUserId(), request);
        return StatusCode(201, new { toy });
    }

    /// <summary>Update an existing toy (owner only).</summary>
    /// <remarks>PUT /api/toys/{toyId}</remarks>
    [HttpPut("{toyId:int}")]
    public async Task<IActionResult> Update(int toyId, [FromBody] UpdateToyRequest request)
    {
        var toy = await _toyService.UpdateAsync(User.GetUserId(), toyId, request);
        return Ok(new { toy });
    }

    /// <summary>List or unlist a toy for exchange (owner only).</summary>
    /// <remarks>PUT /api/toys/{toyId}/listing</remarks>
    [HttpPut("{toyId:int}/listing")]
    public async Task<IActionResult> SetListing(int toyId, [FromBody] SetListingRequest request)
    {
        var toy = await _toyService.SetListedAsync(User.GetUserId(), toyId, request.IsListed);
        return Ok(new { toy });
    }

    /// <summary>Delete a toy (owner only).</summary>
    /// <remarks>DELETE /api/toys/{toyId}</remarks>
    [HttpDelete("{toyId:int}")]
    public async Task<IActionResult> Delete(int toyId)
    {
        await _toyService.DeleteAsync(User.GetUserId(), toyId);
        return NoContent();
    }

    /// <summary>Upload an image for a toy (max 5 images, owner only).</summary>
    /// <remarks>POST /api/toys/{toyId}/images — multipart/form-data, field: "image"</remarks>
    [HttpPost("{toyId:int}/images")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadImage(int toyId, IFormFile image)
    {
        var toy = await _toyService.AddImageAsync(User.GetUserId(), toyId, image);
        return Ok(new { toy });
    }

    /// <summary>Ownership and loan history of a toy, oldest first.</summary>
    /// <remarks>GET /api/toys/{toyId}/history</remarks>
    [HttpGet("{toyId:int}/history")]
    public async Task<IActionResult> GetHistory(int toyId)
    {
        var history = await _toyService.GetHistoryAsync(User.GetUserId(), toyId);
        return Ok(history);
    }

    /// <summary>Mark priority (interest) on someone else's toy. Idempotent.</summary>
    /// <remarks>POST /api/toys/{toyId}/priority</remarks>
    [HttpPost("{toyId:int}/priority")]
    public async Task<IActionResult> AddPriority(int toyId)
    {
        await _toyService.AddPriorityAsync(User.GetUserId(), toyId);
        return NoContent();
    }

    /// <summary>Remove your priority from a toy. Idempotent.</summary>
    /// <remarks>DELETE /api/toys/{toyId}/priority</remarks>
    [HttpDelete("{toyId:int}/priority")]
    public async Task<IActionResult> RemovePriority(int toyId)
    {
        await _toyService.RemovePriorityAsync(User.GetUserId(), toyId);
        return NoContent();
    }

    /// <summary>Priority queue for a toy in first-come order (owner only).</summary>
    /// <remarks>GET /api/toys/{toyId}/priority-queue</remarks>
    [HttpGet("{toyId:int}/priority-queue")]
    public async Task<IActionResult> GetPriorityQueue(int toyId)
    {
        var queue = await _toyService.GetPriorityQueueAsync(User.GetUserId(), toyId);
        return Ok(queue);
    }
}
