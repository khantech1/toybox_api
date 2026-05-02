using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToyBoxApi.DTOs.Profile;
using ToyBoxApi.Helpers;
using ToyBoxApi.Services;

namespace ToyBoxApi.Controllers;

[ApiController]
[Route("api/profile")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IProfileService _profileService;

    public ProfileController(IProfileService profileService)
    {
        _profileService = profileService;
    }

    /// <summary>Get the authenticated user's own profile.</summary>
    /// <remarks>GET /api/profile</remarks>
    [HttpGet]
    public async Task<IActionResult> GetMe()
    {
        var userId = User.GetUserId();
        var user = await _profileService.GetMeAsync(userId);
        return Ok(user);
    }

    /// <summary>Get any user's public profile by ID.</summary>
    /// <remarks>GET /api/profile/{userId}</remarks>
    [HttpGet("{userId:int}")]
    public async Task<IActionResult> GetById(int userId)
    {
        try
        {
            var user = await _profileService.GetByIdAsync(userId);
            return Ok(user);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>Update the current user's profile (name, address).</summary>
    /// <remarks>PUT /api/profile</remarks>
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateProfileRequest request)
    {
        try
        {
            var userId = User.GetUserId();
            var user = await _profileService.UpdateAsync(userId, request);
            return Ok(new { user });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>Upload or replace the current user's profile picture.</summary>
    /// <remarks>POST /api/profile/photo — multipart/form-data, field: "profile_pic"</remarks>
    [HttpPost("photo")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadPhoto(IFormFile profile_pic)
    {
        try
        {
            var userId = User.GetUserId();
            var user = await _profileService.UploadProfilePicAsync(userId, profile_pic);
            return Ok(new { user });
        }
        catch (KeyNotFoundException ex)      { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    // ── Contacts ──────────────────────────────────────────────────────────────

    /// <summary>Get the current user's contacts list.</summary>
    /// <remarks>GET /api/contacts</remarks>
    [HttpGet("/api/contacts")]
    public async Task<IActionResult> GetContacts()
    {
        var userId = User.GetUserId();
        var contacts = await _profileService.GetContactsAsync(userId);
        return Ok(contacts);
    }

    /// <summary>Add a user to contacts.</summary>
    /// <remarks>POST /api/contacts</remarks>
    [HttpPost("/api/contacts")]
    public async Task<IActionResult> AddContact([FromBody] AddContactRequest request)
    {
        try
        {
            var userId = User.GetUserId();
            await _profileService.AddContactAsync(userId, request.ContactId);
            return StatusCode(201, new { message = "Contact added." });
        }
        catch (KeyNotFoundException ex)      { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    /// <summary>Remove a contact.</summary>
    /// <remarks>DELETE /api/contacts/{contactId}</remarks>
    [HttpDelete("/api/contacts/{contactId:int}")]
    public async Task<IActionResult> RemoveContact(int contactId)
    {
        try
        {
            var userId = User.GetUserId();
            await _profileService.RemoveContactAsync(userId, contactId);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("/api/contacts/sync")]
    public async Task<IActionResult> SyncContacts([FromBody] SyncContactsRequest request)
    {
        var userId = User.GetUserId();
        var contacts = await _profileService.SyncContactsAsync(userId, request);
        return Ok(contacts);
    }
}
