using System.ComponentModel.DataAnnotations;

namespace TurnTable.Api.Models.DTOs;

/// <summary>
/// Payload used to book a new guest reservation.
/// </summary>
public class CreateReservationDto
{
    [Required(ErrorMessage = "Party name is required.")]
    public string PartyName { get; set; } = string.Empty;

    [Range(1, 100, ErrorMessage = "Party size must be at least 1.")]
    public int PartySize { get; set; }

    [Required(ErrorMessage = "Phone number is required.")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Reservation time is required.")]
    public DateTimeOffset ReservationTime { get; set; }
}