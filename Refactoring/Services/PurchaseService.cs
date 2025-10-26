using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class PurchaseService : IPurchaseService
{
    private readonly ApplicationDbContext _context;

    public PurchaseService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<(IEnumerable<Purchase> Purchases, int TotalCount)> GetByClientAsync(Guid clientId, int page, int size)
    {
        var query = _context.Purchases
            .Where(p => p.ClientId == clientId)
            .OrderByDescending(p => p.CreatedAt);

        var total = await query.CountAsync();
        var purchases = await query.Skip(page * size).Take(size).ToListAsync();

        return (purchases, total);
    }

    public async Task<Purchase> GetByIdAsync(Guid id)
    {
        var purchase = await _context.Purchases.FindAsync(id);
        if (purchase == null)
            throw new KeyNotFoundException($"������� � ID {id} �� �������");

        return purchase;
    }

    public async Task<Purchase> CreateAsync(Guid clientId, PurchaseCreate dto)
    {
        if (dto.TicketIds == null || dto.TicketIds.Count == 0)
            throw new ArgumentException("�� ������� ������ ��� �������");

        var tickets = await _context.Tickets
            .Where(t => dto.TicketIds.Contains(t.Id))
            .ToListAsync();

        if (tickets.Count != dto.TicketIds.Count)
            throw new InvalidOperationException("��������� ������ �� �������");

        foreach (var ticket in tickets)
        {
            ticket.BuyerId = clientId;
        }

        var totalCents = tickets.Sum(t => t.PriceCents);

        var purchase = new Purchase
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            TicketIds = dto.TicketIds,
            TotalCents = totalCents,
            Status = PurchaseStatus.PENDING,
            CreatedAt = DateTime.UtcNow
        };

        _context.Purchases.Add(purchase);
        await _context.SaveChangesAsync();

        return purchase;
    }

    public async Task<Purchase> CancelAsync(Guid id, Guid clientId)
    {
        var purchase = await _context.Purchases.FindAsync(id);
        if (purchase == null)
            throw new KeyNotFoundException($"������� � ID {id} �� �������");

        if (purchase.ClientId != clientId)
            throw new UnauthorizedAccessException("������� ����������� ������� ������������");

        if (purchase.Status == PurchaseStatus.CANCELLED)
            throw new InvalidOperationException("������� ��� ��������");

        if (purchase.Status == PurchaseStatus.PAID)
            throw new InvalidOperationException("���������� �������� ���������� �����");

        purchase.Status = PurchaseStatus.CANCELLED;

        var tickets = await _context.Tickets
            .Where(t => purchase.TicketIds.Contains(t.Id))
            .ToListAsync();

        foreach (var ticket in tickets)
            ticket.Status = Status.Available;

        await _context.SaveChangesAsync();
        return purchase;
    }
}
