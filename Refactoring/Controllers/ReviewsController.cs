using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[ApiController]
[Route("films/{filmId:guid}/reviews")]
public class ReviewsController : ControllerBase
{
    private readonly IReviewService _reviewService;

    public ReviewsController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    [HttpGet]
    public async Task<IActionResult> GetReviews(Guid filmId, int page = 0, int size = 20)
    {
        try
        {
            var (reviews, total) = await _reviewService.GetByFilmAsync(filmId, page, size);

            if (!reviews.Any())
            {
                return NotFound(new
                {
                    success = false,
                    message = "Фильм не найден или нет отзывов"
                });
            }

            return Ok(new
            {
                data = reviews,
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
            return StatusCode(500, new
            {
                success = false,
                message = "Внутренняя ошибка сервера при получении отзывов"
            });
        }
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateReview(Guid filmId, [FromBody] ReviewCreate dto)
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

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { success = false, message = "Неверный токен" });
            }

            var review = await _reviewService.CreateAsync(filmId, Guid.Parse(userId), dto);
            return CreatedAtAction(nameof(GetReviewById), "Reviews", new { id = review.Id }, review);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch
        {
            return StatusCode(500, new { success = false, message = "Внутренняя ошибка сервера при создании отзыва" });
        }
    }

    [HttpGet("~/reviews/{id:guid}")]
    public async Task<IActionResult> GetReviewById(Guid id)
    {
        try
        {
            var review = await _reviewService.GetByIdAsync(id);
            if (review == null)
            {
                return NotFound(new { success = false, message = "Отзыв не найден" });
            }

            return Ok(review);
        }
        catch
        {
            return StatusCode(500, new { success = false, message = "Ошибка при получении отзыва" });
        }
    }

    [HttpPut("~/reviews/{id:guid}")]
    [Authorize]
    public async Task<IActionResult> UpdateReview(Guid id, [FromBody] ReviewUpdate dto)
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

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { success = false, message = "Неверный токен" });
            }

            var review = await _reviewService.UpdateAsync(id, Guid.Parse(userId), dto);
            if (review == null)
            {
                return Forbid();
            }

            return Ok(review);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch
        {
            return StatusCode(500, new { success = false, message = "Ошибка при обновлении отзыва" });
        }
    }

    [HttpDelete("~/reviews/{id:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteReview(Guid id)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { success = false, message = "Неверный токен" });
            }

            var deleted = await _reviewService.DeleteAsync(id, Guid.Parse(userId));
            if (!deleted)
            {
                return Forbid();
            }

            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch
        {
            return StatusCode(500, new { success = false, message = "Ошибка при удалении отзыва" });
        }
    }
}
