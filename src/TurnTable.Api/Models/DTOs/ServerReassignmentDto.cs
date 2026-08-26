using System.ComponentModel.DataAnnotations;

namespace TurnTable.Api.Models.DTOs;

/// <summary>
/// Helper DTO representing a section reassignment for a staying active server.
/// </summary>
public class ServerReassignmentDto
{
    /// <summary>
    /// The unique identifier of the server being reassigned.
    /// </summary>
    [Required]
    public required string ServerId { get; set; }
    
    /// <summary>
    /// The new section assignment for this server.
    /// </summary>
    [Required]
    public required string NewSection { get; set; }
}