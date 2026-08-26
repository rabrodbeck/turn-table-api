namespace TurnTable.Api.Models.Domain;

/// <summary>
/// Represents a server at the restaurant.
/// </summary>
public class Server
{
    /// <summary>
    /// Gets or sets the Id of the server.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the server.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the active status of the server (true: actively working; false: not currently working)
    /// </summary>
    public bool Active { get; set; } = true;

    /// <summary>
    /// Gets or sets the section that the server is assigned to.
    /// </summary>
    public string? Section { get; set; }

}