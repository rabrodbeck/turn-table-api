namespace TurnTable.Api.Exceptions;

/// <summary>
/// Exception thrown when a business logic rule or validation fails (ex: seating over capacity).
/// </summary>
public class BusinessValidationException : Exception
{

    /// <summary>
    /// Initializes a new instance of the <see cref="BusinessValidationException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">the message describing which business rules was violated</param>
    /// <returns></returns>
    public BusinessValidationException(string message) : base(message)
    {
        
    }
    
}