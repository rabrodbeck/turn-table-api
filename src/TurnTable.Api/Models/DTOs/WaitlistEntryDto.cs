namespace TurnTable.Api.Models.DTOs;

/// <summary>
/// Data Transfer Object representing a waitlist entry returned to the frontend.
/// </summary>
public class WaitlistEntryDto
{
    /// <summary>
    /// Gets or sets the unique identifier for the waitlist entry.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the party.
    /// </summary>
    public string PartyName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of guests in the party.
    /// </summary>
    public int PartySize { get; set; }

    /// <summary>
    /// Gets or sets the phone number to contact the party.
    /// </summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time when the party checked in.
    /// </summary>
    public DateTimeOffset CheckedInAt { get; set; }

    /// <summary>
    /// Gets or sets the estimated wait time in minutes given to the guests.
    /// </summary>
    public int QuotedWaitInMinutes { get; set; }

    /// <summary>
    /// Gets or sets the status of the waitlist entry (ex: "waiting", "contacted", "seated", "canceled", "no_show").
    /// </summary>
    public string Status { get; set; } = "waiting";

    /// <summary>
    /// Gets or sets whether this waitlist entry is a high-priority reservation arrival.
    /// </summary>
    public bool IsReservationArrival { get; set; }
}