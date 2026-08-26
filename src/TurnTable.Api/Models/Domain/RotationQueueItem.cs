namespace TurnTable.Api.Models.Domain;

/// <summary>
/// Represents a section's position in the active server queue in the database.
/// </summary>
public class RotationQueueItem
{
    /// <summary>
    /// Unique database primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The name of the server section (ex: "Section 1", "Front").
    /// </summary>
    public string Section { get; set; } = string.Empty;    
       
    /// <summary>
    /// The sorting sequence order index of this item in the queue.
    /// </summary>
    public int SortOrder { get; set; }
}