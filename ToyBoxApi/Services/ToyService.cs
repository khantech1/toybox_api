using Microsoft.EntityFrameworkCore;
using ToyBoxApi.Data;
using ToyBoxApi.DTOs.Toys;
using ToyBoxApi.Entities;

namespace ToyBoxApi.Services;

public interface IToyService
{
    Task<List<ToyDto>> GetAllAsync(int currentUserId, string? search, int? categoryId, string? ageGroup);
    Task<ToyDto> GetByIdAsync(int toyId, int currentUserId);
    Task<List<ToyDto>> GetMyToysAsync(int userId);
    Task<List<ToyDto>> GetUserToysAsync(int viewerId, int ownerId);
    Task<ToyDto> CreateAsync(int userId, CreateToyRequest request);
    Task<ToyDto> UpdateAsync(int userId, int toyId, UpdateToyRequest request);
    Task<ToyDto> SetListedAsync(int userId, int toyId, bool isListed);
    Task DeleteAsync(int userId, int toyId);
    Task<ToyDto> AddImageAsync(int userId, int toyId, IFormFile file);
    Task<List<OwnershipHistoryDto>> GetHistoryAsync(int userId, int toyId);
    Task AddPriorityAsync(int userId, int toyId);
    Task RemovePriorityAsync(int userId, int toyId);
    Task<List<PriorityQueueEntryDto>> GetPriorityQueueAsync(int userId, int toyId);
}

public class ToyService : IToyService
{
    private readonly AppDbContext _db;
    private readonly IFileService _fileService;
    private readonly IToyTransferService _transfers;
    private readonly INotificationService _notifications;

    public ToyService(
        AppDbContext db,
        IFileService fileService,
        IToyTransferService transfers,
        INotificationService notifications)
    {
        _db            = db;
        _fileService   = fileService;
        _transfers     = transfers;
        _notifications = notifications;
    }

    private IQueryable<Toy> ToysWithDetails() => _db.Toys
        .Include(t => t.Owner)
        .Include(t => t.CurrentHolder)
        .Include(t => t.Category)
        .Include(t => t.DesiredCategory)
        .Include(t => t.Images);

    public async Task<List<ToyDto>> GetAllAsync(
        int currentUserId, string? search, int? categoryId, string? ageGroup)
    {
        var query = ToysWithDetails()
            .VisibleTo(_db, currentUserId)
            .Where(t => t.IsListed && t.CurrentHolderUserId == t.OwnerUserId);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(t =>
                t.ToyName.Contains(search) ||
                (t.ToyDescription != null && t.ToyDescription.Contains(search)));

        if (categoryId.HasValue)
            query = query.Where(t => t.CategoryId == categoryId.Value);

        var toys = await query
            .OrderByDescending(t => t.ToyId)
            .ToListAsync();

        var dtos = toys.Select(MapToDto).ToList();
        await FillPriorityInfoAsync(dtos, currentUserId);
        return dtos;
    }

    public async Task<ToyDto> GetByIdAsync(int toyId, int currentUserId)
    {
        var toy = await ToysWithDetails()
            .FirstOrDefaultAsync(t => t.ToyId == toyId)
            ?? throw new KeyNotFoundException("Toy not found.");

        if (!await CanViewAsync(toy, currentUserId))
            throw new UnauthorizedAccessException("You cannot view this toy.");

        var dto = MapToDto(toy);
        await FillPriorityInfoAsync(new List<ToyDto> { dto }, currentUserId);

        if (toy.OwnerUserId == currentUserId)
            dto.HasPendingGift = await _db.ToyGifts.AnyAsync(g => g.ToyId == toyId && g.Status == "pending");

        return dto;
    }

    public async Task<List<ToyDto>> GetMyToysAsync(int userId)
    {
        // Owned toys plus toys the user is currently borrowing.
        var toys = await ToysWithDetails()
            .Where(t => t.OwnerUserId == userId || t.CurrentHolderUserId == userId)
            .OrderByDescending(t => t.ToyId)
            .ToListAsync();

        return toys.Select(MapToDto).ToList();
    }

    public async Task<List<ToyDto>> GetUserToysAsync(int viewerId, int ownerId)
    {
        if (viewerId == ownerId)
            return await GetMyToysAsync(viewerId);

        var toys = await ToysWithDetails()
            .Where(t => t.OwnerUserId == ownerId)
            .VisibleTo(_db, viewerId)
            .OrderByDescending(t => t.ToyId)
            .ToListAsync();

        var dtos = toys.Select(MapToDto).ToList();
        await FillPriorityInfoAsync(dtos, viewerId);
        return dtos;
    }

