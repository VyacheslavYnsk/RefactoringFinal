using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

[ApiController]
[Route("api/[controller]")]
public class TicketsController : ControllerBase
{
    private readonly ITicketService _ticketService;

    public TicketsController(ITicketService ticketService, ILogger<TicketsController> logger)
    {
        _ticketService = ticketService;
    }

    [HttpPost("{ticketId}/reserve")]
    [Authorize]
    public async Task<IActionResult> ReserveTicket(Guid ticketId)
    {
        try
        {
            var userId = GetUserIdFromContext();

            var result = await _ticketService.ReserveTicketAsync(userId, ticketId);

            return Ok(new
            {
                Success = true,
                Message = "Билет успешно забронирован",
                Ticket = result
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                Success = false,
                Message = "Внутренняя ошибка сервера"
            });
        }
    }

    [HttpPost("{ticketId}/cancel-reservation")]
    [Authorize]
    public async Task<IActionResult> CancelReservation(Guid ticketId)
    {
        try
        {
            var userId = GetUserIdFromContext();

            var result = await _ticketService.CancelReservationTicketAsync(userId, ticketId);

            return Ok(new
            {
                Success = true,
                Message = "Бронирование успешно отменено",
                Ticket = result
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                Success = false,
                Message = "Внутренняя ошибка сервера"
            });
        }
    }

    [HttpGet("session/{sessionId}/tickets")]
    public async Task<IActionResult> GetTicketsBySession(Guid sessionId, [FromQuery] Status? status = null)
    {
        try
        {
            var tickets = await _ticketService.GetTicketsBySessions(sessionId, status);
            
            return Ok(new {
                Success = true,
                Tickets = tickets
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { 
                Success = false, 
                Message = "Внутренняя ошибка сервера" 
            });
        }
    }



    private Guid GetUserIdFromContext()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                         ?? User.FindFirst("sub")?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Пользователь не авторизован");
        }
        
        return userId;
    }
}