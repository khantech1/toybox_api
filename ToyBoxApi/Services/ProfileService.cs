using Microsoft.EntityFrameworkCore;
using ToyBoxApi.Data;
using ToyBoxApi.DTOs.Auth;
using ToyBoxApi.DTOs.Profile;
using ToyBoxApi.Entities;

namespace ToyBoxApi.Services;

public interface IProfileService
{
    Task<UserDto> GetMeAsync(int userId);
    Task<UserDto> GetByIdAsync(int viewerId, int userId);
    Task<UserDto> UpdateAsync(int userId, UpdateProfileRequest request);
    Task<UserDto> UploadProfilePicAsync(int userId, IFormFile file);
    Task<List<ContactDto>> GetContactsAsync(int userId);
    Task AddContactAsync(int userId, int contactId);
    Task RemoveContactAsync(int userId, int contactId);
    Task<List<ContactDto>> SyncContactsAsync(int userId, SyncContactsRequest request);
}

public class ProfileService : IProfileService
{
    private readonly AppDbContext _db;
    private readonly IFileService _fileService;

    public ProfileService(AppDbContext db, IFileService fileService)
    {
        _db          = db;
        _fileService = fileService;
    }

    public async Task<UserDto> GetMeAsync(int userId)
    {
        var user = await _db.Users.FindAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");
        return AuthService.MapToDto(user);
    }

    public async Task<UserDto> GetByIdAsync(int viewerId, int userId)
    {
        var user = await _db.Users.FindAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");

        var dto = AuthService.MapToDto(user);

        var linked = viewerId == userId || await _db.Contacts.AnyAsync(c =>
            (c.UserId == viewerId && c.ContactId == userId) ||
            (c.UserId == userId && c.ContactId == viewerId));

        if (!linked)
        {
            dto.Email   = null;
            dto.PhoneNo = null;
        }

        return dto;
    }

    public async Task<UserDto> UpdateAsync(int userId, UpdateProfileRequest request)
    {
        var user = await _db.Users.FindAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");

        if (request.Name is not null)    user.Name    = request.Name.Trim();
        if (request.Address is not null) user.Address = request.Address.Trim();

        await _db.SaveChangesAsync();
        return AuthService.MapToDto(user);
    }

    public async Task<UserDto> UploadProfilePicAsync(int userId, IFormFile file)
    {
        var user = await _db.Users.FindAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");

        // Delete the old profile picture
        _fileService.DeleteFile(user.ProfilePic);

        user.ProfilePic = await _fileService.SaveFileAsync(file, "profiles");
        await _db.SaveChangesAsync();

        return AuthService.MapToDto(user);
    }

    public async Task<List<ContactDto>> GetContactsAsync(int userId)
    {
        var contacts = await _db.Contacts
            .Include(c => c.ContactUser)
            .Where(c => c.UserId == userId)
            .ToListAsync();

        return contacts.Select(c => new ContactDto
        {
            UserId    = c.UserId,
            ContactId = c.ContactId,
            ContactUser = c.ContactUser is null
                ? null
                : AuthService.MapToDto(c.ContactUser),
        }).ToList();
    }

    public async Task AddContactAsync(int userId, int contactId)
    {
        if (userId == contactId)
            throw new InvalidOperationException("You cannot add yourself as a contact.");

        var targetUser = await _db.Users.FindAsync(contactId)
            ?? throw new KeyNotFoundException("User not found.");

        var exists = await _db.Contacts.AnyAsync(c =>
            c.UserId == userId && c.ContactId == contactId);

        if (exists)
            throw new InvalidOperationException("Already in your contacts.");

        _db.Contacts.Add(new Contact { UserId = userId, ContactId = contactId });
        await _db.SaveChangesAsync();
    }

    public async Task RemoveContactAsync(int userId, int contactId)
    {
        var contact = await _db.Contacts
            .FirstOrDefaultAsync(c => c.UserId == userId && c.ContactId == contactId)
            ?? throw new KeyNotFoundException("Contact not found.");

        _db.Contacts.Remove(contact);
        await _db.SaveChangesAsync();
    }
    public async Task<List<ContactDto>> SyncContactsAsync(int userId, SyncContactsRequest request)
    {
        Console.WriteLine($"Controller received count: {request.PhoneNumbers.Count}");
        Console.WriteLine("Incoming Contacts:");

        foreach (var number in request.PhoneNumbers)
        {
            Console.WriteLine("Number ${number}");
        }

        var oldContacts = await _db.Contacts
            .Where(c => c.UserId == userId)
            .ToListAsync();

        _db.Contacts.RemoveRange(oldContacts);

        var cleanNumbers = request.PhoneNumbers
            .Select(NormalizePhone)
            .Where(p => p.Length == 11 && p.StartsWith("03"))
            .Distinct()
            .ToHashSet();
        Console.WriteLine("Cleaned Contacts:");

        foreach (var number in cleanNumbers)
        {
            Console.WriteLine(number);
        }


        var users = await _db.Users
            .Where(u => u.UserId != userId)
            .ToListAsync();

        var matchedUsers = users
            .Where(u => cleanNumbers.Contains(NormalizePhone(u.PhoneNo)))
            .ToList();

        foreach (var user in matchedUsers)
        {
            _db.Contacts.Add(new Contact
            {
                UserId = userId,
                ContactId = user.UserId
            });
        }

        await _db.SaveChangesAsync();

        return await GetContactsAsync(userId);
    }
    private static string NormalizePhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return "";

        var value = phone.Trim();

        value = new string(value.Where(c => char.IsDigit(c) || c == '+').ToArray());

        if (value.StartsWith("+92"))
            value = "0" + value.Substring(3);
        else if (value.StartsWith("92") && value.Length == 12)
            value = "0" + value.Substring(2);
        else if (value.Length == 10 && value.StartsWith("3"))
            value = "0" + value;

        return value;
    }
}