    public async Task<ToyDto> CreateAsync(int userId, CreateToyRequest request)
    {
        var toy = new Toy
        {
            OwnerUserId         = userId,
            CurrentHolderUserId = userId,
            ToyName             = request.ToyName.Trim(),
            ToyDescription      = request.ToyDescription?.Trim(),
            CategoryId          = request.CategoryId,
            DesiredCategoryId   = request.DesiredCategoryId,
            ConditionStatus     = request.ConditionStatus,
            Value               = request.Value,
            IsListed            = request.IsListed,
        };

        if (!request.VisibleToAll && request.VisibleToUserIds?.Any() == true)
        {
            foreach (var contactId in request.VisibleToUserIds.Distinct())
                toy.SharedWith.Add(new SharedToy { SharedWithUserId = contactId });
        }

        toy.History.Add(new ToyOwnershipHistory
        {
            UserId    = userId,
            Type      = HistoryTypes.Created,
            StartedAt = DateTime.UtcNow,
        });

        _db.Toys.Add(toy);
        await _db.SaveChangesAsync();

        return await GetByIdAsync(toy.ToyId, userId);
    }

    public async Task<ToyDto> UpdateAsync(int userId, int toyId, UpdateToyRequest request)
    {
        var toy = await _db.Toys
            .FirstOrDefaultAsync(t => t.ToyId == toyId)
            ?? throw new KeyNotFoundException("Toy not found.");

        if (toy.OwnerUserId != userId)
            throw new UnauthorizedAccessException("You do not own this toy.");

        if (request.ToyName is not null)
            toy.ToyName = request.ToyName.Trim();

        if (request.ToyDescription is not null)
            toy.ToyDescription = request.ToyDescription.Trim();

        if (request.CategoryId.HasValue)
            toy.CategoryId = request.CategoryId;

        if (request.DesiredCategoryId.HasValue)
            toy.DesiredCategoryId = request.DesiredCategoryId;

        if (request.ConditionStatus.HasValue)
            toy.ConditionStatus = request.ConditionStatus;

        if (request.Value.HasValue)
            toy.Value = request.Value;

        if (request.VisibleToAll.HasValue)
        {
            var oldShared = await _db.SharedToys
                .Where(s => s.ToyId == toyId)
                .ToListAsync();

            _db.SharedToys.RemoveRange(oldShared);

            if (!request.VisibleToAll.Value)
            {
                if (request.VisibleToUserIds == null || !request.VisibleToUserIds.Any())
                    throw new InvalidOperationException("Please select at least one contact.");

                foreach (var contactId in request.VisibleToUserIds.Distinct())
                {
                    _db.SharedToys.Add(new SharedToy
                    {
                        ToyId = toyId,
                        SharedWithUserId = contactId
                    });
                }
            }
        }

        // Save visibility first so priority notifications respect the new audience.
        await _db.SaveChangesAsync();

        if (request.IsListed.HasValue && request.IsListed.Value != toy.IsListed)
        {
            await ApplyListingAsync(toy, request.IsListed.Value, userId);
            await _db.SaveChangesAsync();
        }

        return await GetByIdAsync(toyId, userId);
    }

    public async Task<ToyDto> SetListedAsync(int userId, int toyId, bool isListed)
    {
        var toy = await _db.Toys.FirstOrDefaultAsync(t => t.ToyId == toyId)
            ?? throw new KeyNotFoundException("Toy not found.");

        if (toy.OwnerUserId != userId)
            throw new UnauthorizedAccessException("You do not own this toy.");

        if (toy.IsListed != isListed)
        {
            await ApplyListingAsync(toy, isListed, userId);
            await _db.SaveChangesAsync();
        }

        return await GetByIdAsync(toyId, userId);
    }

    private async Task ApplyListingAsync(Toy toy, bool isListed, int actorUserId)
    {
        if (!isListed)
        {
            toy.IsListed = false;
            return;
        }

        await _transfers.EnsureTransferableAsync(toy);
        toy.IsListed = true;

        // Notify everyone who queued interest and can actually see the toy.
        var visibleIds = await _db.ToyPriorities
            .Where(p => p.ToyId == toy.ToyId)
            .Where(p =>
                p.Toy!.SharedWith.Any(s => s.SharedWithUserId == p.UserId) ||
                (!p.Toy.SharedWith.Any() &&
                 _db.Contacts.Any(c => c.UserId == p.UserId && c.ContactId == p.Toy.OwnerUserId)))
            .Select(p => p.UserId)
            .ToListAsync();

        foreach (var uid in visibleIds)
        {
            _notifications.Add(uid, NotificationTypes.PriorityToyListed,
                "A toy you want is now listed",
                $"\"{toy.ToyName}\" is now available for exchange.",
                toyId: toy.ToyId, actorUserId: actorUserId);
        }
    }

