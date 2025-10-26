using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class PaymentService : IPaymentService
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IUserService _userService;

    public PaymentService(ApplicationDbContext context, IEmailService emailService, IUserService userService)
    {
        _context = context;
        _emailService = emailService;
        _userService = userService;
    }

    public async Task<PaymentResponse> ProcessAsync(Guid clientId, PaymentRequest request)
    {
        var purchase = await _context.Purchases
            .FirstOrDefaultAsync(p => p.Id == request.PurchaseId && p.ClientId == clientId);

        if (purchase == null)
            throw new KeyNotFoundException("Покупка не найдена или не принадлежит пользователю");

        var tickets = await _context.Tickets
            .Where(t => purchase.TicketIds.Contains(t.Id))
            .ToListAsync();



        if (purchase.Status != PurchaseStatus.PENDING)
            throw new InvalidOperationException("Покупку можно оплатить только со статусом PENDING");

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            PurchaseId = request.PurchaseId,
            Status = PaymentStatusEnum.SUCCESS,
            CreatedAt = DateTime.UtcNow
        };

        tickets.ForEach(ticket =>
        {
            ticket.Status = Status.Sold;
            ticket.ReservedUntil = null;
        });


        _context.Payments.Add(payment);
        purchase.Status = PurchaseStatus.PAID;

        await _context.SaveChangesAsync();

        var client = await _userService.GetUserByIdAsync(clientId);
        if (string.IsNullOrWhiteSpace(client.Email))
            throw new InvalidOperationException("У пользователя не указана почта, невозможно отправить подтверждение");

        var subject = "Подтверждение покупки билетов";
        var body = $@"
        Здравствуйте, {client.FirstName} {client.LastName}!

        Ваша покупка билетов успешно подтверждена

        Номер покупки: {purchase.Id}
        Сумма: {purchase.TotalCents} рублей
        Количество билетов: {purchase.TicketIds.Count}

        Спасибо, что выбрали наш кинотеатр!
        Хорошего просмотра
        ";

        await _emailService.SendAsync(client.Email, subject, body);

        return new PaymentResponse
        {
            PaymentId = payment.Id,
            Status = payment.Status,
            Message = "Платёж успешно обработан и подтверждение отправлено на почту"
        };
    }

    public async Task<PaymentStatus> GetStatusAsync(Guid paymentId)
    {
        var payment = await _context.Payments.FirstOrDefaultAsync(p => p.Id == paymentId);

        if (payment == null)
            throw new KeyNotFoundException($"Платёж с ID {paymentId} не найден");

        return new PaymentStatus
        {
            PaymentId = payment.Id,
            Status = payment.Status,
            CreatedAt = payment.CreatedAt
        };
    }
}
