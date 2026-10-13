using Microsoft.AspNetCore.Mvc;
using MultiGuide.Domain.Constants;
using MultiGuide.Domain.Exceptions;

namespace MultiGuide.Integration.Tests.TestControllers;

[ApiController]
[Route("api/v1/test-errors")]
public class ErrorTestController : ControllerBase
{
    [HttpGet("not-found")]
    public IActionResult ThrowNotFound() =>
        throw new NotFoundException(ErrorCodes.AreaNotFound, "Tour area not found");

    [HttpGet("validation")]
    public IActionResult ThrowValidation() =>
        throw new ValidationException(ErrorCodes.ValidationError, "Invalid coordinates",
            new Dictionary<string, string[]> { { "lat", new[] { "Lat must be between -90 and 90" } } });

    [HttpGet("conflict")]
    public IActionResult ThrowConflict() =>
        throw new ConflictException(ErrorCodes.DuplicateRequest, "Duplicate idempotency key");

    [HttpGet("unauthorized")]
    public IActionResult ThrowUnauthorized() =>
        throw new UnauthorizedException(ErrorCodes.PassMissing, "AccessPass token is missing");

    [HttpGet("forbidden")]
    public IActionResult ThrowForbidden() =>
        throw new ForbiddenException(ErrorCodes.PassExpired, "AccessPass has expired");

    [HttpGet("unhandled")]
    public IActionResult ThrowUnhandled() =>
        throw new InvalidOperationException("Something unexpected crashed");
}
