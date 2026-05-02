using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToyBoxApi.DTOs.Toys;
using ToyBoxApi.Helpers;
using ToyBoxApi.Services;

namespace ToyBoxApi.Controllers;

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
    /// Get all public toys (catalog). Supports search, category, and age_group filters.
    /// </summary>
    /// <remarks>GET /api/toys?search=train&amp;category_id=3&amp;age_group=3-5</remarks>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery(Name = "category_id")] int? categoryId,
        [FromQuery(Name = "age_group")] string? ageGroup)
    {
        var userId = User.GetUserId();
        var toys = await _toyService.GetAllAsync(userId, search, categoryId, ageGroup);
        return Ok(toys);
    }

    /// <summary>Get the current user's own toy listings.</summary>
    /// <remarks>GET /api/toys/my-toys</remarks>
    [HttpGet("my-toys")]
    public async Task<IActionResult> GetMyToys()
    {
        var userId = User.GetUserId();
        var toys = await _toyService.GetMyToysAsync(userId);
        return Ok(toys);
    }

    /// <summary>Get a single toy by ID.</summary>
    /// <remarks>GET /api/toys/{toyId}</remarks>
    [HttpGet("{toyId:int}")]
    public async Task<IActionResult> GetById(int toyId)
    {
        try
        {
            var userId = User.GetUserId();
            var toy = await _toyService.GetByIdAsync(toyId, userId);
            return Ok(toy);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>Create a new toy listing.</summary>
    /// <remarks>POST /api/toys</remarks>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateToyRequest request)
    {
        try
        {
            var userId = User.GetUserId();
            var toy = await _toyService.CreateAsync(userId, request);
            return StatusCode(201, new { toy });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Update an existing toy listing (owner only).</summary>
    /// <remarks>PUT /api/toys/{toyId}</remarks>
    [HttpPut("{toyId:int}")]
    public async Task<IActionResult> Update(int toyId, [FromBody] UpdateToyRequest request)
    {
        try
        {
            var userId = User.GetUserId();
            var toy = await _toyService.UpdateAsync(userId, toyId, request);
            return Ok(new { toy });
        }
        catch (KeyNotFoundException ex)        { return NotFound(new { message = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return Forbid(); }
        catch (Exception ex)                   { return BadRequest(new { message = ex.Message }); }
    }

    /// <summary>Delete a toy listing (owner only).</summary>
    /// <remarks>DELETE /api/toys/{toyId}</remarks>
    [HttpDelete("{toyId:int}")]
    public async Task<IActionResult> Delete(int toyId)
    {
        try
        {
            var userId = User.GetUserId();
            await _toyService.DeleteAsync(userId, toyId);
            return NoContent();
        }
        catch (KeyNotFoundException ex)        { return NotFound(new { message = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return Forbid(); }
    }

    /// <summary>Upload an image for a toy (max 5 images, owner only).</summary>
    /// <remarks>POST /api/toys/{toyId}/images — multipart/form-data, field: "image"</remarks>
    [HttpPost("{toyId:int}/images")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadImage(int toyId, IFormFile image)
    {
        try
        {
            var userId = User.GetUserId();
            var toy = await _toyService.AddImageAsync(userId, toyId, image);
            return Ok(new { toy });
        }
        catch (KeyNotFoundException ex)        { return NotFound(new { message = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return Forbid(); }
        catch (InvalidOperationException ex)   { return BadRequest(new { message = ex.Message }); }
    }
}
