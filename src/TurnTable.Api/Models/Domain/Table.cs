namespace TurnTable.Api.Models.Domain;

/// <summary>
/// Represents a dining table in a restaurant tracking its capacity and current server section.
/// </summary>
public class Table
{
    /// <summary>
    /// Gets or sets the unique identifier for the table (essentially the table number).
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type/shape of the table (ex: "rectangle", "square", "circle")
    /// </summary>
    public string TypeOrShape { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the maximum number of people that can be seated at the table.
    /// </summary>
    public int MaxSeats { get; set; }

    /// <summary>
    /// Server section the table is currently in, can change depending on number of servers on shift.
    /// </summary>
    public string CurrentServerSection { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current status of the table (ex: "available", "seated", "not usable").
    /// </summary>
    public string Status { get; set; } = "available";

    /// <summary>
    /// Gets or sets the name of the party currently seated at the table if provided.
    /// Returns null or empty if the table is unoccupied.
    /// </summary>
    public string? PartyName { get; set; } 

    /// <summary>
    /// Gets or sets the number of guests currently seated at the table.
    /// </summary>
    public int? PartySize { get; set; }

    /// <summary>
    /// Gets or sets the id of the server for the table.
    /// </summary>
    public string? ServerId { get; set; }

    /// <summary>
    /// Gets or sets the date and time the current party was seated at the table.
    /// Used to calculate live dining time elapsed.
    /// </summary>
    public DateTimeOffset? SeatedAt { get; set; }
}