using System.ComponentModel.DataAnnotations;

namespace TurnTable.Api.Models.DTOs;

/// <summary>
/// Data Transfer Object representing the request to clock out a server and reassign the remaining servers' sections.
/// </summary>
public class ClockOutServerDto
{
    /// <summary>
    /// The unique identifier fo the server being clocked out.
    /// </summary>
    [Required]
    public required string ServerId { get; set; }

    public List<ServerReassignmentDto> Reassignments { get; set; } = new();
}

