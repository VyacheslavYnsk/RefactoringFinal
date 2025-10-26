using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

[ApiController]
[Route("sessions")]
public class SessionsController : ControllerBase
{
    private readonly ISessionService _sessionService;
    private readonly IUserService _userService;

    private readonly IHallService _hallService;

    private readonly ISeatCategoryService _seatCategoryService;

    private readonly ISeatService _seatService;
    private readonly ITicketService _ticketService;






    public SessionsController(ISessionService sessionService, IUserService userService, IHallService hallService, ISeatCategoryService seatCategoryService,
    ISeatService seatService, ITicketService ticketService)
    {
        _sessionService = sessionService;
        _userService = userService;
        _hallService = hallService;
        _seatCategoryService = seatCategoryService;
        _seatService = seatService;
        _ticketService = ticketService;

    }

    [HttpGet]
    public async Task<IActionResult> GetSessions([FromQuery] int page = 0, [FromQuery] int size = 20, [FromQuery] Guid? filmId = null, [FromQuery] DateTime? date = null)
    {
        try
        {
            var (sessions, total) = await _sessionService.GetAllAsync(page, size, filmId, date);

            return Ok(new
            {
                data = sessions,
                pagination = new
                {
                    page,
                    limit = size,
                    total,
                    pages = (int)Math.Ceiling(total / (double)size)
                }
            });
        }
        catch
        {
            return StatusCode(500, new { success = false, message = "Внутренняя ошибка сервера при получении списка сеансов" });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetSessionById(Guid id)
    {
        try
        {
            var session = await _sessionService.GetByIdAsync(id);
            if (session == null)
            {
                return NotFound(new { success = false, message = $"Сеанс с ID {id} не найден" });
            }
            return Ok(session);
        }
        catch
        {
            return StatusCode(500, new { success = false, message = "Внутренняя ошибка сервера при получении данных о сеансе" });
        }
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateSession([FromBody] SessionCreate dto)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized(new { success = false, message = "Неверный токен" });

            var userRole = await _userService.GetRoleAsync(Guid.Parse(userId));
            if (userRole != Role.Admin) return BadRequest(new { success = false, message = "Только администратор может создавать сеансы" });

            var hall = await _hallService.GetByIdAsync(dto.HallId);
            if (hall == null)
            {
                return BadRequest(new { success = false, message = "Зала с таким id не существует" });
            }

            

            var session = await _sessionService.CreateAsync(dto);

            var seats = await _seatService.GetSeatsByHallIdAsync(dto.HallId);


            var categories = await _seatCategoryService.GetCategoriesBySeatIdAsync(seats, s => s.CategotyId);;

            var isTicketCreated = await _ticketService.CreateTicketAsync(seats, categories, session.Id);

            if (!isTicketCreated)
            {
                return BadRequest(new { success = false, message = "Ошибка при создании билета"});
            }

            return CreatedAtAction(nameof(GetSessionById), new { id = session.Id }, session);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch
        {
            return StatusCode(500, new { success = false, message = "Внутренняя ошибка сервера при создании сеанса" });
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> UpdateSession(Guid id, [FromBody] SessionUpdate dto)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized(new { success = false, message = "Неверный токен" });

            var userRole = await _userService.GetRoleAsync(Guid.Parse(userId));
            if (userRole != Role.Admin) return BadRequest(new { success = false, message = "Только администратор может редактировать сеансы" });

            var session = await _sessionService.UpdateAsync(id, dto);
            if (session == null) return NotFound(new { success = false, message = $"Сеанс с ID {id} не найден" });


            if (session.HallId != dto.HallId && dto.HallId != null)
            {
                Guid hallId = dto.HallId.Value;
                await _ticketService.DeleteTicketsBySessionAsync(id);

                var seats = await _seatService.GetSeatsByHallIdAsync(hallId);

                var categories = await _seatCategoryService.GetCategoriesBySeatIdAsync(seats, s => s.CategotyId); ;

                var isTicketCreated = await _ticketService.CreateTicketAsync(seats, categories, session.Id);

                if (!isTicketCreated)
                {
                    return BadRequest(new { success = false, message = "Ошибка при создании билета" });
                }

            }

            return Ok(new { success = true, data = session });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch
        {
            return StatusCode(500, new { success = false, message = "Внутренняя ошибка сервера при создании сеанса" });
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteSession(Guid id)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized(new { success = false, message = "Неверный токен" });

            var userRole = await _userService.GetRoleAsync(Guid.Parse(userId));
            if (userRole != Role.Admin) return BadRequest(new { success = false, message = "Только администратор может удалять сеансы" });

            var deleted = await _sessionService.DeleteAsync(id);
            if (!deleted) return NotFound(new { success = false, message = $"Сеанс с ID {id} не найден" });

            return Ok(new { success = true, message = "Сеанс успешно удалён" });
        }
        catch
        {
            return StatusCode(500, new { success = false, message = "Внутренняя ошибка сервера при удалении сеанса" });
        }
    }
}
