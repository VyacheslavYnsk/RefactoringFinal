using Microsoft.EntityFrameworkCore;
public class TicketExpirationService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TicketExpirationService> _logger;

    public TicketExpirationService(IServiceScopeFactory scopeFactory, ILogger<TicketExpirationService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var expiredTickets = await context.Tickets
                    .Where(t => t.Status == Status.Reserved &&
                               t.ReservedUntil.HasValue &&
                               t.ReservedUntil.Value < DateTime.UtcNow)
                    .ToListAsync(stoppingToken);

                foreach (var ticket in expiredTickets)
                {
                    ticket.Status = Status.Available;
                    ticket.ReservedUntil = null;
                    ticket.BuyerId = null;
                }

                if (expiredTickets.Any())
                {
                    await context.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation("Released {Count} expired reservations", expiredTickets.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing expired tickets");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}