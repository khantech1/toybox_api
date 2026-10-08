using Microsoft.EntityFrameworkCore;
using ToyBoxApi.Data;
using ToyBoxApi.DTOs.Exchange;
using ToyBoxApi.Entities;

namespace ToyBoxApi.Services;

public interface IExchangeRequestService
{
    Task<List<ExchangeRequestDto>> GetAllAsync(int userId, string? status, string? direction);
    Task<ExchangeRequestDto> GetByIdAsync(int requestId, int userId);
    Task<ExchangeRequestDto> CreateAsync(int userId, CreateExchangeRequest request);
    Task<ExchangeRequestDto> AcceptAsync(int requestId, int userId);
    Task<ExchangeRequestDto> DeclineAsync(int requestId, int userId);
    Task<ExchangeRequestDto> ConfirmReturnAsync(int requestId, int userId);
}

public class ExchangeRequestService : IExchangeRequestService
{
    private static readonly TimeSpan MinLoan = TimeSpan.FromHours(1);
    private static readonly TimeSpan MaxLoan = TimeSpan.FromDays(365);

    private readonly AppDbContext _db;
    private readonly IToyTransferService _transfers;
    private readonly INotificationService _notifications;

    public ExchangeRequestService(
        AppDbContext db,
        IToyTransferService transfers,
        INotificationService notifications)
    {
        _db            = db;
        _transfers     = transfers;
        _notifications = notifications;
    }

    /// <param name="status">Single status or comma-separated list.</param>
    /// <param name="direction">incoming | outgoing | all. Defaults to incoming for pending, all otherwise.</param>
    public async Task<List<ExchangeRequestDto>> GetAllAsync(int userId, string? status, string? direction)
    {
        var statuses = (status ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => s.ToLowerInvariant())
            .ToList();

        direction = direction?.ToLowerInvariant()
            ?? (statuses.Count == 1 && statuses[0] == ExchangeStatuses.Pending ? "incoming" : "all");

        var query = RequestsWithDetails();

        query = direction switch
        {
            "incoming" => query.Where(r => r.ReceiverUserId == userId),
            "outgoing" => query.Where(r => r.InitiatorUserId == userId),
            _          => query.Where(r => r.InitiatorUserId == userId || r.ReceiverUserId == userId),
        };

        if (statuses.Count > 0)
            query = query.Where(r => statuses.Contains(r.Status));

        var requests = await query
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return requests.Select(MapToDto).ToList();
    }

    public async Task<ExchangeRequestDto> GetByIdAsync(int requestId, int userId)
    {
        var request = await LoadFullRequest(requestId)
            ?? throw new KeyNotFoundException("Exchange request not found.");

        if (!IsParticipant(request, userId))
            throw new UnauthorizedAccessException("Access denied.");

        return MapToDto(request);
    }

