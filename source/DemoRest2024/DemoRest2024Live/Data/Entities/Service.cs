namespace DemoRest2024Live.Data.Entities;

public class Service
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required decimal Price { get; set; }
    public bool IsDeleted { get; set; } = false;
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();


    public ServiceDto ToDto()
    {
        return new ServiceDto(Id, Name, Price, IsDeleted);
    }
}



