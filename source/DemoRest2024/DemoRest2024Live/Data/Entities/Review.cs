using System.Xml.Linq;

namespace DemoRest2024Live.Data.Entities;

public class Review
{
    public int Id { get; set; }
    public required string Content { get; set; }
    public required int Rating { get; set; }
    public bool IsDeleted { get; set; } = false;
    public required int ReservationId { get; set; }


    public Reservation Reservation { get; set; }

    public ReviewDto ToDto()
    {
        return new ReviewDto(Id, Content, Rating, IsDeleted, ReservationId);
    }
}