using DemoRest2024Live.Auth.Model;
using DemoRest2024Live.Data.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DemoRest2024Live.Data;

public class ForumDbContext : IdentityDbContext<BarberShopClient>
{
    private readonly IConfiguration _configuration;
    public DbSet<Service> Services { get; set; }
    public DbSet<Reservation> Reservations { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<Session> Sessions { get; set; }


    public ForumDbContext(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(_configuration.GetConnectionString(("PostgreSQL")));
    }
}