using Microsoft.EntityFrameworkCore;
using ToyBoxApi.Data;
using ToyBoxApi.DTOs.Auth;
using ToyBoxApi.DTOs.Exchange;
using ToyBoxApi.DTOs.Toys;
using ToyBoxApi.Entities;

namespace ToyBoxApi.Services;

public interface IExchangeRequestService
{
    Task<List<ExchangeRequestDto>> GetAllAsync(int userId, string? status);
    Task<ExchangeRequestDto> GetByIdAsync(int requestId, int userId);
    Task<ExchangeRequestDto> CreateAsync(int userId, CreateExchangeRequest request);
    Task<ExchangeRequestDto> AcceptAsync(int requestId, int userId);
    Task<ExchangeRequestDto> DeclineAsync(int requestId, int userId);
}

public class ExchangeRequestService : IExchangeRequestService
{
    private readonly AppDbContext _db;

    public ExchangeRequestService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<ExchangeRequestDto>> GetAllAsync(int userId, string? status)
    {
        // Return requests where the user is either the initiator
        // or the owner of one of the requested toys
        var query = _db.ExchangeRequests
     .Include(r => r.Initiator)
     .Include(r => r.Toys)
         .ThenInclude(rt => rt.Toy)
             .ThenInclude(t => t!.Images)
     .Include(r => r.Toys)
         .ThenInclude(rt => rt.Toy)
             .ThenInclude(t => t!.Category)
     .Include(r => r.Toys)
         .ThenInclude(rt => rt.Toy)
             .ThenInclude(t => t!.Owner)
     .Where(r =>
         status == "pending"
             ? r.Toys.Any(rt =>
                 rt.ExchangeRole == "requested" &&
                 rt.Toy!.OwnerUserId == userId)
             : r.InitiatorUserId == userId ||
               r.Toys.Any(rt => rt.Toy!.OwnerUserId == userId))
     .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(r => r.Status == status.ToLower());

        var requests = await query
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return requests.Select(MapToDto).ToList();
    }

    public async Task<ExchangeRequestDto> GetByIdAsync(int requestId, int userId)
    {
        var request = await LoadFullRequest(requestId)
            ?? throw new KeyNotFoundException("Exchange request not found.");

        // Only initiator or the toy owner can view
        var isParticipant =
            request.InitiatorUserId == userId ||
            request.Toys.Any(rt => rt.Toy?.OwnerUserId == userId);

        if (!isParticipant)
            throw new UnauthorizedAccessException("Access denied.");

        return MapToDto(request);
    }

    public async Task<ExchangeRequestDto> CreateAsync(int userId, CreateExchangeRequest req)
    {
        // Validate requested toy exists and is not owned by caller
        var requestedToy = await _db.Toys
            .Include(t => t.Owner)
            .FirstOrDefaultAsync(t => t.ToyId == req.RequestedToyId)
            ?? throw new KeyNotFoundException("Requested toy not found.");

        if (requestedToy.OwnerUserId == userId)
            throw new InvalidOperationException("You cannot request your own toy.");

        // Validate offered toy is owned by caller
        var offeredToy = await _db.Toys.FindAsync(req.OfferedToyId)
            ?? throw new KeyNotFoundException("Offered toy not found.");

        if (offeredToy.OwnerUserId != userId)
            throw new UnauthorizedAccessException("You do not own the offered toy.");

        // Check no duplicate pending request
        var duplicate = await _db.ExchangeRequests
            .AnyAsync(r =>
                r.InitiatorUserId == userId &&
                r.Status == "pending" &&
                r.Toys.Any(rt => rt.ToyId == req.RequestedToyId && rt.ExchangeRole == "requested"));

        if (duplicate)
            throw new InvalidOperationException("You already have a pending request for this toy.");

        var exchangeRequest = new ExchangeRequest
        {
            InitiatorUserId = userId,
            Status          = "pending",
            CreatedAt       = DateTime.UtcNow,
            Message         = req.Message?.Trim(),
        };

        _db.ExchangeRequests.Add(exchangeRequest);
        await _db.SaveChangesAsync();

        _db.ExchangeRequestToys.AddRange(
            new ExchangeRequestToy
            {
                RequestId    = exchangeRequest.RequestId,
                ToyId        = req.RequestedToyId,
                ExchangeRole = "requested",
            },
            new ExchangeRequestToy
            {
                RequestId    = exchangeRequest.RequestId,
                ToyId        = req.OfferedToyId,
                ExchangeRole = "offered",
            }
        );

        await _db.SaveChangesAsync();

        var full = await LoadFullRequest(exchangeRequest.RequestId);
        return MapToDto(full!);
    }

