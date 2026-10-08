using Microsoft.EntityFrameworkCore;
using ToyBoxApi.Data;
using ToyBoxApi.DTOs.Gifts;
using ToyBoxApi.Entities;

namespace ToyBoxApi.Services;

public interface IGiftService
{
    Task<GiftDto> CreateAsync(int userId, CreateGiftRequest request);
    Task<List<GiftDto>> GetAllAsync(int userId, string? direction, string? status);
    Task<GiftDto> GetByIdAsync(int userId, int giftId);
    Task<GiftDto> AcceptAsync(int userId, int giftId);
    Task<GiftDto> DeclineAsync(int userId, int giftId);
    Task<GiftDto> CancelAsync(int userId, int giftId);
}

public class GiftService : IGiftService
{
    private readonly AppDbContext _db;
    private readonly IToyTransferService _transfers;
    private readonly INotificationService _notifications;
    private readonly IChildService _children;

    public GiftService(
        AppDbContext db,
        IToyTransferService transfers,
        INotificationService notifications,
        IChildService children)
    {
        _db            = db;
        _transfers     = transfers;
        _notifications = notifications;
        _children      = children;
    }

    public async Task<GiftDto> CreateAsync(int userId, CreateGiftRequest req)
    {
        if (req.ToUserId == userId)
            throw new InvalidOperationException("You cannot gift a toy to yourself.");

        var toy = await _db.Toys.FirstOrDefaultAsync(t => t.ToyId == req.ToyId)
            ?? throw new KeyNotFoundException("Toy not found.");

        if (toy.OwnerUserId != userId)
            throw new UnauthorizedAccessException("You do not own this toy.");

        await _transfers.EnsureTransferableAsync(toy);

        var isContact = await _db.Contacts.AnyAsync(c => c.UserId == userId && c.ContactId == req.ToUserId);
        if (!isContact)
            throw new InvalidOperationException("You can only gift toys to people in your contacts.");

        Child? child = null;
        if (req.ChildId is int childId)
        {
            child = await _children.FindVisibleAsync(userId, childId);
            if (child is null || child.ParentUserId != req.ToUserId)
                throw new InvalidOperationException("That child isn't available for this gift.");
        }

        var gift = new ToyGift
        {
            ToyId      = toy.ToyId,
            FromUserId = userId,
            ToUserId   = req.ToUserId,
            Status     = "pending",
            Message    = req.Message?.Trim(),
            ChildId    = child?.ChildId,
            ChildName  = child?.Name,
            CreatedAt  = DateTime.UtcNow,
        };

        // A toy being gifted leaves the exchange catalog while the gift is pending.
        toy.IsListed = false;

        _db.ToyGifts.Add(gift);
        await _db.SaveChangesAsync();

        var fromName = await UserNameAsync(userId);
        _notifications.Add(req.ToUserId, NotificationTypes.GiftReceived,
            child is null ? "You've been offered a gift" : $"Birthday gift for {child.Name}",
            child is null
                ? $"{fromName} wants to gift you \"{toy.ToyName}\"."
                : $"{fromName} wants to gift \"{toy.ToyName}\" to {child.Name}.",
            toyId: toy.ToyId, giftId: gift.GiftId, actorUserId: userId);
        await _db.SaveChangesAsync();

        return await GetByIdAsync(userId, gift.GiftId);
    }

    public async Task<List<GiftDto>> GetAllAsync(int userId, string? direction, string? status)
    {
        var query = GiftsWithDetails();

        query = direction?.ToLowerInvariant() switch
        {
            "incoming" => query.Where(g => g.ToUserId == userId),
            "outgoing" => query.Where(g => g.FromUserId == userId),
            _          => query.Where(g => g.ToUserId == userId || g.FromUserId == userId),
        };

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(g => g.Status == status.ToLower());

        var gifts = await query.OrderByDescending(g => g.CreatedAt).ToListAsync();
        return gifts.Select(MapToDto).ToList();
    }

    public async Task<GiftDto> GetByIdAsync(int userId, int giftId)
    {
        var gift = await GiftsWithDetails().FirstOrDefaultAsync(g => g.GiftId == giftId)
            ?? throw new KeyNotFoundException("Gift not found.");

        if (gift.FromUserId != userId && gift.ToUserId != userId)
            throw new UnauthorizedAccessException("Access denied.");

        return MapToDto(gift);
    }

