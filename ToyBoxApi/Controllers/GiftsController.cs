using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToyBoxApi.DTOs.Gifts;
using ToyBoxApi.Helpers;
using ToyBoxApi.Services;

namespace ToyBoxApi.Controllers;

[ApiController]
[Route("api/gifts")]
[Authorize]
public class GiftsController : ControllerBase
{
    private readonly IGiftService _service;

    public GiftsController(IGiftService service)
    {
        _service = service;
    }

    /// <summary>Offer one of your toys as a gift to a contact.</summary>
    /// <remarks>POST /api/gifts</remarks>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateGiftRequest request)
    {
        var gift = await _service.CreateAsync(User.GetUserId(), request);
        return StatusCode(201, new { gift });
    }

    /// <summary>Gifts for the current user. direction: incoming | outgoing | all. status: pending | accepted | declined | cancelled.</summary>
    /// <remarks>GET /api/gifts?direction=incoming&amp;status=pending</remarks>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? direction, [FromQuery] string? status)
    {
        var gifts = await _service.GetAllAsync(User.GetUserId(), direction, status);
        return Ok(gifts);
    }

    /// <remarks>GET /api/gifts/{giftId}</remarks>
    [HttpGet("{giftId:int}")]
    public async Task<IActionResult> GetById(int giftId)
    {
        var gift = await _service.GetByIdAsync(User.GetUserId(), giftId);
        return Ok(gift);
    }

    /// <summary>Accept a gift (recipient only). Ownership transfers to you.</summary>
    /// <remarks>PUT /api/gifts/{giftId}/accept</remarks>
    [HttpPut("{giftId:int}/accept")]
    public async Task<IActionResult> Accept(int giftId)
    {
        var gift = await _service.AcceptAsync(User.GetUserId(), giftId);
        return Ok(new { gift });
    }

    /// <summary>Decline a gift (recipient only).</summary>
    /// <remarks>PUT /api/gifts/{giftId}/decline</remarks>
    [HttpPut("{giftId:int}/decline")]
    public async Task<IActionResult> Decline(int giftId)
    {
        var gift = await _service.DeclineAsync(User.GetUserId(), giftId);
        return Ok(new { gift });
    }

    /// <summary>Cancel a pending gift (giver only).</summary>
    /// <remarks>PUT /api/gifts/{giftId}/cancel</remarks>
    [HttpPut("{giftId:int}/cancel")]
    public async Task<IActionResult> Cancel(int giftId)
    {
        var gift = await _service.CancelAsync(User.GetUserId(), giftId);
        return Ok(new { gift });
    }
}
