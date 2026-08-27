namespace TurnTable.Api.Models.DTOs;

/// <summary>
/// Data Transfer Object representing a reservation response.
/// </summary>
public class ReservationDto
{
    public string Id { get; set; } = string.Empty;
    public string PartyName { get; set; } = string.Empty;
    public int PartySize { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public DateTimeOffset ReservationTime { get; set; }
    public string Status { get; set; } = string.Empty;
}