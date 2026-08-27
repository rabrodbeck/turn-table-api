namespace TurnTable.Api.Models.Domain;

/// <summary>
/// Represents an entry on the waiting list.
/// </summary>
public class WaitlistEntry
{
    /// <summary>
    /// Gets or sets the unique identifier for the waitlist entry.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the party on the waitlist.
    /// </summary>
    public string PartyName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of guests in the party.
    /// </summary>
    public int PartySize { get; set; }

    /// <summary>
    /// Gets or set the phone number to contact for the party.
    /// </summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp fo when the party checked-in.
    /// </summary>
    public DateTimeOffset CheckedInAt { get; set; }

    /// <summary>
    /// Gets or sets the estimated wait time (in minutes) the party was told upon check-in.
    /// </summary>
    public int QuotedWaitInMinutes { get; set; } = 0;

    /// <summary>
    /// Gets or sets the status of the waitlist entry (ex: "waiting", "contacted", "seated", "canceled", "no_show").
    /// </summary>
    public string Status { get; set; } = "waiting";

    /// <summary>
    /// Indicates if this waitlist entry was originally a reservation arrival (to sort at the top of the queue).
    /// </summary>
    public bool IsReservationArrival { get; set; } = false;
}