    public async Task<ExchangeRequestDto> CreateAsync(int userId, CreateExchangeRequest req)
    {
        var requestedToyIds = req.RequestedToyIds.Distinct().ToList();
        var offeredToyIds   = req.OfferedToyIds.Distinct().ToList();
        var exchangeType    = (req.ExchangeType ?? "permanent").Trim().ToLowerInvariant();

        if (requestedToyIds.Count == 0)
            throw new InvalidOperationException("At least one requested toy is required.");

        if (offeredToyIds.Count == 0)
            throw new InvalidOperationException("At least one offered toy is required.");

        if (requestedToyIds.Intersect(offeredToyIds).Any())
            throw new InvalidOperationException("A toy cannot be on both sides of the exchange.");

        if (exchangeType is not ("permanent" or "temporary"))
            throw new InvalidOperationException("Exchange type must be 'permanent' or 'temporary'.");

        DateTime? returnDueAt = null;
        if (exchangeType == "temporary")
        {
            if (req.ReturnDueAt is null)
                throw new InvalidOperationException("A return date is required for a temporary exchange.");

            var due = ToUtc(req.ReturnDueAt.Value);
            var now = DateTime.UtcNow;

            if (due < now + MinLoan)
                throw new InvalidOperationException("The return date must be at least 1 hour from now.");

            if (due > now + MaxLoan)
                throw new InvalidOperationException("The return date cannot be more than a year away.");

            returnDueAt = due;
        }

        // Requested toys: exist, not mine, one owner, listed, visible to me, available.
        var requestedToys = await _db.Toys
            .Where(t => requestedToyIds.Contains(t.ToyId))
            .ToListAsync();

        if (requestedToys.Count != requestedToyIds.Count)
            throw new KeyNotFoundException("One or more requested toys were not found.");

        if (requestedToys.Any(t => t.OwnerUserId == userId))
            throw new InvalidOperationException("You cannot request your own toy.");

        if (requestedToys.Select(t => t.OwnerUserId).Distinct().Count() > 1)
            throw new InvalidOperationException("All requested toys must belong to the same owner.");

        if (requestedToys.Any(t => !t.IsListed))
            throw new InvalidOperationException("One or more requested toys are not listed for exchange.");

        var visibleCount = await _db.Toys
            .Where(t => requestedToyIds.Contains(t.ToyId))
            .VisibleTo(_db, userId)
            .CountAsync();

        if (visibleCount != requestedToyIds.Count)
            throw new InvalidOperationException("One or more requested toys are not shared with you.");

        foreach (var toy in requestedToys)
            await _transfers.EnsureTransferableAsync(toy);

        // Offered toys: exist, mine, available.
        var offeredToys = await _db.Toys
            .Where(t => offeredToyIds.Contains(t.ToyId))
            .ToListAsync();

        if (offeredToys.Count != offeredToyIds.Count)
            throw new KeyNotFoundException("One or more offered toys were not found.");

        if (offeredToys.Any(t => t.OwnerUserId != userId))
            throw new UnauthorizedAccessException("You do not own one or more of the offered toys.");

        foreach (var toy in offeredToys)
            await _transfers.EnsureTransferableAsync(toy);

        var duplicate = await _db.ExchangeRequests
            .AnyAsync(r =>
                r.InitiatorUserId == userId &&
                r.Status == ExchangeStatuses.Pending &&
                r.Toys.Any(rt => requestedToyIds.Contains(rt.ToyId) && rt.ExchangeRole == "requested"));

        if (duplicate)
            throw new InvalidOperationException("You already have a pending request for one of these toys.");

        var receiverId = requestedToys[0].OwnerUserId;

        var exchangeRequest = new ExchangeRequest
        {
            InitiatorUserId = userId,
            ReceiverUserId  = receiverId,
            Status          = ExchangeStatuses.Pending,
            ExchangeType    = exchangeType,
            ReturnDueAt     = returnDueAt,
            CreatedAt       = DateTime.UtcNow,
            Message         = req.Message?.Trim(),
        };

        foreach (var t in requestedToys)
            exchangeRequest.Toys.Add(new ExchangeRequestToy
            {
                ToyId = t.ToyId, ExchangeRole = "requested", ValueAtRequest = t.Value,
            });

        foreach (var t in offeredToys)
            exchangeRequest.Toys.Add(new ExchangeRequestToy
            {
                ToyId = t.ToyId, ExchangeRole = "offered", ValueAtRequest = t.Value,
            });

        _db.ExchangeRequests.Add(exchangeRequest);
        await _db.SaveChangesAsync();

        var initiatorName = await UserNameAsync(userId);
        _notifications.Add(receiverId, NotificationTypes.ExchangeRequestReceived,
            exchangeType == "temporary" ? "New temporary exchange request" : "New exchange request",
            $"{initiatorName} wants to exchange for {DescribeToys(requestedToys)}.",
            requestId: exchangeRequest.RequestId, actorUserId: userId);
        await _db.SaveChangesAsync();

        var full = await LoadFullRequest(exchangeRequest.RequestId);
        return MapToDto(full!);
    }

