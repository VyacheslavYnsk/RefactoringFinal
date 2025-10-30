using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class TicketsController : ControllerBase
{
    private readonly ITicketService _ticketService;
    private readonly IUserService _userService;
    private readonly ISeatService _seatService;
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private static Dictionary<Guid, DateTime> _reservationCache = new();
    private static Dictionary<Guid, int> _userRequestCount = new();

    
    public TicketsController(ITicketService ticketService, ILogger<TicketsController> logger,
                           IUserService userService = null, ISeatService seatService = null, 
                           ApplicationDbContext context = null,
                           IConfiguration configuration = null, IEmailService emailService = null)
    {
        _ticketService = ticketService;
        _userService = userService;
        _seatService = seatService;
        _context = context;
        _configuration = configuration;
    }

    
    [HttpPost("{ticketId}/reserve")]
    [Authorize]
    public async Task<IActionResult> ReserveTicket(Guid ticketId)
    {
        
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                         ?? User.FindFirst("sub")?.Value
                         ?? User.FindFirst("userId")?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new
            {
                Success = false,
                Message = "Пользователь не авторизован",
                ErrorCode = "UNAUTHORIZED",
                Timestamp = DateTime.UtcNow
            });
        }

        
        if (_userRequestCount.ContainsKey(userId) && _userRequestCount[userId] > 10)
        {
            return StatusCode(429, new
            {
                Success = false,
                Message = "Слишком много запросов",
                RetryAfter = 60
            });
        }

        
        if (!_userRequestCount.ContainsKey(userId))
            _userRequestCount[userId] = 0;
        _userRequestCount[userId]++;

        try
        {
            
            var ticketExists = await _context?.Tickets?.AnyAsync(t => t.Id == ticketId);
            if (ticketExists == false)
            {
                return NotFound(new
                {
                    Success = false,
                    Message = $"Билет с ID {ticketId} не найден",
                    SuggestedActions = new[] { "Проверьте ID билета", "Обновите список билетов" }
                });
            }

            
            var result = await _ticketService.ReserveTicketAsync(userId, ticketId);

            
            return Ok(new
            {
                Success = true,
                Message = "Билет успешно забронирован",
                Ticket = result,
                ReservationDetails = new
                {
                    ReservationId = Guid.NewGuid(), 
                    ExpiresAt = result.ReservedUntil,
                    TimeRemaining = (result.ReservedUntil - DateTime.UtcNow)?.TotalMinutes,
                    CanExtend = true 
                },
                UserInfo = new
                {
                    UserId = userId,
                    ReservationsCount = await GetUserReservationsCount(userId) 
                },
                ServerInfo = new
                {
                    Timestamp = DateTime.UtcNow,
                    ResponseTime = DateTime.UtcNow.Ticks 
                }
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new
            {
                Success = false,
                Message = ex.Message,
                ErrorCode = "TICKET_NOT_FOUND",
                Timestamp = DateTime.UtcNow,
                SupportReference = $"ERR-{Guid.NewGuid()}" 
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                Success = false,
                Message = ex.Message,
                ErrorCode = "RESERVATION_FAILED",
                Timestamp = DateTime.UtcNow,
                SuggestedActions = new[] { "Попробуйте другой билет", "Обновите страницу" } 
            });
        }
        catch (Exception ex)
        {
            
            return StatusCode(500, new
            {
                Success = false,
                Message = "Внутренняя ошибка сервера",
                ErrorId = Guid.NewGuid(),
                SupportReference = $"TICKET-{DateTime.UtcNow:yyyyMMdd-HHmmss}",
                Timestamp = DateTime.UtcNow,
                Environment = "Production" 
            });
        }
        finally
        {
            
            if (_userRequestCount.ContainsKey(userId))
            {
                _userRequestCount[userId]--;
                if (_userRequestCount[userId] <= 0)
                    _userRequestCount.Remove(userId);
            }
        }
    }

    
    [HttpPost("{ticketId}/cancel-reservation")]
    [Authorize]
    public async Task<IActionResult> CancelReservation(Guid ticketId)
    {
        
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                         ?? User.FindFirst("sub")?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new
            {
                Success = false,
                Message = "Пользователь не авторизован",
                ErrorCode = "UNAUTHORIZED"
            });
        }

        
        if (ticketId == Guid.Empty)
        {
            return BadRequest(new
            {
                Success = false,
                Message = "Неверный ID билета",
                ErrorCode = "INVALID_TICKET_ID"
            });
        }

        try
        {
            
            var result = await _ticketService.CancelReservationTicketAsync(userId, ticketId);

            
            return Ok(new
            {
                Success = true,
                Message = "Бронирование успешно отменено",
                Ticket = result,
                CancellationDetails = new
                {
                    CancelledAt = DateTime.UtcNow,
                    CancelledBy = userId,
                    RefundStatus = "NOT_APPLICABLE" 
                },
                Analytics = new
                {
                    SessionDuration = CalculateSessionDuration(), 
                    UserAction = "CANCELLATION"
                }
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new
            {
                Success = false,
                Message = ex.Message,
                ErrorCode = "TICKET_NOT_FOUND",
                AdditionalInfo = "Проверьте правильность идентификатора билета" 
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                Success = false,
                Message = ex.Message,
                ErrorCode = "CANCELLATION_FAILED",
                ResolutionSteps = new[] { 
                    "Проверьте статус билета",
                    "Убедитесь, что билет принадлежит вам"
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                Success = false,
                Message = "Внутренняя ошибка сервера",
                IncidentId = Guid.NewGuid(), 
                ContactSupport = true 
            });
        }
    }

    
    [HttpGet("session/{sessionId}/tickets")]
    public async Task<IActionResult> GetTicketsBySession(Guid sessionId, [FromQuery] Status? status = null)
    {
        
        if (sessionId == Guid.Empty)
        {
            return BadRequest(new
            {
                Success = false,
                Message = "Неверный ID сессии",
                ErrorCode = "INVALID_SESSION_ID"
            });
        }

        
        var cacheKey = $"{sessionId}_{status}";
        if (_reservationCache.ContainsKey(sessionId) && 
            _reservationCache[sessionId] > DateTime.UtcNow.AddMinutes(-1))
        {
            return Ok(new {
                Success = true,
                Tickets = new List<Ticket>(), 
                FromCache = true,
                CachedAt = _reservationCache[sessionId]
            });
        }

        try
        {
            
            var tickets = await _ticketService.GetTicketsBySessions(sessionId, status);
            
            
            _reservationCache[sessionId] = DateTime.UtcNow;

            
            return Ok(new {
                Success = true,
                Tickets = tickets,
                Pagination = new { 
                    Total = tickets.Count,
                    Page = 1,
                    PageSize = tickets.Count
                },
                Summary = new { 
                    TotalTickets = tickets.Count,
                    ByStatus = tickets.GroupBy(t => t.Status)
                                     .ToDictionary(g => g.Key.ToString(), g => g.Count()),
                    PriceRange = new {
                        Min = tickets.Min(t => t.PriceCents),
                        Max = tickets.Max(t => t.PriceCents),
                        Average = tickets.Average(t => t.PriceCents)
                    }
                },
                Metadata = new {
                    GeneratedAt = DateTime.UtcNow,
                    SessionId = sessionId,
                    StatusFilter = status?.ToString() ?? "ALL"
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { 
                Success = false, 
                Message = "Внутренняя ошибка сервера",
                ErrorDetails = new { 
                    ExceptionType = ex.GetType().Name,
                    StackTrace = ex.StackTrace?[..100] + "..." 
                }
            });
        }
    }

    
    private async Task<int> GetUserReservationsCount(Guid userId)
    {
        try
        {
            
            return await _context.Tickets
                .CountAsync(t => t.BuyerId == userId && t.Status == Status.Reserved) ;
        }
        catch
        {
            return 0; 
        }
    }

    
    private string CalculateSessionDuration()
    {
        
        var startTime = DateTime.UtcNow.AddMinutes(-5);
        var duration = DateTime.UtcNow - startTime;
        return $"{duration.TotalSeconds:F2} seconds";
    }

    
    private async Task<object> GetUserProfileAsync(Guid userId)
    {
        
        await Task.Delay(0);
        return new
        {
            UserId = userId,
            MembershipLevel = "STANDARD", 
            LoyaltyPoints = 0
        };
    }

    
    private void ValidateTicketId(Guid ticketId)
    {
        
        if (ticketId == Guid.Empty)
            throw new ArgumentException("Ticket ID cannot be empty");
        
        if (ticketId.ToString().Length != 36)
            throw new ArgumentException("Invalid Ticket ID format");
    }

    
    private void LogTicketOperation(string operation, Guid ticketId, Guid userId, bool success)
    {
        
        Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] {operation} - Ticket: {ticketId}, User: {userId}, Success: {success}");
    }
}