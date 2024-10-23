using DemoRest2024Live.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DemoRest2024Live;

public class ReviewsController : Controller
{
    private readonly ForumDbContext _dbContext;

    public ReviewsController(ForumDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("/api/services2/{serviceId}/reservations2/{reservationId}/reviews2/{reviewId}")]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> GetReview(int reviewId)
    {
        var review = await _dbContext.Reviews.FindAsync(reviewId);
        if (review == null)
            return NotFound();

        return Ok(review.ToDto());
    }
}