using Microsoft.EntityFrameworkCore;
using ToyBoxApi.Data;
using ToyBoxApi.DTOs.Notifications;
using ToyBoxApi.Entities;

namespace ToyBoxApi.Services;

public static class NotificationTypes
{
    public const string ExchangeRequestReceived  = "exchange_request_received";
    public const string ExchangeRequestAccepted  = "exchange_request_accepted";
    public const string ExchangeRequestDeclined  = "exchange_request_declined";
    public const string ExchangeRequestCancelled = "exchange_request_cancelled";
    public const string GiftReceived             = "gift_received";
    public const string GiftAccepted             = "gift_accepted";
    public const string GiftDeclined             = "gift_declined";
    public const string GiftCancelled            = "gift_cancelled";
    public const string PriorityToyListed        = "priority_toy_listed";
    public const string LoanReturnConfirmed      = "loan_return_confirmed";
    public const string LoanReturned             = "loan_returned";
    public const string LoanDueSoon              = "loan_due_soon";
    public const string LoanOverdue              = "loan_overdue";
}

public interface INotificationService
{
    /// <summary>Queues a notification on the current DbContext. The caller saves.</summary>
    void Add(int userId, string type, string title, string? body = null,
             int? toyId = null, int? requestId = null, int? giftId = null, int? actorUserId = null);

    Task<List<NotificationDto>> GetAsync(int userId, bool unreadOnly, int page, int pageSize);
    Task<int> GetUnreadCountAsync(int userId);
    Task MarkReadAsync(int userId, int notificationId);
    Task MarkAllReadAsync(int userId);
}

public class NotificationService : INotificationService
{
    private readonly AppDbContext _db;

    public NotificationService(AppDbContext db)
    {
        _db = db;
    }

    public void Add(int userId, string type, string title, string? body = null,
                    int? toyId = null, int? requestId = null, int? giftId = null, int? actorUserId = null)
    {
        _db.Notifications.Add(new Notification
        {
            UserId      = userId,
            Type        = type,
            Title       = title.Length > 150 ? title[..150] : title,
            Body        = body is { Length: > 500 } ? body[..500] : body,
            ToyId       = toyId,
            RequestId   = requestId,
            GiftId      = giftId,
            ActorUserId = actorUserId,
            CreatedAt   = DateTime.UtcNow,
        });
    }

    public async Task<List<NotificationDto>> GetAsync(int userId, bool unreadOnly, int page, int pageSize)
    {
        page     = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.Notifications.Where(n => n.UserId == userId);
        if (unreadOnly)
            query = query.Where(n => !n.IsRead);

        return await query
            .OrderByDescending(n => n.CreatedAt)
            .ThenByDescending(n => n.NotificationId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NotificationDto
            {
                NotificationId = n.NotificationId,
                Type           = n.Type,
                Title          = n.Title,
                Body           = n.Body,
                ToyId          = n.ToyId,
                RequestId      = n.RequestId,
                GiftId         = n.GiftId,
                ActorUserId    = n.ActorUserId,
                IsRead         = n.IsRead,
                CreatedAt      = n.CreatedAt,
            })
            .ToListAsync();
    }

    public Task<int> GetUnreadCountAsync(int userId) =>
        _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);

    public async Task MarkReadAsync(int userId, int notificationId)
    {
        var n = await _db.Notifications
            .FirstOrDefaultAsync(x => x.NotificationId == notificationId && x.UserId == userId)
            ?? throw new KeyNotFoundException("Notification not found.");

        n.IsRead = true;
        await _db.SaveChangesAsync();
    }

    public Task MarkAllReadAsync(int userId) =>
        _db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
}
