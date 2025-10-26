using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Model.Register;



namespace Refactoring.Controllers
{
    [ApiController]
    [Route("api/[controller]")]

    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ITokenRevocationService _tokenRevocationService;


        public AuthController(IAuthService authService, ITokenRevocationService tokenRevocationService)
        {
            _authService = authService;
            _tokenRevocationService = tokenRevocationService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegisterRequest request)
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

                var result = await _authService.RegisterAsync(request);

                if (result.Success)
                {
                    return StatusCode(201, new AuthResponse
                    {
                        Success = true,
                        AccesToken = result.AccesToken,
                        Message = "Пользователь успешно зарегистрирован"
                    });
                }

                return BadRequest(new { success = false, message = result.Message });
            }

            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Внутренняя ошибка сервера" });
            }
        }


        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginRequest request)
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

                var result = await _authService.LoginAsync(request);

                if (result.Success)
                {
                    if (result.Success)
                {
                    return StatusCode(201, new AuthResponse
                    {
                        Success = true,
                        AccesToken = result.AccesToken,
                        Message = "Пользователь успешно зарегистрирован"
                    });
                }
                }

                return BadRequest(new { success = false, message = result.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Внутренняя ошибка сервера" });
            }
        }

        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> LogoutUser()
        {
            try
            {
                var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                
                if (string.IsNullOrEmpty(token))
                {
                    return BadRequest(new { success = false, message = "Token is required" });
                }
                
                var result = await _tokenRevocationService.RevokeTokenAsync(token);
                
                if (!result)
                {
                    return BadRequest(new { success = false, message = "Token has already been revoked" });
                }

                return Ok(new { success = true, message = "Logout successful" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }


    }
}