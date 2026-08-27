using System.ComponentModel.DataAnnotations;

namespace TurnTable.Api.Models.DTOs;

/// <summary>
/// Payload used to update a reservation status (ex: canceled, no_show).
/// </summary>
public class UpdateReservationStatusDto
{
    [Required(ErrorMessage = "Status is required.")]
    public string Status { get; set; } = string.Empty;
}