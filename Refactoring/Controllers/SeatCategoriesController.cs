using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;

namespace Refactoring.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SeatCategoriesController : ControllerBase
    {
        private readonly ISeatCategoryService _seatCategoryService;
        private readonly IUserService _userService;


        public SeatCategoriesController(ISeatCategoryService seatCategoryService, IUserService userService)
        {
            _seatCategoryService = seatCategoryService;
            _userService = userService;
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateCategoryAsync([FromBody] SeatCategoryCreate request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Неверные данные",
                        errors = ModelState.Values.SelectMany(v => v.Errors)
                    });
                }


                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { success = false, message = "Неверный токен" });
                }

                var userRole = await _userService.GetRoleAsync(Guid.Parse(userId));

                if (userRole != Role.Admin)
                {
                    return BadRequest(new { success = false, message = "Пользователь не является админом" });
                }

                var result = await _seatCategoryService.CreateAsync(request);


                return StatusCode(201, result);

            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Внутренняя ошибка сервера" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCategoryListAsync(
            [FromQuery] int page = 0,
            [FromQuery] int size = 20)
        {
            try
            {
                var result = await _seatCategoryService.GetListAsync(page, size);
                return Ok(new
                {
                    success = true,
                    data = result.Data,
                    pagination = result.Pagination
                });
            }
            catch
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Внутренняя ошибка сервера при получении списка категорий"
                });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCategoryById(Guid id)
        {
            try
            {
                var hall = await _seatCategoryService.GetByIdAsync(id);
                return Ok(new { Hall = hall });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Внутренняя ошибка сервера" });
            }
        }

        [HttpPut("{id}")]
        [Authorize]

        public async Task<IActionResult> EditCategoryAsync([FromBody] SeatCategoryUpdate request, Guid id)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Неверные данные",
                        errors = ModelState.Values.SelectMany(v => v.Errors)
                    });
                }
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { success = false, message = "Неверный токен" });
                }

                var userRole = await _userService.GetRoleAsync(Guid.Parse(userId));

                if (userRole != Role.Admin)
                {
                    return BadRequest(new { success = false, message = "Пользователь не является админом" });
                }

                var result = await _seatCategoryService.EditAsync(request, id);
                return Ok(new { data = result });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Внутренняя ошибка сервера" });
            }
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteCategoryAsync(Guid id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { success = false, message = "Неверный токен" });
                }

                var userRole = await _userService.GetRoleAsync(Guid.Parse(userId));

                if (userRole != Role.Admin)
                {
                    return BadRequest(new { success = false, message = "Пользователь не является админом" });
                }

                var result = await _seatCategoryService.DeleteAsync(id);

                if (result)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "Категория успешно удалена"
                    });
                }
                else
                {
                    return StatusCode(500, new
                    {
                        success = false,
                        message = "Не удалось удалить категорию"
                    });
                }
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch 
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Внутренняя ошибка сервера при удалении категории"
                });
            }
        }
    }

}