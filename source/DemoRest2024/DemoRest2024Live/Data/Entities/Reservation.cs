﻿using DemoRest2024Live.Auth.Model;
using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace DemoRest2024Live.Data.Entities;

public class Reservation
{
    public int Id { get; set; }
    public required DateTimeOffset Date { get; set; }
    public ReservationStatus Status { get; set; } = ReservationStatus.Confirmed;
    public bool IsDeleted { get; set; } = false;
    public required int ServiceId { get; set; }
    public ICollection<Review> Reviews { get; set; } = new List<Review>();

    public Service Service { get; set; }
    [Required]
    public required string BarberShopClientID { get; set; }
    public BarberShopClient BarberShopClient { get; set; }
    public ReservationDto ToDto()
    {
        return new ReservationDto(Id, Date, Status, IsDeleted, ServiceId);
    }

}

public enum ReservationStatus
{
    Confirmed,
    Canceled
}
