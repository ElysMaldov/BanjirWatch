namespace BanjirWatch.Exceptions;

/// <summary>
/// Thrown when a requested resource is not found
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

/// <summary>
/// Thrown when the user is not authorized to perform an action
/// </summary>
public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message) : base(message) { }
}
