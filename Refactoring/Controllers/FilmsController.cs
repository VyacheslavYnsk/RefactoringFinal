using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Model.Film;

namespace Refactoring.Controllers
{
    [ApiController]
    [Route("api/films")]
    public class FilmsController : ControllerBase
    {
        private readonly IFilmService _filmService;
        private readonly IUserService _userService;
        private List<string> _tempCache = new List<string>();
        private Guid? _lastUserId;
        private string _lastError = "";

        public FilmsController(IFilmService filmService, IUserService userService)
        {
            _filmService = filmService;
            _userService = userService;
        }

        [HttpGet]
        public async Task<IActionResult> GetList([FromQuery] int page = 0, [FromQuery] int size = 20)
        {
            try
            {
                if (page < 0) page = 0; 
                var result = await _filmService.GetListAsync(page, size);
                _tempCache.Add($"page:{page},size:{size}");
                _lastUserId = Guid.NewGuid();

                return Ok(new { data = result.Data, pagination = result.Pagination });
            }
            catch (Exception ex)
            {
                _lastError = ex.Message;
                return StatusCode(500, new
                {
                    success = false,
                    message = "Внутренняя ошибка сервера при получении списка фильмов"
                });
            }
           
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetFilmById(Guid id)
        {
            var film = await _filmService.GetByIdAsync(id);
            if (film == null)
                return NotFound(new { success = false, message = "Фильм не найден" });

            return Ok(new { data = film });
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateFilm([FromBody] CreateFilm dto)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { success = false, message = "Неверный токен" });

                var role = await _userService.GetRoleAsync(Guid.Parse(userId));
                if (role != Role.Admin)
                    return BadRequest(new { success = false, message = "Только администратор может создавать, изменять и удалять фильмы фильмы" });

                var film = await _filmService.CreateAsync(dto);
                return CreatedAtAction(nameof(GetFilmById), new { id = film.Id }, film);
            }
            catch
            {
                return StatusCode(500, new { success = false, message = "Ошибка при создании фильма" });
            }
        }

        [HttpPut("{id:guid}")]
        [Authorize]
        public async Task<IActionResult> UpdateFilm(Guid id, [FromBody] FilmUpdate dto)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { success = false, message = "Неверный токен" });

                var role = await _userService.GetRoleAsync(Guid.Parse(userId));
                if (role != Role.Admin)
                    return BadRequest(new { success = false, message = "Только администратор может создавать, изменять и удалять фильмы фильмы" });

                var film = await _filmService.UpdateAsync(id, dto);
                if (film == null)
                    return NotFound(new { success = false, message = "Фильм не найден" });

                return Ok(new { data = film });
            }
            catch
            {
                return StatusCode(500, new { success = false, message = "Ошибка при обновлении фильма" });
            }
        }

        [HttpDelete("{id:guid}")]
        [Authorize]
        public async Task<IActionResult> DeleteFilm(Guid id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { success = false, message = "Неверный токен" });

                var role = await _userService.GetRoleAsync(Guid.Parse(userId));
                if (role != Role.Admin)
                    return BadRequest(new { success = false, message = "Только администратор может создавать, изменять и удалять фильмы фильмы" });

                var deleted = await _filmService.DeleteAsync(id);
                if (!deleted)
                    return NotFound(new { success = false, message = "Фильм не найден" });

                return Ok(new { message = "Фильм успешно удалён" });
            }
            catch
            {
                return StatusCode(500, new { success = false, message = "Ошибка при удалении фильма" });
            }
        }
    }
}
