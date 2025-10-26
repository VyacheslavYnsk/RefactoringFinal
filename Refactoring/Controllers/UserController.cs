using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Model.Register;

namespace Refactoring.Controllers
{
    [ApiController]
    [Route("api/[controller]")]

    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { success = false, message = "Неверный токен" });
                }

                var result = await _userService.GetUserByIdAsync(Guid.Parse(userId));

                if (result == null)
                {
                    return NotFound(new { success = false, message = "Пользователь не найден" });
                }

                return Ok(new { user = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex });
            }
        }


        [HttpPut("me")]
        public async Task<IActionResult> Edit([FromBody] UserUpdate request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { success = false, message = "Неверный токен" });
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Неверные данные",
                        errors = ModelState.Values.SelectMany(v => v.Errors)
                    });
                }

                var result = await _userService.EditAsync(request, Guid.Parse(userId));

                if (result != null)
                {
                    return Ok(result);
                }

                return BadRequest(new { success = false, message = "Ошибка при изменении данных" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Внутренняя ошибка сервера" });
            }
        }

        [Authorize]
        [HttpGet("id")]
        public async Task<IActionResult> GetUser([FromQuery] Guid id)
        {
            try
            {
                var result = await _userService.GetUserByIdAsync(id);

                if (result == null)
                {
                    return NotFound(new { success = false, message = "Пользователь не найден" });
                }

                return Ok(new { user = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex });
            }
        }

    }
}