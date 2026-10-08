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
    private readonly IToyService _toyService;
    private readonly IChildService _childService;

    public ProfileController(IProfileService profileService, IToyService toyService, IChildService childService)
    {
        _profileService = profileService;
        _toyService     = toyService;
        _childService   = childService;
    }

    // ── Children ──────────────────────────────────────────────────────────────

    /// <summary>The current user's children.</summary>
    /// <remarks>GET /api/profile/children</remarks>
    [HttpGet("children")]
    public async Task<IActionResult> GetMyChildren() =>
        Ok(await _childService.GetMineAsync(User.GetUserId()));

    /// <summary>Add a child (name, birth_date yyyy-MM-dd, interests, is_visible_to_contacts).</summary>
    /// <remarks>POST /api/profile/children</remarks>
    [HttpPost("children")]
    public async Task<IActionResult> AddChild([FromBody] SaveChildRequest request) =>
        StatusCode(201, await _childService.CreateAsync(User.GetUserId(), request));

    /// <remarks>PUT /api/profile/children/{childId}</remarks>
    [HttpPut("children/{childId:int}")]
    public async Task<IActionResult> UpdateChild(int childId, [FromBody] SaveChildRequest request) =>
        Ok(await _childService.UpdateAsync(User.GetUserId(), childId, request));

    /// <remarks>DELETE /api/profile/children/{childId}</remarks>
    [HttpDelete("children/{childId:int}")]
    public async Task<IActionResult> DeleteChild(int childId)
    {
        await _childService.DeleteAsync(User.GetUserId(), childId);
        return NoContent();
    }

    /// <summary>A user's children that the caller may see (the parent must have the caller in contacts).</summary>
    /// <remarks>GET /api/profile/{userId}/children</remarks>
    [HttpGet("{userId:int}/children")]
    public async Task<IActionResult> GetUserChildren(int userId) =>
        Ok(await _childService.GetForParentAsync(User.GetUserId(), userId));

    /// <summary>Upcoming birthdays of contacts' children, soonest first.</summary>
    /// <remarks>GET /api/contacts/birthdays?days=30</remarks>
    [HttpGet("/api/contacts/birthdays")]
    public async Task<IActionResult> GetUpcomingBirthdays([FromQuery] int days = 30) =>
        Ok(await _childService.GetUpcomingBirthdaysAsync(User.GetUserId(), days));

    /// <summary>Get the authenticated user's own profile.</summary>
    /// <remarks>GET /api/profile</remarks>
    [HttpGet]
    public async Task<IActionResult> GetMe()
    {
        var userId = User.GetUserId();
        var user = await _profileService.GetMeAsync(userId);
        return Ok(user);
    }

    /// <summary>Get a user's profile. Email and phone are only included for contacts.</summary>
    /// <remarks>GET /api/profile/{userId}</remarks>
    [HttpGet("{userId:int}")]
    public async Task<IActionResult> GetById(int userId)
    {
        var user = await _profileService.GetByIdAsync(User.GetUserId(), userId);
        return Ok(user);
    }

    /// <summary>Toys owned by a user that the caller is allowed to see (listed and unlisted).</summary>
    /// <remarks>GET /api/profile/{userId}/toys</remarks>
    [HttpGet("{userId:int}/toys")]
    public async Task<IActionResult> GetUserToys(int userId)
    {
        var toys = await _toyService.GetUserToysAsync(User.GetUserId(), userId);
        return Ok(toys);
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