    public async Task DeleteAsync(int userId, int toyId)
    {
        var toy = await _db.Toys.FindAsync(toyId)
            ?? throw new KeyNotFoundException("Toy not found.");

        if (toy.OwnerUserId != userId)
            throw new UnauthorizedAccessException("You do not own this toy.");

        if (toy.IsOnLoan)
            throw new InvalidOperationException("This toy is on loan and cannot be deleted.");

        if (await _db.ToyGifts.AnyAsync(g => g.ToyId == toyId && g.Status == "pending"))
            throw new InvalidOperationException("Cancel the pending gift for this toy before deleting it.");

        await _transfers.DeclineCompetingRequestsAsync(new[] { toyId }, 0, userId);

        var images = await _db.ToyImages
            .Where(i => i.ToyId == toyId)
            .ToListAsync();

        foreach (var img in images)
            _fileService.DeleteFile(img.ImageUrl);

        _db.ToyImages.RemoveRange(images);

        var exchangeRequestToys = await _db.ExchangeRequestToys
            .Where(e => e.ToyId == toyId)
            .ToListAsync();

        _db.ExchangeRequestToys.RemoveRange(exchangeRequestToys);

        // SharedToys, history, priorities and gifts cascade with the toy.
        _db.Toys.Remove(toy);

        await _db.SaveChangesAsync();
    }

    public async Task<ToyDto> AddImageAsync(int userId, int toyId, IFormFile file)
    {
        var toy = await _db.Toys.FindAsync(toyId)
            ?? throw new KeyNotFoundException("Toy not found.");

        if (toy.OwnerUserId != userId)
            throw new UnauthorizedAccessException("You do not own this toy.");

        var imageCount = await _db.ToyImages.CountAsync(i => i.ToyId == toyId);
        if (imageCount >= 5)
            throw new InvalidOperationException("A toy can have at most 5 images.");

        var imageUrl = await _fileService.SaveFileAsync(file, "toys");

        _db.ToyImages.Add(new ToyImage { ToyId = toyId, ImageUrl = imageUrl });
        await _db.SaveChangesAsync();

        return await GetByIdAsync(toyId, userId);
    }

    // ── History ───────────────────────────────────────────────────────────────

    public async Task<List<OwnershipHistoryDto>> GetHistoryAsync(int userId, int toyId)
    {
        var toy = await _db.Toys.FirstOrDefaultAsync(t => t.ToyId == toyId)
            ?? throw new KeyNotFoundException("Toy not found.");

        if (!await CanViewAsync(toy, userId))
            throw new UnauthorizedAccessException("You cannot view this toy.");

        var rows = await _db.ToyOwnershipHistory
            .Include(h => h.User)
            .Where(h => h.ToyId == toyId)
            .OrderBy(h => h.StartedAt)
            .ThenBy(h => h.HistoryId)
            .ToListAsync();

        return rows.Select(h => new OwnershipHistoryDto
        {
            HistoryId = h.HistoryId,
            Type      = h.Type,
            User      = h.User is null ? null : MapOwner(h.User),
            RequestId = h.RequestId,
            GiftId    = h.GiftId,
            StartedAt = h.StartedAt,
            EndedAt   = h.EndedAt,
        }).ToList();
    }

    // ── Priority queue ────────────────────────────────────────────────────────

    public async Task AddPriorityAsync(int userId, int toyId)
    {
        var visible = await _db.Toys
            .Where(t => t.ToyId == toyId)
            .VisibleTo(_db, userId)
            .AnyAsync();

        if (!visible)
        {
            var exists = await _db.Toys.AnyAsync(t => t.ToyId == toyId);
            if (!exists) throw new KeyNotFoundException("Toy not found.");
            throw new InvalidOperationException("You can only prioritise toys shared with you.");
        }

        var already = await _db.ToyPriorities.AnyAsync(p => p.ToyId == toyId && p.UserId == userId);
        if (already)
            return;

        _db.ToyPriorities.Add(new ToyPriority
        {
            ToyId     = toyId,
            UserId    = userId,
            CreatedAt = DateTime.UtcNow,
        });

        await _db.SaveChangesAsync();
    }

    public async Task RemovePriorityAsync(int userId, int toyId)
    {
        var entry = await _db.ToyPriorities
            .FirstOrDefaultAsync(p => p.ToyId == toyId && p.UserId == userId);

        if (entry is null)
            return;

        _db.ToyPriorities.Remove(entry);
        await _db.SaveChangesAsync();
    }