    public async Task<GiftDto> AcceptAsync(int userId, int giftId)
    {
        var gift = await _db.ToyGifts
            .Include(g => g.Toy)
            .FirstOrDefaultAsync(g => g.GiftId == giftId)
            ?? throw new KeyNotFoundException("Gift not found.");

        if (gift.ToUserId != userId)
            throw new UnauthorizedAccessException("Only the recipient can accept this gift.");

        EnsurePending(gift);

        var toy = gift.Toy!;
        if (toy.OwnerUserId != gift.FromUserId)
            throw new InvalidOperationException("This toy is no longer available.");

        await _transfers.EnsureTransferableAsync(toy, excludeGiftId: giftId);

        gift.Status      = "accepted";
        gift.RespondedAt = DateTime.UtcNow;

        await _transfers.TransferAsync(toy, userId, HistoryTypes.Gift, userId, giftId: giftId);

        var toName = await UserNameAsync(userId);
        _notifications.Add(gift.FromUserId, NotificationTypes.GiftAccepted,
            "Gift accepted", $"{toName} accepted your gift \"{toy.ToyName}\".",
            toyId: toy.ToyId, giftId: giftId, actorUserId: userId);

        await _db.SaveChangesAsync();
        return await GetByIdAsync(userId, giftId);
    }

    public async Task<GiftDto> DeclineAsync(int userId, int giftId)
    {
        var gift = await _db.ToyGifts.Include(g => g.Toy).FirstOrDefaultAsync(g => g.GiftId == giftId)
            ?? throw new KeyNotFoundException("Gift not found.");

        if (gift.ToUserId != userId)
            throw new UnauthorizedAccessException("Only the recipient can decline this gift.");

        EnsurePending(gift);

        gift.Status      = "declined";
        gift.RespondedAt = DateTime.UtcNow;

        var toName = await UserNameAsync(userId);
        _notifications.Add(gift.FromUserId, NotificationTypes.GiftDeclined,
            "Gift declined", $"{toName} declined your gift \"{gift.Toy?.ToyName}\".",
            toyId: gift.ToyId, giftId: giftId, actorUserId: userId);

        await _db.SaveChangesAsync();
        return await GetByIdAsync(userId, giftId);
    }

    public async Task<GiftDto> CancelAsync(int userId, int giftId)
    {
        var gift = await _db.ToyGifts.Include(g => g.Toy).FirstOrDefaultAsync(g => g.GiftId == giftId)
            ?? throw new KeyNotFoundException("Gift not found.");

        if (gift.FromUserId != userId)
            throw new UnauthorizedAccessException("Only the giver can cancel this gift.");

        EnsurePending(gift);

        gift.Status      = "cancelled";
        gift.RespondedAt = DateTime.UtcNow;

        var fromName = await UserNameAsync(userId);
        _notifications.Add(gift.ToUserId, NotificationTypes.GiftCancelled,
            "Gift cancelled", $"{fromName} cancelled the gift \"{gift.Toy?.ToyName}\".",
            toyId: gift.ToyId, giftId: giftId, actorUserId: userId);

        await _db.SaveChangesAsync();
        return await GetByIdAsync(userId, giftId);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void EnsurePending(ToyGift gift)
    {
        if (gift.Status != "pending")
            throw new InvalidOperationException($"This gift is already {gift.Status}.");
    }

    private async Task<string> UserNameAsync(int userId) =>
        await _db.Users.Where(u => u.UserId == userId).Select(u => u.Name).FirstOrDefaultAsync()
        ?? "Someone";

    private IQueryable<ToyGift> GiftsWithDetails() => _db.ToyGifts
        .Include(g => g.FromUser)
        .Include(g => g.ToUser)
        .Include(g => g.Toy).ThenInclude(t => t!.Images)
        .Include(g => g.Toy).ThenInclude(t => t!.Category)
        .Include(g => g.Toy).ThenInclude(t => t!.Owner)
        .AsSplitQuery();

    private static GiftDto MapToDto(ToyGift g) => new()
    {
        GiftId      = g.GiftId,
        ToyId       = g.ToyId,
        FromUserId  = g.FromUserId,
        ToUserId    = g.ToUserId,
        Status      = g.Status,
        Message     = g.Message,
        ChildId     = g.ChildId,
        ChildName   = g.ChildName,
        CreatedAt   = g.CreatedAt,
        RespondedAt = g.RespondedAt,
        Toy         = g.Toy is null ? null : ToyService.MapToDto(g.Toy),
        FromUser    = g.FromUser is null ? null : ToyService.MapOwner(g.FromUser),
        ToUser      = g.ToUser is null ? null : ToyService.MapOwner(g.ToUser),
    };
}
