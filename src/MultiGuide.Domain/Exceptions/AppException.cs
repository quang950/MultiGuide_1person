namespace MultiGuide.Domain.Exceptions;

public class AppException : Exception
{
    public string Code { get; }
    public int StatusCode { get; }
    public IDictionary<string, string[]>? Errors { get; }

    public AppException(string code, string message, int statusCode = 400, IDictionary<string, string[]>? errors = null)
        : base(message)
    {
        Code = code;
        StatusCode = statusCode;
        Errors = errors;
    }
}

public class NotFoundException : AppException
{
    public NotFoundException(string code, string message)
        : base(code, message, 404)
    {
    }
}

public class ValidationException : AppException
{
    public ValidationException(string code, string message, IDictionary<string, string[]>? errors = null)
        : base(code, message, 422, errors)
    {
    }
}

public class ConflictException : AppException
{
    public ConflictException(string code, string message)
        : base(code, message, 409)
    {
    }
}

public class UnauthorizedException : AppException
{
    public UnauthorizedException(string code, string message)
        : base(code, message, 401)
    {
    }
}

public class ForbiddenException : AppException
{
    public ForbiddenException(string code, string message)
        : base(code, message, 403)
    {
    }
}

public class PaymentRequiredException : AppException
{
    public PaymentRequiredException(string code, string message)
        : base(code, message, 402)
    {
    }
}
