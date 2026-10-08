using Microsoft.EntityFrameworkCore;
using ToyBoxApi.Data;
using ToyBoxApi.DTOs.Profile;
using ToyBoxApi.Entities;

namespace ToyBoxApi.Services;

public interface IChildService
{
    Task<List<ChildDto>> GetMineAsync(int userId);
    Task<List<ChildDto>> GetForParentAsync(int viewerId, int parentId);
    Task<List<ChildDto>> GetUpcomingBirthdaysAsync(int viewerId, int days);
    Task<ChildDto> CreateAsync(int userId, SaveChildRequest request);
    Task<ChildDto> UpdateAsync(int userId, int childId, SaveChildRequest request);
    Task DeleteAsync(int userId, int childId);

    /// <summary>A child the viewer may see; null when hidden or not found.</summary>
    Task<Child?> FindVisibleAsync(int viewerId, int childId);
}

public class ChildService : IChildService
{
    private readonly AppDbContext _db;

    public ChildService(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Children are visible to the parent, and to users the parent has in their
    /// contacts when the child is marked visible. The parent controls the audience.
    /// </summary>
    private IQueryable<Child> VisibleTo(int viewerId) => _db.Children.Where(c =>
        c.ParentUserId == viewerId ||
        (c.IsVisibleToContacts &&
         _db.Contacts.Any(k => k.UserId == c.ParentUserId && k.ContactId == viewerId)));

    public async Task<List<ChildDto>> GetMineAsync(int userId)
    {
        var children = await _db.Children
            .Where(c => c.ParentUserId == userId)
            .OrderBy(c => c.BirthDate)
            .ToListAsync();

        return children.Select(c => MapToDto(c)).ToList();
    }

    public async Task<List<ChildDto>> GetForParentAsync(int viewerId, int parentId)
    {
        var children = await VisibleTo(viewerId)
            .Where(c => c.ParentUserId == parentId)
            .OrderBy(c => c.BirthDate)
            .ToListAsync();

        return children.Select(c => MapToDto(c)).ToList();
    }

    public async Task<List<ChildDto>> GetUpcomingBirthdaysAsync(int viewerId, int days)
    {
        days = Math.Clamp(days, 1, 366);

        var children = await VisibleTo(viewerId)
            .Include(c => c.Parent)
            .Where(c => c.ParentUserId != viewerId)
            .ToListAsync();

        return children
            .Select(c => MapToDto(c, includeParent: true))
            .Where(d => d.DaysUntilBirthday <= days)
            .OrderBy(d => d.DaysUntilBirthday)
            .ThenBy(d => d.Name)
            .ToList();
    }

    public async Task<ChildDto> CreateAsync(int userId, SaveChildRequest req)
    {
        Validate(req);

        var child = new Child
        {
            ParentUserId        = userId,
            Name                = req.Name.Trim(),
            BirthDate           = req.BirthDate,
            Interests           = string.IsNullOrWhiteSpace(req.Interests) ? null : req.Interests.Trim(),
            IsVisibleToContacts = req.IsVisibleToContacts,
            CreatedAt           = DateTime.UtcNow,
        };

        _db.Children.Add(child);
        await _db.SaveChangesAsync();
        return MapToDto(child);
    }

    public async Task<ChildDto> UpdateAsync(int userId, int childId, SaveChildRequest req)
    {
        Validate(req);

        var child = await OwnChildAsync(userId, childId);
        child.Name                = req.Name.Trim();
        child.BirthDate           = req.BirthDate;
        child.Interests           = string.IsNullOrWhiteSpace(req.Interests) ? null : req.Interests.Trim();
        child.IsVisibleToContacts = req.IsVisibleToContacts;

        await _db.SaveChangesAsync();
        return MapToDto(child);
    }

    public async Task DeleteAsync(int userId, int childId)
    {
        var child = await OwnChildAsync(userId, childId);
        _db.Children.Remove(child);
        await _db.SaveChangesAsync();
    }

    public Task<Child?> FindVisibleAsync(int viewerId, int childId) =>
        VisibleTo(viewerId).FirstOrDefaultAsync(c => c.ChildId == childId);

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<Child> OwnChildAsync(int userId, int childId)
    {
        var child = await _db.Children.FirstOrDefaultAsync(c => c.ChildId == childId)
            ?? throw new KeyNotFoundException("Child not found.");

        if (child.ParentUserId != userId)
            throw new UnauthorizedAccessException("You can only manage your own children.");

        return child;
    }

    private static void Validate(SaveChildRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            throw new InvalidOperationException("Please enter the child's name.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (req.BirthDate > today)
            throw new InvalidOperationException("Birthday cannot be in the future.");

        if (req.BirthDate < today.AddYears(-25))
            throw new InvalidOperationException("Please enter a valid birthday.");
    }

    /// <summary>Birthday in <paramref name="year"/>; Feb 29 falls on Feb 28 in non-leap years.</summary>
    private static DateOnly BirthdayIn(DateOnly birth, int year) =>
        birth.Month == 2 && birth.Day == 29 && !DateTime.IsLeapYear(year)
            ? new DateOnly(year, 2, 28)
            : new DateOnly(year, birth.Month, birth.Day);

    public static ChildDto MapToDto(Child c, bool includeParent = false)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var next = BirthdayIn(c.BirthDate, today.Year);
        if (next < today)
            next = BirthdayIn(c.BirthDate, today.Year + 1);

        var age = today.Year - c.BirthDate.Year;
        if (BirthdayIn(c.BirthDate, today.Year) > today)
            age--;

        return new ChildDto
        {
            ChildId             = c.ChildId,
            ParentUserId        = c.ParentUserId,
            Name                = c.Name,
            BirthDate           = c.BirthDate,
            Interests           = c.Interests,
            IsVisibleToContacts = c.IsVisibleToContacts,
            Age                 = Math.Max(age, 0),
            NextBirthday        = next,
            DaysUntilBirthday   = next.DayNumber - today.DayNumber,
            TurningAge          = next.Year - c.BirthDate.Year,
            Parent              = includeParent && c.Parent is not null ? ToyService.MapOwner(c.Parent) : null,
        };
    }
}
