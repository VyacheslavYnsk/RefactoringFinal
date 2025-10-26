using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

[ApiController]
[Route("payments")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly IUserService _userService;

    public PaymentsController(IPaymentService paymentService, IUserService userService)
    {
        _paymentService = paymentService;
        _userService = userService;
    }

    [HttpPost("process")]
    [Authorize]
    public async Task<IActionResult> ProcessPayment([FromBody] PaymentRequest dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    message = "Неверные данные",
                    errors = ModelState.Values.SelectMany(v => v.Errors)
                });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Неверный токен" });

            var result = await _paymentService.ProcessAsync(Guid.Parse(userId), dto);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch
        {
            return StatusCode(500, new { message = "Внутренняя ошибка сервера при обработке платежа" });
        }
    }

    [HttpGet("{id:guid}/status")]
    [Authorize]
    public async Task<IActionResult> GetPaymentStatus(Guid id)
    {
        try
        {
            var payment = await _paymentService.GetStatusAsync(id);
            return Ok(payment);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch
        {
            return StatusCode(500, new { message = "Внутренняя ошибка сервера при получении статуса платежа" });
        }
    }
}
