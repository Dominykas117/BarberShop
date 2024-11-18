using DemoRest2024Live.Auth.Model;
using System.ComponentModel.DataAnnotations;

namespace DemoRest2024Live.Data.Entities;

public class Service
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required decimal Price { get; set; }
    public bool IsDeleted { get; set; } = false;
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    [Required]
    public required string BarberShopClientID { get; set; }
    public BarberShopClient BarberShopClient { get; set; }

    public ServiceDto ToDto()
    {
        return new ServiceDto(Id, Name, Price, IsDeleted);
    }
}



