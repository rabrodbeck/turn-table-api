namespace TurnTable.Api.Models.DTOs;

/// <summary>
/// Data Transfer Object representing a server returned to the frontend.
/// </summary>
public class ServerDto
{
    /// <summary>
    /// Gets or sets the unique identifier of the server.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the server.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the server is currently clocked in.
    /// </summary>
    public bool Active { get; set; }

    /// <summary>
    /// Gets or sets the section currently assigned to the server.
    /// </summary>
    public string? Section { get; set; }
}