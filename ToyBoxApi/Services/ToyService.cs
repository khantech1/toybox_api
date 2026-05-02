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
    Task<ToyDto> CreateAsync(int userId, CreateToyRequest request);
    Task<ToyDto> UpdateAsync(int userId, int toyId, UpdateToyRequest request);
    Task DeleteAsync(int userId, int toyId);
    Task<ToyDto> AddImageAsync(int userId, int toyId, IFormFile file);
}

public class ToyService : IToyService
{
    private readonly AppDbContext _db;
    private readonly IFileService _fileService;

    public ToyService(AppDbContext db, IFileService fileService)
    {
        _db          = db;
        _fileService = fileService;
    }

    public async Task<List<ToyDto>> GetAllAsync(
    int currentUserId, string? search, int? categoryId, string? ageGroup)
    {
        var contactUserIds = await _db.Contacts
            .Where(c => c.UserId == currentUserId)
            .Select(c => c.ContactId)
            .ToListAsync();

        var query = _db.Toys
            .Include(t => t.Owner)
            .Include(t => t.Category)
            .Include(t => t.DesiredCategory)
            .Include(t => t.Images)
            .Include(t => t.SharedWith)
            .Where(t =>
                t.OwnerUserId != currentUserId &&
                (
                    // show toys of contacts
                    contactUserIds.Contains(t.OwnerUserId)

                    // OR toys specifically shared with current user
                    || t.SharedWith.Any(s => s.SharedWithUserId == currentUserId)
                )
            )
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(t =>
                t.ToyName.Contains(search) ||
                (t.ToyDescription != null && t.ToyDescription.Contains(search)));

        if (categoryId.HasValue)
            query = query.Where(t => t.CategoryId == categoryId.Value);

        var toys = await query
            .OrderByDescending(t => t.ToyId)
            .ToListAsync();

        return toys.Select(MapToDto).ToList();
    }

    public async Task<ToyDto> GetByIdAsync(int toyId, int currentUserId)
    {
        var toy = await _db.Toys
            .Include(t => t.Owner)
            .Include(t => t.Category)
            .Include(t => t.DesiredCategory)
            .Include(t => t.Images)
            .FirstOrDefaultAsync(t => t.ToyId == toyId)
            ?? throw new KeyNotFoundException("Toy not found.");

        return MapToDto(toy);
    }

    public async Task<List<ToyDto>> GetMyToysAsync(int userId)
    {
        var toys = await _db.Toys
            .Include(t => t.Category)
            .Include(t => t.DesiredCategory)
            .Include(t => t.Images)
            .Where(t => t.OwnerUserId == userId)
            .OrderByDescending(t => t.ToyId)
            .ToListAsync();

        return toys.Select(MapToDto).ToList();
    }

    public async Task<ToyDto> CreateAsync(int userId, CreateToyRequest request)
    {
        var toy = new Toy
        {
            OwnerUserId      = userId,
            ToyName          = request.ToyName.Trim(),
            ToyDescription   = request.ToyDescription?.Trim(),
            CategoryId       = request.CategoryId,
            DesiredCategoryId = request.DesiredCategoryId,
            ConditionStatus  = request.ConditionStatus,
            Value            = request.Value,
        };

        _db.Toys.Add(toy);
        await _db.SaveChangesAsync();

        // Handle visibility: if not visible to all, create SharedToy records
        if (!request.VisibleToAll && request.VisibleToUserIds?.Any() == true)
        {
            foreach (var contactId in request.VisibleToUserIds)
            {
                _db.SharedToys.Add(new SharedToy
                {
                    ToyId            = toy.ToyId,
                    SharedWithUserId = contactId,
                });
            }
            await _db.SaveChangesAsync();
        }

        // Reload with navigation
        return await GetByIdAsync(toy.ToyId, userId);
    }

    public async Task<ToyDto> UpdateAsync(int userId, int toyId, UpdateToyRequest request)
    {
        var toy = await _db.Toys
            .Include(t => t.SharedWith)
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

            if (!request.VisibleToAll.Value && request.VisibleToUserIds?.Any() == true)
            {
                foreach (var contactId in request.VisibleToUserIds)
                {
                    _db.SharedToys.Add(new SharedToy
                    {
                        ToyId = toyId,
                        SharedWithUserId = contactId
                    });
                }
            }
        }

        await _db.SaveChangesAsync();
        return await GetByIdAsync(toyId, userId);
    }

    public async Task DeleteAsync(int userId, int toyId)
    {
        var toy = await _db.Toys.FindAsync(toyId)
            ?? throw new KeyNotFoundException("Toy not found.");

        if (toy.OwnerUserId != userId)
            throw new UnauthorizedAccessException("You do not own this toy.");

        var images = await _db.ToyImages
            .Where(i => i.ToyId == toyId)
            .ToListAsync();

        foreach (var img in images)
            _fileService.DeleteFile(img.ImageUrl);

        _db.ToyImages.RemoveRange(images);

        var sharedToys = await _db.SharedToys
            .Where(s => s.ToyId == toyId)
            .ToListAsync();

        _db.SharedToys.RemoveRange(sharedToys);

        var exchangeRequestToys = await _db.ExchangeRequestToys
            .Where(e => e.ToyId == toyId)
            .ToListAsync();

        _db.ExchangeRequestToys.RemoveRange(exchangeRequestToys);

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

    // ── Mapper ────────────────────────────────────────────────────────────────
    public static ToyDto MapToDto(Toy t) => new()
    {
        ToyId           = t.ToyId,
        OwnerUserId     = t.OwnerUserId,
        ToyName         = t.ToyName,
        ToyDescription  = t.ToyDescription,
        CategoryId      = t.CategoryId,
        DesiredCategoryId = t.DesiredCategoryId,
        ConditionStatus = t.ConditionStatus,
        Value           = t.Value,
        Owner = t.Owner is null ? null : new ToyOwnerDto
        {
            UserId     = t.Owner.UserId,
            Name       = t.Owner.Name,
            ProfilePic = t.Owner.ProfilePic,
            Rating     = t.Owner.Rating,
            Address    = t.Owner.Address,
        },
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
