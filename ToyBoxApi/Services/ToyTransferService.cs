using Microsoft.EntityFrameworkCore;
using ToyBoxApi.Data;
using ToyBoxApi.Entities;

namespace ToyBoxApi.Services;

/// <summary>
/// Single place for ownership changes and the locks around them, so exchange
/// and gift flows stay consistent. Methods only stage changes; callers save.
/// </summary>
public interface IToyTransferService
{
    Task TransferAsync(Toy toy, int newOwnerId, string historyType, int actorUserId,
                       int? requestId = null, int? giftId = null);

    Task EnsureTransferableAsync(Toy toy, int? excludeGiftId = null);

    Task DeclineCompetingRequestsAsync(IEnumerable<int> toyIds, int exceptRequestId, int actorUserId);

    Task CancelPendingGiftsAsync(int toyId, int? exceptGiftId, int actorUserId);
}

public class ToyTransferService : IToyTransferService
{
    private readonly AppDbContext _db;
    private readonly INotificationService _notifications;

    public ToyTransferService(AppDbContext db, INotificationService notifications)
    {
        _db            = db;
        _notifications = notifications;
    }

    public async Task TransferAsync(Toy toy, int newOwnerId, string historyType, int actorUserId,
                                    int? requestId = null, int? giftId = null)
    {
        var now = DateTime.UtcNow;

        var openRows = await _db.ToyOwnershipHistory
            .Where(h => h.ToyId == toy.ToyId && h.EndedAt == null)
            .ToListAsync();

        foreach (var row in openRows)
            row.EndedAt = now;

        _db.ToyOwnershipHistory.Add(new ToyOwnershipHistory
        {
            ToyId     = toy.ToyId,
            UserId    = newOwnerId,
            Type      = historyType,
            RequestId = requestId,
            GiftId    = giftId,
            StartedAt = now,
        });

        toy.OwnerUserId         = newOwnerId;
        toy.CurrentHolderUserId = newOwnerId;
        // A received toy lands in the new owner's toys but is NOT listed for exchange.
        toy.IsListed            = false;

        _db.SharedToys.RemoveRange(
            await _db.SharedToys.Where(s => s.ToyId == toy.ToyId).ToListAsync());

        _db.ToyPriorities.RemoveRange(
            await _db.ToyPriorities.Where(p => p.ToyId == toy.ToyId).ToListAsync());

        await CancelPendingGiftsAsync(toy.ToyId, giftId, actorUserId);
        await DeclineCompetingRequestsAsync(new[] { toy.ToyId }, requestId ?? 0, actorUserId);
    }

    public async Task EnsureTransferableAsync(Toy toy, int? excludeGiftId = null)
    {
        if (toy.IsOnLoan)
            throw new InvalidOperationException($"\"{toy.ToyName}\" is currently on loan.");

        var hasPendingGift = await _db.ToyGifts.AnyAsync(g =>
            g.ToyId == toy.ToyId &&
            g.Status == "pending" &&
            (excludeGiftId == null || g.GiftId != excludeGiftId));

        if (hasPendingGift)
            throw new InvalidOperationException($"\"{toy.ToyName}\" has a pending gift.");
    }

    public async Task DeclineCompetingRequestsAsync(IEnumerable<int> toyIds, int exceptRequestId, int actorUserId)
    {
        var ids = toyIds.ToList();

        var competing = await _db.ExchangeRequests
            .Where(r =>
                r.RequestId != exceptRequestId &&
                r.Status == ExchangeStatuses.Pending &&
                r.Toys.Any(rt => ids.Contains(rt.ToyId)))
            .ToListAsync();

        foreach (var r in competing)
        {
            // Tracked instances may already be declined earlier in this unit of work.
            if (r.Status != ExchangeStatuses.Pending)
                continue;

            r.Status = ExchangeStatuses.Declined;

            foreach (var party in new[] { r.InitiatorUserId, r.ReceiverUserId })
            {
                if (party is null || party == actorUserId)
                    continue;

                _notifications.Add(party.Value, NotificationTypes.ExchangeRequestCancelled,
                    "Exchange request closed",
                    "A toy in this exchange request is no longer available.",
                    requestId: r.RequestId, actorUserId: actorUserId);
            }
        }
    }

    public async Task CancelPendingGiftsAsync(int toyId, int? exceptGiftId, int actorUserId)
    {
        var gifts = await _db.ToyGifts
            .Where(g =>
                g.ToyId == toyId &&
                g.Status == "pending" &&
                (exceptGiftId == null || g.GiftId != exceptGiftId))
            .ToListAsync();

        foreach (var g in gifts)
        {
            if (g.Status != "pending")
                continue;

            g.Status      = "cancelled";
            g.RespondedAt = DateTime.UtcNow;

            if (g.ToUserId != actorUserId)
                _notifications.Add(g.ToUserId, NotificationTypes.GiftCancelled,
                    "Gift cancelled", "A gift offered to you is no longer available.",
                    toyId: toyId, giftId: g.GiftId, actorUserId: actorUserId);
        }
    }
}

public static class ToyVisibility
{
    /// <summary>
    /// Toys the viewer may see (catalog + contact profile): not their own, and either
    /// shared with them explicitly, or shared with nobody and the owner is in the viewer's contacts.
    /// </summary>
    public static IQueryable<Toy> VisibleTo(this IQueryable<Toy> query, AppDbContext db, int viewerId) =>
        query.Where(t =>
            t.OwnerUserId != viewerId &&
            (t.SharedWith.Any(s => s.SharedWithUserId == viewerId) ||
             (!t.SharedWith.Any() &&
              db.Contacts.Any(c => c.UserId == viewerId && c.ContactId == t.OwnerUserId))));
}
