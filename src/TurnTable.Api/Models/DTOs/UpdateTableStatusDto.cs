namespace TurnTable.Api.Models.DTOs;

/// <summary>
/// Data Transfer Object representing the request payload to update a table's operational status.
/// </summary>
public class UpdateTableStatusDto
{
    /// <summary>
    /// Gets or sets the current status of the table (ex: "available", "seated", "not usable").
    /// </summary>
    public string Status { get; set; } = string.Empty;
    
}