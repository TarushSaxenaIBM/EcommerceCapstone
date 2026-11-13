namespace ECommerce.Application.Exceptions;

public class AppException : Exception
{
    public int StatusCode { get; }

    public AppException(string message, int statusCode) : base(message)
    {
        StatusCode = statusCode;
    }
}

public class BadRequestException(string message) : AppException(message, 400) { }

public class UnauthorizedException(string message) : AppException(message, 401) { }

public class NotFoundException(string message) : AppException(message, 404) { }

public class ConflictException(string message) : AppException(message, 409) { }

public class ServiceUnavailableException(string message) : AppException(message, 503) { }