    public async Task<ExchangeRequestDto> AcceptAsync(int requestId, int userId)
    {
        var request = await LoadFullRequest(requestId)
            ?? throw new KeyNotFoundException("Exchange request not found.");

        var requestedToyEntry = request.Toys
            .FirstOrDefault(rt => rt.ExchangeRole == "requested");

        var offeredToyEntry = request.Toys
            .FirstOrDefault(rt => rt.ExchangeRole == "offered");

        if (requestedToyEntry?.Toy == null || offeredToyEntry?.Toy == null)
            throw new InvalidOperationException("Exchange toys not found.");

        var requestedToy = requestedToyEntry.Toy;
        var offeredToy = offeredToyEntry.Toy;

        // Only owner of requested toy can accept
        if (requestedToy.OwnerUserId != userId)
            throw new UnauthorizedAccessException("You are not authorised to accept this request.");

        if (request.Status != "pending")
            throw new InvalidOperationException($"Request is already {request.Status}.");

        // Save old owners
        int requestedToyOwnerId = requestedToy.OwnerUserId;
        int offeredToyOwnerId = offeredToy.OwnerUserId;

        // Exchange ownership
        requestedToy.OwnerUserId = offeredToyOwnerId;
        offeredToy.OwnerUserId = requestedToyOwnerId;

        // Remove old shared-toy entries because ownership is changing
        var toyIds = new[] { requestedToy.ToyId, offeredToy.ToyId };

        var sharedEntries = await _db.SharedToys
            .Where(st => toyIds.Contains(st.ToyId))
            .ToListAsync();

        _db.SharedToys.RemoveRange(sharedEntries);

        // Mark request accepted/completed
        request.Status = "completed";

        await _db.SaveChangesAsync();

        var full = await LoadFullRequest(request.RequestId);
        return MapToDto(full!);
    }

    public async Task<ExchangeRequestDto> DeclineAsync(int requestId, int userId)
    {
        var request = await LoadFullRequest(requestId)
            ?? throw new KeyNotFoundException("Exchange request not found.");

        var isParticipant =
            request.InitiatorUserId == userId ||
            request.Toys.Any(rt => rt.Toy?.OwnerUserId == userId);

        if (!isParticipant)
            throw new UnauthorizedAccessException("Access denied.");

        if (request.Status != "pending")
            throw new InvalidOperationException($"Request is already {request.Status}.");

        request.Status = "declined";
        await _db.SaveChangesAsync();

        return MapToDto(request);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task<ExchangeRequest?> LoadFullRequest(int requestId)
    {
        return await _db.ExchangeRequests
            .Include(r => r.Initiator)
            .Include(r => r.Toys)
                .ThenInclude(rt => rt.Toy)
                    .ThenInclude(t => t!.Images)
            .Include(r => r.Toys)
                .ThenInclude(rt => rt.Toy)
                    .ThenInclude(t => t!.Category)
            .Include(r => r.Toys)
                .ThenInclude(rt => rt.Toy)
                    .ThenInclude(t => t!.Owner)
            .FirstOrDefaultAsync(r => r.RequestId == requestId);
    }

    // ── Mapper ────────────────────────────────────────────────────────────────
    private static ExchangeRequestDto MapToDto(ExchangeRequest r) => new()
    {
        RequestId       = r.RequestId,
        InitiatorUserId = r.InitiatorUserId,
        Status          = r.Status,
        CreatedAt       = r.CreatedAt,
        Message         = r.Message,
        Initiator = r.Initiator is null ? null : AuthService.MapToDto(r.Initiator),
        Toys = r.Toys.Select(rt => new ExchangeRequestToyDto
        {
            RequestId    = rt.RequestId,
            ToyId        = rt.ToyId,
            ExchangeRole = rt.ExchangeRole,
            Toy          = rt.Toy is null ? null : ToyService.MapToDto(rt.Toy),
        }).ToList(),
    };
}
