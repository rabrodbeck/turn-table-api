namespace TurnTable.Api.Models.DTOs;

/// <summary>
/// Data Transfer Object representing the current server rotation state.
/// </summary>
public class RotationDto
{
    /// <summary>
    /// Gets or sets the server that is currently next up in the rotation queue.
    /// Returns null if no active servers are clocked in.
    /// </summary>
    public ServerDto? NextServerUp { get; set; }

    /// <summary>
    /// Gets or sets the ordered queue of servers in the rotation.
    /// </summary>
    public List<ServerDto> Queue { get; set; } = new();
}