    public async Task<List<PriorityQueueEntryDto>> GetPriorityQueueAsync(int userId, int toyId)
    {
        var toy = await _db.Toys.FirstOrDefaultAsync(t => t.ToyId == toyId)
            ?? throw new KeyNotFoundException("Toy not found.");

        if (toy.OwnerUserId != userId)
            throw new UnauthorizedAccessException("Only the owner can see the priority queue.");

        var entries = await _db.ToyPriorities
            .Include(p => p.User)
            .Where(p => p.ToyId == toyId)
            .OrderBy(p => p.CreatedAt)
            .ThenBy(p => p.UserId)
            .ToListAsync();

        var ownerContacts = await _db.Contacts
            .Where(c => c.UserId == userId)
            .Select(c => c.ContactId)
            .ToListAsync();

        return entries.Select((p, i) => new PriorityQueueEntryDto
        {
            Position  = i + 1,
            User      = MapOwner(p.User!),
            CreatedAt = p.CreatedAt,
            IsContact = ownerContacts.Contains(p.UserId),
        }).ToList();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<bool> CanViewAsync(Toy toy, int userId)
    {
        if (toy.OwnerUserId == userId || toy.CurrentHolderUserId == userId)
            return true;

        if (await _db.Toys.Where(t => t.ToyId == toy.ToyId).VisibleTo(_db, userId).AnyAsync())
            return true;

        // Participants in an exchange or gift, and past owners/holders, may still view it.
        return await _db.ExchangeRequestToys.AnyAsync(rt =>
                   rt.ToyId == toy.ToyId &&
                   (rt.ExchangeRequest!.InitiatorUserId == userId ||
                    rt.ExchangeRequest.ReceiverUserId == userId))
            || await _db.ToyGifts.AnyAsync(g =>
                   g.ToyId == toy.ToyId && (g.FromUserId == userId || g.ToUserId == userId))
            || await _db.ToyOwnershipHistory.AnyAsync(h =>
                   h.ToyId == toy.ToyId && h.UserId == userId);
    }

    private async Task FillPriorityInfoAsync(List<ToyDto> dtos, int userId)
    {
        if (dtos.Count == 0)
            return;

        var ids = dtos.Select(d => d.ToyId).ToList();

        var counts = await _db.ToyPriorities
            .Where(p => ids.Contains(p.ToyId))
            .GroupBy(p => p.ToyId)
            .Select(g => new { ToyId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ToyId, x => x.Count);

        var mine = (await _db.ToyPriorities
            .Where(p => ids.Contains(p.ToyId) && p.UserId == userId)
            .Select(p => p.ToyId)
            .ToListAsync()).ToHashSet();

        foreach (var dto in dtos)
        {
            dto.PriorityCount     = counts.GetValueOrDefault(dto.ToyId);
            dto.IsPrioritizedByMe = mine.Contains(dto.ToyId);
        }
    }

    // ── Mapper ────────────────────────────────────────────────────────────────

    public static ToyOwnerDto MapOwner(User u) => new()
    {
        UserId     = u.UserId,
        Name       = u.Name,
        ProfilePic = u.ProfilePic,
        Rating     = u.Rating,
        Address    = u.Address,
    };

    public static ToyDto MapToDto(Toy t) => new()
    {
        ToyId               = t.ToyId,
        OwnerUserId         = t.OwnerUserId,
        ToyName             = t.ToyName,
        ToyDescription      = t.ToyDescription,
        CategoryId          = t.CategoryId,
        DesiredCategoryId   = t.DesiredCategoryId,
        ConditionStatus     = t.ConditionStatus,
        Value               = t.Value,
        IsListed            = t.IsListed,
        CurrentHolderUserId = t.CurrentHolderUserId,
        IsOnLoan            = t.IsOnLoan,
        Owner               = t.Owner is null ? null : MapOwner(t.Owner),
        CurrentHolder       = t.IsOnLoan && t.CurrentHolder is not null ? MapOwner(t.CurrentHolder) : null,
        Category = t.Category is null ? null : new CategoryDto
        {
            CategoryId   = t.Category.CategoryId,
            CategoryName = t.Category.CategoryName,
        },
        DesiredCategory = t.DesiredCategory is null ? null : new CategoryDto
        {
            CategoryId   = t.DesiredCategory.CategoryId,
            CategoryName = t.DesiredCategory.CategoryName,
        },
        Images = t.Images.Select(i => new ToyImageDto
        {
            ImageId  = i.ImageId,
            ToyId    = i.ToyId,
            ImageUrl = i.ImageUrl,
        }).ToList(),
    };
}
