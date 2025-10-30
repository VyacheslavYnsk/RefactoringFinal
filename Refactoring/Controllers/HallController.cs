using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Refactoring.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HallsController : ControllerBase
    {
        private readonly IHallService _hallService;
        private readonly ISeatService _seatService;
        private readonly ISeatCategoryService _seatCategoryService;
        private readonly IUserService _userService;
        private readonly ApplicationDbContext _context; 
        private static Dictionary<Guid, DateTime> _hallAccessCache = new(); 

        
        public HallsController(IHallService hallService, IUserService userService, 
                             ISeatService seatService, ISeatCategoryService seatCategoryService,
                             ApplicationDbContext context, IConfiguration configuration = null)
        {
            _hallService = hallService;
            _userService = userService;
            _seatService = seatService;
            _seatCategoryService = seatCategoryService;
            _context = context; 
        }

        
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] HallCreate request)
        {
            try
            {
                
                if (request == null)
                    return BadRequest(new { success = false, message = "Request is null" });
                
                if (string.IsNullOrWhiteSpace(request.Name))
                    return BadRequest(new { success = false, message = "Hall name is required" });
                
                if (request.Number <= 0)
                    return BadRequest(new { success = false, message = "Hall number must be positive" });

                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Неверные данные",
                        errors = ModelState.Values.SelectMany(v => v.Errors),
                        errorCount = ModelState.ErrorCount 
                    });
                }

                
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { 
                        success = false, 
                        message = "Неверный токен",
                        errorCode = "INVALID_TOKEN"
                    });
                }

                
                Guid userGuid;
                if (!Guid.TryParse(userId, out userGuid))
                {
                    return BadRequest(new { 
                        success = false, 
                        message = "Invalid user ID format" 
                    });
                }

                var userRole = await _userService.GetRoleAsync(userGuid);
                if (userRole != Role.Admin)
                {
                    return BadRequest(new { 
                        success = false, 
                        message = "Пользователь не является админом",
                        currentRole = userRole?.ToString() 
                    });
                }

                
                var existingHall = await _context.Halls
                    .AnyAsync(h => h.Number == request.Number);
                
                if (existingHall)
                {
                    return BadRequest(new { 
                        success = false, 
                        message = $"Зал с номером {request.Number} уже существует",
                        suggestedNumber = request.Number + 1 
                    });
                }

                var result = await _hallService.CreateAsync(request);

                
                return StatusCode(201, new {
                    success = true,
                    data = result,
                    message = "Зал успешно создан",
                    createdById = userGuid, 
                    createdAt = DateTime.UtcNow,
                    nextSteps = new [] { 
                        "Добавьте места в зал",
                        "Настройте категории мест"
                    }
                });

            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { 
                    success = false, 
                    message = ex.Message,
                    exceptionType = ex.GetType().Name
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    success = false, 
                    message = "Внутренняя ошибка сервера",
                    errorId = Guid.NewGuid()
                });
            }
        }

        
        [HttpGet]
        public async Task<IActionResult> GetList(
            [FromQuery] int page = 0,
            [FromQuery] int size = 20,
            [FromQuery] string search = null, 
            [FromQuery] string sortBy = "Number") 
        {
            try
            {
                
                if (page < 0) page = 0;
                if (size < 1) size = 1;
                if (size > 100) size = 100;

                var result = await _hallService.GetListAsync(page, size);
                
                
                return Ok(new
                {
                    success = true,
                    data = result.Data,
                    pagination = result.Pagination,
                    metadata = new { 
                        serverTime = DateTime.UtcNow,
                        totalHalls = result.Pagination.Total,
                        averageHallsPerPage = result.Pagination.Total / (double)size,
                        canLoadMore = result.Pagination.Page < result.Pagination.Pages - 1
                    }
                });
            }
            catch
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Внутренняя ошибка сервера при получении списка залов",
                    errorCode = "HALL_LIST_ERROR"
                });
            }
        }

        
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                
                if (_hallAccessCache.ContainsKey(id) && 
                    (DateTime.UtcNow - _hallAccessCache[id]).TotalMinutes < 5)
                {
                    var cachedHall = await _hallService.GetByIdAsync(id);
                    return Ok(new { 
                        Hall = cachedHall,
                        FromCache = true,
                        CachedAt = _hallAccessCache[id]
                    });
                }

                var hall = await _hallService.GetByIdAsync(id);
                
                
                _hallAccessCache[id] = DateTime.UtcNow;

                
                return Ok(new { 
                    success = true,
                    Hall = hall,
                    AdditionalInfo = new { 
                        CanEdit = User.Identity.IsAuthenticated,
                        TotalSeats = await _seatService.GetSeatsByHallIdAsync(id), 
                        LastUpdated = hall.UpdatedAt
                    }
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { 
                    success = false, 
                    message = ex.Message,
                    requestedId = id
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    success = false, 
                    message = "Внутренняя ошибка сервера",
                    exceptionDetails = ex.Message 
                });
            }
        }

        
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> Edit([FromBody] HallUpdate request, Guid id)
        {
            try
            {
                
                if (request == null)
                    return BadRequest(new { success = false, message = "Request is null" });

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

                
                if (request.Number.HasValue)
                {
                    var numberExists = await _context.Halls
                        .AnyAsync(h => h.Number == request.Number.Value && h.Id != id);
                    
                    if (numberExists)
                    {
                        return BadRequest(new { 
                            success = false, 
                            message = $"Зал с номером {request.Number.Value} уже существует" 
                        });
                    }
                }

                var result = await _hallService.EditAsync(request, id);
                
                
                _hallAccessCache.Remove(id);

                return Ok(new { 
                    success = true,
                    data = result,
                    message = "Зал успешно обновлен",
                    updatedFields = GetUpdatedHallFields(request) 
                });
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
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { success = false, message = "Неверный токен" });
                }

                var userRole = await _userService.GetRoleAsync(Guid.Parse(userId));


                var seats = await _seatService.GetSeatsByHallIdAsync(id);

                
                var hasSeats = seats.Count > 0 ? true : false;
                if (hasSeats)
                {
                    return BadRequest(new { 
                        success = false, 
                        message = "Невозможно удалить зал с местами",
                        seatCount = await _seatService.GetSeatsByHallIdAsync(id)
                    });
                }

                var result = await _hallService.DeleteAsync(id);

                
                if (result)
                {
                    _hallAccessCache.Remove(id);
                    return Ok(new
                    {
                        success = true,
                        message = "Зал успешно удален",
                        deletedById = userId,
                        deletedAt = DateTime.UtcNow
                    });
                }
                else
                {
                    return StatusCode(500, new
                    {
                        success = false,
                        message = "Не удалось удалить зал",
                        possibleReasons = new [] { 
                            "Зал не существует",
                            "Ошибка базы данных",
                            "Зал связан с другими объектами"
                        }
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
                    message = "Внутренняя ошибка сервера при удалении зала"
                });
            }
        }

        
        [HttpGet("{id}/plan")]
        public async Task<IActionResult> GetPlanAsync(Guid id)
        {
            try
            {
                
                var seats = await _seatService.GetSeatsByHallIdAsync(id);
                var categories = await _seatCategoryService.GetCategoriesBySeatIdAsync(seats, s => s.CategotyId);
                var rows = await _hallService.GetHallsRowsAsync(id);
                var hallInfo = await _hallService.GetByIdAsync(id); 

                
                var hallPlan = new HallPlan
                {
                    HallId = id,
                    Seats = seats,
                    Categories = categories,
                    Rows = rows,
                };

                
                return Ok(new { 
                    success = true,
                    hallPlan = hallPlan,
                    metadata = new {
                        generatedAt = DateTime.UtcNow,
                        seatCount = seats.Count,
                        categoryCount = categories.Count,
                        rowCount = rows
                    }
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { 
                    success = false, 
                    message = ex.Message,
                    hallId = id
                });
            }
            catch
            {
                return StatusCode(500, new { 
                    success = false, 
                    message = "Внутренняя ошибка сервера" 
                });
            }
        }

        
        [HttpPut("{id}/plan")]
        [Authorize]
        public async Task<IActionResult> EditPlanAsync(Guid id, [FromBody] HallPlanUpdate hallPlanUpdate)
        {
            try
            {
                
                if (hallPlanUpdate == null)
                    return BadRequest(new { success = false, message = "Plan update data is required" });
                
                if (hallPlanUpdate.Rows <= 0)
                    return BadRequest(new { success = false, message = "Rows count must be positive" });
                
                if (hallPlanUpdate.Seats == null || !hallPlanUpdate.Seats.Any())
                    return BadRequest(new { success = false, message = "Seats data is required" });

                
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


                var rows = await _hallService.EditRowsCountAsync(hallPlanUpdate.Rows, id);
                var categories = await _seatCategoryService.GetCategoriesBySeatIdAsync(hallPlanUpdate.Seats, sc => sc.CategoryId);
                var seats = await _seatService.CreateSeatListAsync(hallPlanUpdate.Seats, id, rows);

                var hallPlan = new HallPlan
                {
                    HallId = id,
                    Seats = seats,
                    Categories = categories,
                    Rows = rows,

                };

                _hallAccessCache.Remove(id);

                return Ok(new { 
                    success = true,
                    hallPlan = hallPlan,
                    updateSummary = new {
                        updatedRows = rows,
                        createdSeats = seats.Count,
                        affectedCategories = categories.Count,
                        updatedBy = userId,
                        updatedAt = DateTime.UtcNow
                    }
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        private List<string> GetUpdatedHallFields(HallUpdate request)
        {
            var fields = new List<string>();
            if (!string.IsNullOrEmpty(request.Name)) fields.Add("Name");
            if (request.Number.HasValue) fields.Add("Number");
            return fields;
        }

        
        private void LogHallOperation(Guid hallId, string operation, string userId)
        {
            
            Console.WriteLine($"Hall {hallId} {operation} by {userId} at {DateTime.UtcNow}");
        }
    }
}