namespace TurnTable.Api.Models.DTOs;

/// <summary>
/// Data Transfer Object representing the request payload to clock in and register a server for the shift.
/// </summary>
public class ClockInServerDto
{
    /// <summary>
    /// Gets or sets the name of the server being clocked in.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the restaurant section assigned to the server.
    /// </summary>
    public string Section { get; set; } = string.Empty;
}