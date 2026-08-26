namespace TurnTable.Api.Exceptions;

/// <summary>
/// Exception thrown when a requested resource (ex: table, server, waitlist entry) cannot be found.
/// Translated into an HTTP 404 Not Found response.
/// </summary>
public class NotFoundException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotFoundException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message describing which resource could not be found.</param>
    public NotFoundException(string message) : base(message)
    {
        
    }
}