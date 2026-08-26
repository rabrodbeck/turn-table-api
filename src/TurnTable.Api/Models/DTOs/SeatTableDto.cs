namespace TurnTable.Api.Models.DTOs;

/// <summary>
/// Data Transfer Object representing the request payload to seat a party at a table.
/// </summary>
public class SeatTableDto
{
    /// <summary>
    /// Gets or sets the size of the party being seated.
    /// </summary>
    public int PartySize { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the server being assigned to the table.
    /// </summary>
    public string ServerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the unique identifier of the waitlist entry being seated.
    /// This is null if the guests are a direct walk-in.
    /// </summary>
    public string? WaitlistId { get; set; }
    
}