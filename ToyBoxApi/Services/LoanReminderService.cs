using Microsoft.EntityFrameworkCore;
using ToyBoxApi.Data;
using ToyBoxApi.Entities;

namespace ToyBoxApi.Services;

/// <summary>
/// Periodically notifies both parties of active loans that are due within 24h
/// or overdue. Each reminder is stamped on the request so it is sent only once.
/// </summary>
public class LoanReminderService : BackgroundService
{
    private static readonly TimeSpan DueSoonWindow = TimeSpan.FromHours(24);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LoanReminderService> _logger;
    private readonly TimeSpan _interval;

    public LoanReminderService(
        IServiceScopeFactory scopeFactory,
        ILogger<LoanReminderService> logger,
        IConfiguration config)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
        _interval     = TimeSpan.FromMinutes(config.GetValue("LoanReminder:IntervalMinutes", 60));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_interval);

        do
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Loan reminder tick failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db            = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var now = DateTime.UtcNow;

        var loans = await db.ExchangeRequests
            .Where(r =>
                r.Status == ExchangeStatuses.ActiveLoan &&
                r.ReturnDueAt != null &&
                ((r.ReturnDueAt < now && r.OverdueNotifiedAt == null) ||
                 (r.ReturnDueAt >= now && r.ReturnDueAt <= now + DueSoonWindow && r.DueSoonNotifiedAt == null)))
            .ToListAsync(ct);

        foreach (var loan in loans)
        {
            var overdue = loan.ReturnDueAt < now;

            foreach (var party in new[] { loan.InitiatorUserId, loan.ReceiverUserId ?? 0 })
            {
                if (party == 0) continue;

                if (overdue)
                    notifications.Add(party, NotificationTypes.LoanOverdue,
                        "Temporary exchange overdue",
                        $"The toys were due back on {loan.ReturnDueAt:yyyy-MM-dd HH:mm} UTC. Please return them and confirm.",
                        requestId: loan.RequestId);
                else
                    notifications.Add(party, NotificationTypes.LoanDueSoon,
                        "Temporary exchange due soon",
                        $"The toys are due back on {loan.ReturnDueAt:yyyy-MM-dd HH:mm} UTC.",
                        requestId: loan.RequestId);
            }

            if (overdue)
                loan.OverdueNotifiedAt = now;
            else
                loan.DueSoonNotifiedAt = now;
        }

        if (loans.Count > 0)
        {
            await db.SaveChangesAsync(ct);
            _logger.LogInformation("Sent reminders for {Count} loan(s).", loans.Count);
        }
    }
}
