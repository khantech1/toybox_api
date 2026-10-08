using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToyBoxApi.DTOs.Exchange;
using ToyBoxApi.Helpers;
using ToyBoxApi.Services;

namespace ToyBoxApi.Controllers;

// Errors (not found / forbidden / invalid) are mapped by ExceptionMiddleware.
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
    /// Exchange requests for the current user.
    /// status: pending | declined | completed | active_loan | returned (comma-separated allowed).
    /// direction: incoming | outgoing | all (defaults to incoming for status=pending, all otherwise).
    /// </summary>
    /// <remarks>GET /api/exchange-requests?status=pending&amp;direction=outgoing</remarks>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? status, [FromQuery] string? direction)
    {
        var requests = await _service.GetAllAsync(User.GetUserId(), status, direction);
        return Ok(requests);
    }

    /// <summary>Get a single exchange request by ID.</summary>
    /// <remarks>GET /api/exchange-requests/{requestId}</remarks>
    [HttpGet("{requestId:int}")]
    public async Task<IActionResult> GetById(int requestId)
    {
        var request = await _service.GetByIdAsync(requestId, User.GetUserId());
        return Ok(request);
    }

    /// <summary>Create a permanent or temporary exchange request.</summary>
    /// <remarks>POST /api/exchange-requests</remarks>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateExchangeRequest request)
    {
        var result = await _service.CreateAsync(User.GetUserId(), request);
        return StatusCode(201, new { exchange_request = result });
    }

    /// <summary>Accept a pending exchange request (receiver only).</summary>
    /// <remarks>PUT /api/exchange-requests/{requestId}/accept</remarks>
    [HttpPut("{requestId:int}/accept")]
    public async Task<IActionResult> Accept(int requestId)
    {
        var result = await _service.AcceptAsync(requestId, User.GetUserId());
        return Ok(new { exchange_request = result });
    }

    /// <summary>Decline (receiver) or cancel (initiator) a pending exchange request.</summary>
    /// <remarks>PUT /api/exchange-requests/{requestId}/decline</remarks>
    [HttpPut("{requestId:int}/decline")]
    public async Task<IActionResult> Decline(int requestId)
    {
        var result = await _service.DeclineAsync(requestId, User.GetUserId());
        return Ok(new { exchange_request = result });
    }

    /// <summary>Confirm the toys of a temporary exchange were returned. Completes once both sides confirm.</summary>
    /// <remarks>PUT /api/exchange-requests/{requestId}/confirm-return</remarks>
    [HttpPut("{requestId:int}/confirm-return")]
    public async Task<IActionResult> ConfirmReturn(int requestId)
    {
        var result = await _service.ConfirmReturnAsync(requestId, User.GetUserId());
        return Ok(new { exchange_request = result });
    }
}
