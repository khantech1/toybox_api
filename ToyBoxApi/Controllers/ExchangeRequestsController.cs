using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToyBoxApi.DTOs.Exchange;
using ToyBoxApi.Helpers;
using ToyBoxApi.Services;

namespace ToyBoxApi.Controllers;

[ApiController]
[Route("api/exchange-requests")]
[Authorize]
public class ExchangeRequestsController : ControllerBase
{
    private readonly IExchangeRequestService _service;

    public ExchangeRequestsController(IExchangeRequestService service)
    {
        _service = service;
    }

    /// <summary>
    /// Get all exchange requests for the current user.
    /// Optional status filter: pending | accepted | declined | completed
    /// </summary>
    /// <remarks>GET /api/exchange-requests?status=pending</remarks>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? status)
    {
        var userId = User.GetUserId();
        var requests = await _service.GetAllAsync(userId, status);
        return Ok(requests);
    }

    /// <summary>Get a single exchange request by ID.</summary>
    /// <remarks>GET /api/exchange-requests/{requestId}</remarks>
    [HttpGet("{requestId:int}")]
    public async Task<IActionResult> GetById(int requestId)
    {
        try
        {
            var userId = User.GetUserId();
            var request = await _service.GetByIdAsync(requestId, userId);
            return Ok(request);
        }
        catch (KeyNotFoundException ex)        { return NotFound(new { message = ex.Message }); }
        catch (UnauthorizedAccessException)    { return Forbid(); }
    }

    /// <summary>Create a new exchange request.</summary>
    /// <remarks>POST /api/exchange-requests</remarks>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateExchangeRequest request)
    {
        try
        {
            var userId = User.GetUserId();
            var result = await _service.CreateAsync(userId, request);
            return StatusCode(201, new { exchange_request = result });
        }
        catch (KeyNotFoundException ex)        { return NotFound(new { message = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return Forbid(); }
        catch (InvalidOperationException ex)   { return Conflict(new { message = ex.Message }); }
    }

    /// <summary>Accept a pending exchange request (toy owner only).</summary>
    /// <remarks>PUT /api/exchange-requests/{requestId}/accept</remarks>
    [HttpPut("{requestId:int}/accept")]
    public async Task<IActionResult> Accept(int requestId)
    {
        try
        {
            var userId = User.GetUserId();
            var result = await _service.AcceptAsync(requestId, userId);
            return Ok(new { exchange_request = result });
        }
        catch (KeyNotFoundException ex)        { return NotFound(new { message = ex.Message }); }
        catch (UnauthorizedAccessException)    { return Forbid(); }
        catch (InvalidOperationException ex)   { return BadRequest(new { message = ex.Message }); }
    }

    /// <summary>Decline a pending exchange request.</summary>
    /// <remarks>PUT /api/exchange-requests/{requestId}/decline</remarks>
    [HttpPut("{requestId:int}/decline")]
    public async Task<IActionResult> Decline(int requestId)
    {
        try
        {
            var userId = User.GetUserId();
            var result = await _service.DeclineAsync(requestId, userId);
            return Ok(new { exchange_request = result });
        }
        catch (KeyNotFoundException ex)        { return NotFound(new { message = ex.Message }); }
        catch (UnauthorizedAccessException)    { return Forbid(); }
        catch (InvalidOperationException ex)   { return BadRequest(new { message = ex.Message }); }
    }
}
