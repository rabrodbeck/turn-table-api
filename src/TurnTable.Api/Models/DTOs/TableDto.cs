namespace TurnTable.Api.Models.DTOs;

/// <summary>
/// Data transfer object representing a dining table's state returned to the frontend.
/// Resolves server names and elapsed dining timers dynamically.
/// </summary>
public class TableDto
{
    /// <summary>
    /// Gets or sets the unique identifier for the table (table number).
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type/shape of the table (ex: "rectangle", "square", "circle").
    /// </summary>
    public string TypeOrShape { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the maximum seating capacity of the table.
    /// </summary>
    public int MaxSeats { get; set; }

    /// <summary>
    /// Gets or sets the server section the table is currently grouped in.
    /// </summary>
    public string CurrentServerSection { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current status of the table (ex: "available", "seated", "not usable").
    /// </summary>
    public string Status { get; set; } = "available";

    /// <summary>
    /// Gets or sets the name of the party currently seated at the table.
    /// </summary>
    public string? PartyName { get; set; }

    /// <summary>
    /// gets or sets the number of guests currently seated at the table.
    /// </summary>
    public int? PartySize { get; set; }

    /// <summary>
    /// Gets or sets the date and time of when the current party was seated.
    /// </summary>
    public DateTimeOffset? SeatedAt { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the server currently assigned to this table.
    /// </summary>
    public string? ServerId { get; set; }

    /// <summary>
    /// Gets or sets the resolved display name of the server currently assigned to this table.
    /// </summary>
    public string? ServerName { get; set; }

    /// <summary>
    /// Get the number of minutes a party has been seated.
    /// Calculated dynamically relative to current UTC time if SeatedAt has a vlue.
    /// </summary>
    public int ElapsedMinutes => SeatedAt.HasValue
        ? (int)(DateTimeOffset.UtcNow - SeatedAt.Value).TotalMinutes : 0;
}