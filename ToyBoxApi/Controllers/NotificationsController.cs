using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToyBoxApi.Helpers;
using ToyBoxApi.Services;

namespace ToyBoxApi.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _service;

    public NotificationsController(INotificationService service)
    {
        _service = service;
    }

    /// <remarks>GET /api/notifications?unread_only=false&amp;page=1&amp;page_size=30</remarks>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery(Name = "unread_only")] bool unreadOnly = false,
        [FromQuery] int page = 1,
        [FromQuery(Name = "page_size")] int pageSize = 30)
    {
        var items = await _service.GetAsync(User.GetUserId(), unreadOnly, page, pageSize);
        return Ok(items);
    }

    /// <remarks>GET /api/notifications/unread-count</remarks>
    [HttpGet("unread-count")]
    public async Task<IActionResult> UnreadCount()
    {
        var count = await _service.GetUnreadCountAsync(User.GetUserId());
        return Ok(new { count });
    }

    /// <remarks>PUT /api/notifications/{id}/read</remarks>
    [HttpPut("{notificationId:int}/read")]
    public async Task<IActionResult> MarkRead(int notificationId)
    {
        await _service.MarkReadAsync(User.GetUserId(), notificationId);
        return NoContent();
    }

    /// <remarks>PUT /api/notifications/read-all</remarks>
    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        await _service.MarkAllReadAsync(User.GetUserId());
        return NoContent();
    }
}
