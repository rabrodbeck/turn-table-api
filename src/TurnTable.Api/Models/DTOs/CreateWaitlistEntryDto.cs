namespace TurnTable.Api.Models.DTOs;

/// <summary>
/// Data Transfer Object representing the request payload to add a new party to the waiting list.
/// </summary>
public class CreateWaitlistEntryDto
{
    /// <summary>
    /// Gets or sets the name of the party.
    /// </summary>
    public string PartyName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of guests in the party.
    /// </summary>
    public int PartySize { get; set; }

    /// <summary>
    /// Gets or sets the contact phone number for the party.
    /// </summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the estimated wait time in minutes given to the guests upon arrival.
    /// </summary>
    public int QuotedWaitInMinutes { get; set; }
}