using DemoRest2024Live.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DemoRest2024Live;

public class ServicesController : Controller
{
    private readonly ForumDbContext _dbContext;

    public ServicesController(ForumDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("/api/services2/{serviceId}")]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> GetService(int serviceId)
    {
        var service = await _dbContext.Services.FindAsync(serviceId);
        if (service == null)
            return NotFound();

        return Ok(service.ToDto());
    }
}