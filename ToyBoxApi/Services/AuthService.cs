using Microsoft.EntityFrameworkCore;
using ToyBoxApi.Data;
using ToyBoxApi.DTOs.Auth;
using ToyBoxApi.Entities;
using ToyBoxApi.Helpers;

namespace ToyBoxApi.Services;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<UserDto> ProfileSetupAsync(int userId, ProfileSetupRequest request);
}

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IJwtHelper _jwt;

    public AuthService(AppDbContext db, IJwtHelper jwt)
    {
        _db = db;
        _jwt = jwt;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var exists = await _db.Users.AnyAsync(u => u.Email == request.Email.ToLower());
        if (exists)
            throw new InvalidOperationException("An account with this email already exists.");

        var user = new User
        {
            Email    = request.Email.ToLower().Trim(),
            PhoneNo  = request.PhoneNo.Trim(),
            Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Name     = request.Email.Split('@')[0], // temporary name until profile setup
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return new AuthResponse
        {
            Token = _jwt.GenerateToken(user),
            User  = MapToDto(user),
        };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLower().Trim());

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
            throw new UnauthorizedAccessException("Invalid email or password.");

        return new AuthResponse
        {
            Token = _jwt.GenerateToken(user),
            User  = MapToDto(user),
        };
    }

    public async Task<UserDto> ProfileSetupAsync(int userId, ProfileSetupRequest request)
    {
        var user = await _db.Users.FindAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");

        user.Name    = request.Name.Trim();
        user.Address = request.Address?.Trim();

        await _db.SaveChangesAsync();
        return MapToDto(user);
    }

    // ── Mapper ────────────────────────────────────────────────────────────────
    public static UserDto MapToDto(User u) => new()
    {
        UserId     = u.UserId,
        Name       = u.Name,
        Email      = u.Email,
        PhoneNo    = u.PhoneNo,
        Address    = u.Address,
        ProfilePic = u.ProfilePic,
        Rating     = u.Rating,
    };
}