    public async Task<ExchangeRequestDto> AcceptAsync(int requestId, int userId)
    {
        var request = await LoadFullRequest(requestId)
            ?? throw new KeyNotFoundException("Exchange request not found.");

        var requestedToys = ToysByRole(request, "requested");
        var offeredToys   = ToysByRole(request, "offered");

        if (requestedToys.Count == 0 || offeredToys.Count == 0)
            throw new InvalidOperationException("Exchange toys not found.");

        var receiverId = request.ReceiverUserId ?? requestedToys[0].OwnerUserId;

        if (receiverId != userId)
            throw new UnauthorizedAccessException("You are not authorised to accept this request.");

        if (request.Status != ExchangeStatuses.Pending)
            throw new InvalidOperationException($"Request is already {request.Status}.");

        // Re-validate: toys must still belong to the original parties and be available.
        if (requestedToys.Any(t => t.OwnerUserId != receiverId))
            throw new InvalidOperationException("You no longer own one of the requested toys.");

        if (offeredToys.Any(t => t.OwnerUserId != request.InitiatorUserId))
            throw new InvalidOperationException("One of the offered toys is no longer available.");

        foreach (var toy in requestedToys.Concat(offeredToys))
            await _transfers.EnsureTransferableAsync(toy);

        var now = DateTime.UtcNow;

        if (request.ExchangeType == "temporary")
        {
            foreach (var toy in requestedToys)
                LendTo(toy, request.InitiatorUserId, requestId, now);

            foreach (var toy in offeredToys)
                LendTo(toy, receiverId, requestId, now);

            await _transfers.DeclineCompetingRequestsAsync(
                requestedToys.Concat(offeredToys).Select(t => t.ToyId), requestId, userId);

            request.Status        = ExchangeStatuses.ActiveLoan;
            request.LoanStartedAt = now;
        }
        else
        {
            foreach (var toy in offeredToys)
                await _transfers.TransferAsync(toy, receiverId, HistoryTypes.Exchange, userId, requestId: requestId);

            foreach (var toy in requestedToys)
                await _transfers.TransferAsync(toy, request.InitiatorUserId, HistoryTypes.Exchange, userId, requestId: requestId);

            request.Status = ExchangeStatuses.Completed;
        }

        var receiverName = await UserNameAsync(receiverId);
        _notifications.Add(request.InitiatorUserId, NotificationTypes.ExchangeRequestAccepted,
            "Exchange request accepted",
            request.ExchangeType == "temporary"
                ? $"{receiverName} accepted your temporary exchange. Return due {request.ReturnDueAt:yyyy-MM-dd HH:mm} UTC."
                : $"{receiverName} accepted your exchange. The toys are now in your toys (unlisted).",
            requestId: requestId, actorUserId: userId);

        await _db.SaveChangesAsync();

        var full = await LoadFullRequest(requestId);
        return MapToDto(full!);
    }

    public async Task<ExchangeRequestDto> DeclineAsync(int requestId, int userId)
    {
        var request = await LoadFullRequest(requestId)
            ?? throw new KeyNotFoundException("Exchange request not found.");

        if (!IsParticipant(request, userId))
            throw new UnauthorizedAccessException("Access denied.");

        if (request.Status != ExchangeStatuses.Pending)
            throw new InvalidOperationException($"Request is already {request.Status}.");

        request.Status = ExchangeStatuses.Declined;

        var actorName = await UserNameAsync(userId);
        if (userId == request.InitiatorUserId)
        {
            if (request.ReceiverUserId is int receiverId)
                _notifications.Add(receiverId, NotificationTypes.ExchangeRequestCancelled,
                    "Exchange request cancelled", $"{actorName} cancelled their exchange request.",
                    requestId: requestId, actorUserId: userId);
        }
        else
        {
            _notifications.Add(request.InitiatorUserId, NotificationTypes.ExchangeRequestDeclined,
                "Exchange request declined", $"{actorName} declined your exchange request.",
                requestId: requestId, actorUserId: userId);
        }

        await _db.SaveChangesAsync();

        return MapToDto(request);
    }

