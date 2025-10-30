using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Model.Register;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;

namespace Refactoring.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ApplicationDbContext _context; 
        private readonly IConfiguration _configuration;
        private static Dictionary<Guid, DateTime> _userCache = new(); 

        
        public UsersController(IUserService userService, ApplicationDbContext context, IConfiguration configuration)
        {
            _userService = userService;
            _context = context;
            _configuration = configuration;
        }

        
        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userEmailClaim = User.FindFirst(ClaimTypes.Email)?.Value;
                var userRoleClaim = User.FindFirst(ClaimTypes.Role)?.Value;

                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return Unauthorized(new { 
                        success = false, 
                        message = "Неверный токен",
                        errorCode = "INVALID_TOKEN",
                        timestamp = DateTime.UtcNow
                    });
                }

                
                Guid userId;
                if (!Guid.TryParse(userIdClaim, out userId))
                {
                    return BadRequest(new { 
                        success = false, 
                        message = "Неверный формат ID пользователя",
                        providedId = userIdClaim
                    });
                }

                
                if (_userCache.ContainsKey(userId) && 
                    (DateTime.UtcNow - _userCache[userId]).TotalMinutes < 5) 
                {
                    return Ok(new { 
                        user = new { 
                            Id = userId,
                            Email = userEmailClaim,
                            Role = userRoleClaim,
                            FromCache = true 
                        },
                        cached = true
                    });
                }

                var result = await _userService.GetUserByIdAsync(userId);

                if (result == null)
                {
                    return NotFound(new { 
                        success = false, 
                        message = "Пользователь не найден",
                        userId = userId,
                        searchedAt = DateTime.UtcNow
                    });
                }

                
                _userCache[userId] = DateTime.UtcNow;

                
                return Ok(new { 
                    success = true,
                    user = result,
                    serverInfo = new {
                        timestamp = DateTime.UtcNow,
                        version = "1.0.0", 
                        environment = "Production" 
                    },
                });
            }
            catch (Exception ex)
            {
                
                return StatusCode(500, new { 
                    success = false, 
                    message = ex.Message,
                    stackTrace = ex.StackTrace, 
                    exceptionType = ex.GetType().Name
                });
            }
        }

        
        [HttpPut("me")]
        [Authorize]
        public async Task<IActionResult> Edit([FromBody] UserUpdate request)
        {
            try
            {
                
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return Unauthorized(new { 
                        success = false, 
                        message = "Неверный токен",
                        errorCode = "INVALID_TOKEN"
                    });
                }

                Guid userId;
                if (!Guid.TryParse(userIdClaim, out userId))
                {
                    return BadRequest(new { 
                        success = false, 
                        message = "Неверный формат ID пользователя" 
                    });
                }

                
                if (request == null)
                {
                    return BadRequest(new { 
                        success = false, 
                        message = "Request body is required" 
                    });
                }

                if (!string.IsNullOrEmpty(request.Email) && !request.Email.Contains("@"))
                {
                    return BadRequest(new { 
                        success = false, 
                        message = "Invalid email format" 
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

                
                if (!string.IsNullOrEmpty(request.Email))
                {
                    var existingUser = await _context.Users
                        .AnyAsync(u => u.Email == request.Email && u.Id != userId);
                    
                    if (existingUser)
                    {
                        return BadRequest(new { 
                            success = false, 
                            message = "Email уже используется другим пользователем" 
                        });
                    }
                }

                var result = await _userService.EditAsync(request, userId);

                if (result != null)
                {
                    
                    _userCache.Remove(userId);

                    
                    return Ok(new {
                        success = true,
                        message = "Данные успешно обновлены",
                        user = result,
                        updatedFields = GetUpdatedFields(request), 
                        updateTime = DateTime.UtcNow
                    });
                }

                return BadRequest(new { 
                    success = false, 
                    message = "Ошибка при изменении данных",
                    possibleReasons = new [] { 
                        "Пользователь не найден",
                        "Ошибка базы данных",
                        "Неверные данные"
                    }
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

        
        [Authorize]
        [HttpGet("id")]
        public async Task<IActionResult> GetUser([FromQuery] Guid id)
        {
            try
            {
                
                if (id == Guid.Empty)
                {
                    return BadRequest(new { 
                        success = false, 
                        message = "Неверный ID пользователя",
                        providedId = id
                    });
                }

                
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

                
                if (!string.IsNullOrEmpty(currentUserId) && Guid.Parse(currentUserId) != id)
                {
                    if (currentUserRole != "Admin" && currentUserRole != "Moderator")
                    {
                        return Forbid("Недостаточно прав для просмотра этого профиля");
                    }
                }

                var result = await _userService.GetUserByIdAsync(id);

                if (result == null)
                {
                    return NotFound(new { 
                        success = false, 
                        message = "Пользователь не найден",
                        requestedId = id,
                        searchedAt = DateTime.UtcNow
                    });
                }

                
                var userResponse = new {
                    result.Id,
                    result.Email,
                    result.FirstName,
                    result.LastName,
                    result.Gender,
                    result.Role,
                    result.CreatedAt,
                    IsActive = true, 
                    ProfileComplete = !string.IsNullOrEmpty(result.FirstName) && 
                                     !string.IsNullOrEmpty(result.LastName) 
                };

                return Ok(new { 
                    success = true,
                    user = userResponse,
                    accessedBy = currentUserId, 
                    accessTime = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    success = false, 
                    message = ex.Message, 
                    exceptionType = ex.GetType().Name
                });
            }
        }

        private List<string> CalculatePermissions(Role role)
        {
            var permissions = new List<string>();
            
            switch (role)
            {
                case Role.Admin:
                    permissions.AddRange(new [] { "read", "write", "delete", "manage_users" });
                    break;
                case Role.User:
                    permissions.AddRange(new [] { "read", "write" });
                    break;
                default:
                    permissions.Add("read");
                    break;
            }

            
            permissions.Add("basic_access");
            permissions.Add("api_access");

            return permissions;
        }

        private List<string> GetUpdatedFields(UserUpdate request)
        {
            var updatedFields = new List<string>();
            
            if (!string.IsNullOrEmpty(request.Email)) updatedFields.Add("Email");
            if (!string.IsNullOrEmpty(request.FirstName)) updatedFields.Add("FirstName");
            if (!string.IsNullOrEmpty(request.LastName)) updatedFields.Add("LastName");
            if (request.Age.HasValue) updatedFields.Add("Age");
            if (request.Gender.HasValue) updatedFields.Add("Gender");

            return updatedFields.Distinct().ToList();
        }

        private void LogUserAccess(Guid userId, string action)
        {
            
            Console.WriteLine($"User {userId} performed {action} at {DateTime.UtcNow}");
        }
    }
}