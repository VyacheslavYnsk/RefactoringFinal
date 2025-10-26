using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System;
using System.Threading.Tasks;

[ApiController]
[Route("purchases")]
public class PurchasesController : ControllerBase
{
    private readonly IPurchaseService _purchaseService;
    private readonly IUserService _userService;

    public PurchasesController(IPurchaseService purchaseService, IUserService userService)
    {
        _purchaseService = purchaseService;
        _userService = userService;
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetPurchases([FromQuery] int page = 0, [FromQuery] int size = 20)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "�������� �����" });

            var clientId = Guid.Parse(userId);
            var (purchases, total) = await _purchaseService.GetByClientAsync(clientId, page, size);

            return Ok(new
            {
                data = purchases,
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
            return StatusCode(500, new { message = "���������� ������ ������� ��� ��������� ������� �������" });
        }
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> GetPurchaseById(Guid id)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "�������� �����" });

            var purchase = await _purchaseService.GetByIdAsync(id);
            if (purchase.ClientId.ToString() != userId)
                return Forbid();

            return Ok(purchase);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch
        {
            return StatusCode(500, new { message = "������ ��� ��������� �������" });
        }
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreatePurchase([FromBody] PurchaseCreate dto)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "�������� �����" });

            var clientId = Guid.Parse(userId);
            var purchase = await _purchaseService.CreateAsync(clientId, dto);

            return CreatedAtAction(nameof(GetPurchaseById), new { id = purchase.Id }, purchase);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch
        {
            return StatusCode(500, new { message = "������ ��� �������� �������" });
        }
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize]
    public async Task<IActionResult> CancelPurchase(Guid id)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "�������� �����" });

            var clientId = Guid.Parse(userId);
            var purchase = await _purchaseService.CancelAsync(id, clientId);

            return Ok(purchase);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch
        {
            return StatusCode(500, new { message = "������ ��� ������ �������" });
        }
    }
}
