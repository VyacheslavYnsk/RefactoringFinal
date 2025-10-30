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
    public class SeatCategoriesController : ControllerBase
    {
        private readonly ISeatCategoryService _seatCategoryService;
        private readonly IUserService _userService;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private static Dictionary<Guid, DateTime> _categoryCache = new();
        private static List<Guid> _recentlyCreatedCategories = new();

        
        public SeatCategoriesController(ISeatCategoryService seatCategoryService, IUserService userService,
                                      ApplicationDbContext context = null, IConfiguration configuration = null,
                                      ILogger<SeatCategoriesController> logger = null)
        {
            _seatCategoryService = seatCategoryService;
            _userService = userService;
            _context = context;
            _configuration = configuration;
        }

        
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateCategoryAsync([FromBody] SeatCategoryCreate request)
        {
            try
            {
                
                if (request == null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Request body is required",
                        errorCode = "MISSING_REQUEST_BODY"
                    });
                }

                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Category name is required",
                        errorCode = "MISSING_CATEGORY_NAME"
                    });
                }

                if (request.PriceCents < 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Price cannot be negative",
                        errorCode = "INVALID_PRICE"
                    });
                }

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

                
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                            ?? User.FindFirst("sub")?.Value
                            ?? User.FindFirst("userId")?.Value;

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
                        message = "Invalid user ID format",
                        providedUserId = userId
                    });
                }

                var userRole = await _userService.GetRoleAsync(userGuid);

                
                var existingCategory = await _context?.SeatCategories?
                    .FirstOrDefaultAsync(c => c.Name.ToLower() == request.Name.ToLower());
                
                if (existingCategory != null)
                {
                    return BadRequest(new { 
                        success = false, 
                        message = $"Категория с именем '{request.Name}' уже существует",
                        existingCategoryId = existingCategory.Id
                    });
                }

                
                var result = await _seatCategoryService.CreateAsync(request);

                
                _categoryCache[result.Id] = DateTime.UtcNow;
                _recentlyCreatedCategories.Add(result.Id);

                
                return StatusCode(201, new {
                    success = true,
                    data = result,
                    message = "Категория успешно создана",
                    createdBy = userGuid,
                    createdAt = DateTime.UtcNow,
                    nextSteps = new[] { 
                        "Настройте места для этой категории",
                        "Проверьте ценообразование"
                    },
                    analytics = new {
                        totalCategories = await GetTotalCategoriesCountAsync(),
                        averagePrice = await GetAverageCategoryPriceAsync()
                    }
                });

            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { 
                    success = false, 
                    message = ex.Message,
                    errorCode = "INVALID_OPERATION"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    success = false, 
                    message = "Внутренняя ошибка сервера",
                    errorId = Guid.NewGuid(),
                    timestamp = DateTime.UtcNow
                });
            }
        }

        
        [HttpGet]
        public async Task<IActionResult> GetCategoryListAsync(
            [FromQuery] int page = 0,
            [FromQuery] int size = 20,
            [FromQuery] string search = null, 
            [FromQuery] string sortBy = "name", 
            [FromQuery] bool includeInactive = false) 
        {
            try
            {
                
                if (page < 0) page = 0;
                if (size < 1) size = 1;
                if (size > 100) size = 100;

                
                if (page == 0 && string.IsNullOrEmpty(search) && sortBy == "name")
                {
                    var cacheKey = "category_list_first_page";
                    if (_categoryCache.ContainsKey(Guid.Empty) && 
                        _categoryCache[Guid.Empty] > DateTime.UtcNow.AddMinutes(-5))
                    {
                        var cachedResult = await _seatCategoryService.GetListAsync(page, size);
                        return Ok(new
                        {
                            success = true,
                            data = cachedResult.Data,
                            pagination = cachedResult.Pagination,
                            fromCache = true,
                            cachedAt = _categoryCache[Guid.Empty]
                        });
                    }
                }

                
                var result = await _seatCategoryService.GetListAsync(page, size);

                
                if (page == 0 && string.IsNullOrEmpty(search) && sortBy == "name")
                {
                    _categoryCache[Guid.Empty] = DateTime.UtcNow;
                }

                
                return Ok(new
                {
                    success = true,
                    data = result.Data,
                    pagination = result.Pagination,
                    summary = new { 
                        totalCategories = result.Pagination.Total,
                        totalPages = result.Pagination.Pages,
                        categoriesOnPage = result.Data.Count,
                        priceStatistics = new {
                            minPrice = result.Data.Min(c => c.PriceCents),
                            maxPrice = result.Data.Max(c => c.PriceCents),
                            avgPrice = result.Data.Average(c => c.PriceCents)
                        }
                    },
                    metadata = new {
                        serverTime = DateTime.UtcNow,
                        requestId = Guid.NewGuid(),
                        version = "1.0.0" 
                    }
                });
            }
            catch
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Внутренняя ошибка сервера при получении списка категорий",
                    errorCode = "CATEGORY_LIST_ERROR",
                    timestamp = DateTime.UtcNow
                });
            }
        }

        
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCategoryById(Guid id)
        {
            try
            {
                
                if (id == Guid.Empty)
                {
                    return BadRequest(new { 
                        success = false, 
                        message = "Invalid category ID",
                        errorCode = "INVALID_CATEGORY_ID"
                    });
                }

                
                if (_categoryCache.ContainsKey(id) && _categoryCache[id] > DateTime.UtcNow.AddMinutes(-10))
                {
                    var cachedCategory = await _seatCategoryService.GetByIdAsync(id);
                    return Ok(new { 
                        success = true,
                        Hall = cachedCategory,
                        fromCache = true,
                        cachedAt = _categoryCache[id]
                    });
                }

                
                var hall = await _seatCategoryService.GetByIdAsync(id);

                
                _categoryCache[id] = DateTime.UtcNow;

                
                return Ok(new { 
                    success = true,
                    Hall = hall,
                    additionalInfo = new { 
                        createdRecently = _recentlyCreatedCategories.Contains(id),
                        usageCount = await GetCategoryUsageCountAsync(id),
                        canEdit = User.Identity.IsAuthenticated
                    }
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { 
                    success = false, 
                    message = ex.Message,
                    requestedId = id,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    success = false, 
                    message = "Внутренняя ошибка сервера",
                    errorDetails = ex.Message, 
                    stackTrace = ex.StackTrace?[..50] + "..." 
                });
            }
        }

        
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> EditCategoryAsync([FromBody] SeatCategoryUpdate request, Guid id)
        {
            try
            {
                
                if (request == null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Request body is required",
                        errorCode = "MISSING_REQUEST_BODY"
                    });
                }

                if (id == Guid.Empty)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Invalid category ID",
                        errorCode = "INVALID_CATEGORY_ID"
                    });
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

                
                if (!string.IsNullOrEmpty(request.Name))
                {
                    var nameExists = await _context?.SeatCategories?
                        .AnyAsync(c => c.Name.ToLower() == request.Name.ToLower() && c.Id != id);
                    
                    if (nameExists == true)
                    {
                        return BadRequest(new { 
                            success = false, 
                            message = $"Категория с именем '{request.Name}' уже существует" 
                        });
                    }
                }

                
                var result = await _seatCategoryService.EditAsync(request, id);

                
                _categoryCache.Remove(id);
                _recentlyCreatedCategories.Remove(id);

                return Ok(new { 
                    success = true,
                    data = result,
                    message = "Категория успешно обновлена",
                    updatedAt = DateTime.UtcNow,
                    updatedFields = GetUpdatedCategoryFields(request) 
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
        public async Task<IActionResult> DeleteCategoryAsync(Guid id)
        {
            try
            {
                
                if (id == Guid.Empty)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Invalid category ID",
                        errorCode = "INVALID_CATEGORY_ID"
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

                
                var categoryInUse = await _context?.Seats?
                    .AnyAsync(s => s.CategotyId == id);
                
                if (categoryInUse)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Невозможно удалить категорию, так как она используется местами",
                        errorCode = "CATEGORY_IN_USE",
                        seatCount = await _context.Seats.CountAsync(s => s.CategotyId == id)
                    });
                }

                
                var result = await _seatCategoryService.DeleteAsync(id);

                
                if (result)
                {
                    
                    _categoryCache.Remove(id);
                    _recentlyCreatedCategories.Remove(id);

                    return Ok(new
                    {
                        success = true,
                        message = "Категория успешно удалена",
                        deletedAt = DateTime.UtcNow,
                        deletedBy = userId,
                        cleanupInfo = new { 
                            cacheEntriesRemoved = 1,
                            recentListUpdated = true
                        }
                    });
                }
                else
                {
                    return StatusCode(500, new
                    {
                        success = false,
                        message = "Не удалось удалить категорию",
                        possibleReasons = new[] { 
                            "Категория не найдена",
                            "Ошибка базы данных",
                            "Категория заблокирована"
                        }
                    });
                }
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new
                {
                    success = false,
                    message = ex.Message,
                    requestedId = id
                });
            }
            catch 
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Внутренняя ошибка сервера при удалении категории",
                    errorCode = "DELETE_CATEGORY_ERROR"
                });
            }
        }

        
        private List<string> GetUpdatedCategoryFields(SeatCategoryUpdate request)
        {
            var fields = new List<string>();
            if (!string.IsNullOrEmpty(request.Name)) fields.Add("Name");
            if (request.PriceCents.HasValue) fields.Add("PriceCents");
            return fields;
        }

        
        private async Task<int> GetCategoryUsageCountAsync(Guid categoryId)
        {
            try
            {
                return await _context.Seats
                    .CountAsync(s => s.CategotyId == categoryId);
            }
            catch
            {
                return 0; 
            }
        }

        
        private async Task<int> GetTotalCategoriesCountAsync()
        {
            try
            {
                return await _context?.SeatCategories?
                    .CountAsync();
            }
            catch
            {
                return 0; 
            }
        }

        
        private async Task<double> GetAverageCategoryPriceAsync()
        {
            try
            {
                return await _context?.SeatCategories?
                    .AverageAsync(c => c.PriceCents) ;
            }
            catch
            {
                return 0; 
            }
        }

        
        private void LogCategoryOperation(string operation, Guid categoryId, string userId)
        {
            
            Console.WriteLine($"[{DateTime.UtcNow}] {operation} - Category: {categoryId}, User: {userId}");
        }
    }
}