    public async Task<ExchangeRequestDto> ConfirmReturnAsync(int requestId, int userId)
    {
        var request = await LoadFullRequest(requestId)
            ?? throw new KeyNotFoundException("Exchange request not found.");

        if (request.Status != ExchangeStatuses.ActiveLoan)
            throw new InvalidOperationException("This exchange is not an active loan.");

        int otherPartyId;
        if (userId == request.InitiatorUserId)
        {
            request.ReturnConfirmedByInitiator = true;
            otherPartyId = request.ReceiverUserId ?? 0;
        }
        else if (userId == request.ReceiverUserId)
        {
            request.ReturnConfirmedByReceiver = true;
            otherPartyId = request.InitiatorUserId;
        }
        else
        {
            throw new UnauthorizedAccessException("Access denied.");
        }

        var now = DateTime.UtcNow;
        var actorName = await UserNameAsync(userId);

        if (request.ReturnConfirmedByInitiator && request.ReturnConfirmedByReceiver)
        {
            foreach (var rt in request.Toys)
                if (rt.Toy is not null)
                    rt.Toy.CurrentHolderUserId = rt.Toy.OwnerUserId;

            var openLoanRows = await _db.ToyOwnershipHistory
                .Where(h => h.RequestId == requestId && h.Type == HistoryTypes.Loan && h.EndedAt == null)
                .ToListAsync();

            foreach (var row in openLoanRows)
                row.EndedAt = now;

            request.Status     = ExchangeStatuses.Returned;
            request.ReturnedAt = now;

            foreach (var party in new[] { request.InitiatorUserId, request.ReceiverUserId ?? 0 })
                if (party != 0)
                    _notifications.Add(party, NotificationTypes.LoanReturned,
                        "Temporary exchange completed",
                        "Both sides confirmed the return. Your toys are back in your toys (unlisted).",
                        requestId: requestId, actorUserId: userId);
        }
        else if (otherPartyId != 0)
        {
            _notifications.Add(otherPartyId, NotificationTypes.LoanReturnConfirmed,
                "Return confirmed",
                $"{actorName} confirmed the toys were returned. Please confirm on your side.",
                requestId: requestId, actorUserId: userId);
        }

        await _db.SaveChangesAsync();

        var full = await LoadFullRequest(requestId);
        return MapToDto(full!);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private void LendTo(Toy toy, int holderId, int requestId, DateTime now)
    {
        toy.CurrentHolderUserId = holderId;
        toy.IsListed            = false;

        _db.ToyOwnershipHistory.Add(new ToyOwnershipHistory
        {
            ToyId     = toy.ToyId,
            UserId    = holderId,
            Type      = HistoryTypes.Loan,
            RequestId = requestId,
            StartedAt = now,
        });
    }

    private static List<Toy> ToysByRole(ExchangeRequest r, string role) => r.Toys
        .Where(rt => rt.ExchangeRole == role && rt.Toy is not null)
        .Select(rt => rt.Toy!)
        .ToList();

    private static bool IsParticipant(ExchangeRequest r, int userId) =>
        r.InitiatorUserId == userId || r.ReceiverUserId == userId;

    private static DateTime ToUtc(DateTime dt) => dt.Kind switch
    {
        DateTimeKind.Utc   => dt,
        DateTimeKind.Local => dt.ToUniversalTime(),
        _                  => DateTime.SpecifyKind(dt, DateTimeKind.Utc),
    };

    private static string DescribeToys(List<Toy> toys) =>
        toys.Count == 1 ? $"\"{toys[0].ToyName}\"" : $"{toys.Count} of your toys";

    private async Task<string> UserNameAsync(int userId) =>
        await _db.Users.Where(u => u.UserId == userId).Select(u => u.Name).FirstOrDefaultAsync()
        ?? "Someone";

    private IQueryable<ExchangeRequest> RequestsWithDetails() => _db.ExchangeRequests
        .Include(r => r.Initiator)
        .Include(r => r.Receiver)
        .Include(r => r.Toys)
            .ThenInclude(rt => rt.Toy)
                .ThenInclude(t => t!.Images)
        .Include(r => r.Toys)
            .ThenInclude(rt => rt.Toy)
                .ThenInclude(t => t!.Category)
        .Include(r => r.Toys)
            .ThenInclude(rt => rt.Toy)
                .ThenInclude(t => t!.Owner)
        .AsSplitQuery();

    private Task<ExchangeRequest?> LoadFullRequest(int requestId) =>
        RequestsWithDetails().FirstOrDefaultAsync(r => r.RequestId == requestId);

    // ── Mapper ────────────────────────────────────────────────────────────────
    private static ExchangeRequestDto MapToDto(ExchangeRequest r) => new()
    {
        RequestId                  = r.RequestId,
        InitiatorUserId            = r.InitiatorUserId,
        ReceiverUserId             = r.ReceiverUserId,
        Status                     = r.Status,
        ExchangeType               = r.ExchangeType,
        CreatedAt                  = r.CreatedAt,
        Message                    = r.Message,
        ReturnDueAt                = r.ReturnDueAt,
        LoanStartedAt              = r.LoanStartedAt,
        ReturnedAt                 = r.ReturnedAt,
        ReturnConfirmedByInitiator = r.ReturnConfirmedByInitiator,
        ReturnConfirmedByReceiver  = r.ReturnConfirmedByReceiver,
        IsOverdue = r.Status == ExchangeStatuses.ActiveLoan &&
                    r.ReturnDueAt.HasValue && r.ReturnDueAt.Value < DateTime.UtcNow,
        Initiator = r.Initiator is null ? null : AuthService.MapToDto(r.Initiator),
        Receiver  = r.Receiver  is null ? null : AuthService.MapToDto(r.Receiver),
        Toys = r.Toys.Select(rt => new ExchangeRequestToyDto
        {
            RequestId      = rt.RequestId,
            ToyId          = rt.ToyId,
            ExchangeRole   = rt.ExchangeRole,
            ValueAtRequest = rt.ValueAtRequest,
            Toy            = rt.Toy is null ? null : ToyService.MapToDto(rt.Toy),
        }).ToList(),
        RequestedTotalValue = r.Toys
            .Where(rt => rt.ExchangeRole == "requested")
            .Sum(rt => rt.ValueAtRequest ?? 0),
        OfferedTotalValue = r.Toys
            .Where(rt => rt.ExchangeRole == "offered")
            .Sum(rt => rt.ValueAtRequest ?? 0),
    };
}
