using Microsoft.AspNetCore.Mvc;

namespace MultiGuide.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json", "application/problem+json")]
public abstract class BaseApiController : ControllerBase
{
}
