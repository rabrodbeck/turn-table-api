using System.ComponentModel.DataAnnotations;

namespace TurnTable.Api.Models.Domain;

/// <summary>
/// Domain model representing a guest reservation.
/// </summary>
public class Reservation
{
    [Key]
    public string Id { get; set; } = string.Empty;

    [Required]
    public string PartyName { get; set; } = string.Empty;

    public int PartySize { get; set; }

    [Required]
    public string PhoneNumber { get; set; } = string.Empty;

    public DateTimeOffset ReservationTime { get; set; }

    [Required]
    public string Status { get; set; } = "booked"; // "booked", "seated", "no_show", "canceled"
    
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

}