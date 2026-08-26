namespace TurnTable.Api.Models.DTOs;

/// <summary>
/// Data Transfer Object representing the request paylod to update a waitlist party's status.
/// </summary>
public class UpdateWaitlistStatusDto
{
    /// <summary>
    /// Gets or sets the status of the waitlist entry (ex: "waiting", "contacted", "seated", "canceled", "no_show")
    /// </summary>
    public string Status { get; set; } = string.Empty;
}