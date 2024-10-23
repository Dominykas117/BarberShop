using DemoRest2024Live.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DemoRest2024Live;

public class ReservationsController : Controller
{
    private readonly ForumDbContext _dbContext;

    public ReservationsController(ForumDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("/api/services2/{serviceId}/reservations2/{reservationId}")]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> GetReservation(int reservationId)
    {
        var reservation = await _dbContext.Reservations.FindAsync(reservationId);
        if (reservation == null)
            return NotFound();

        return Ok(reservation.ToDto());
    }
}