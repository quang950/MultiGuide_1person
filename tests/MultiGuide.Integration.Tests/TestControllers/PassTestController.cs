using Microsoft.AspNetCore.Mvc;
using MultiGuide.Api.Auth;

namespace MultiGuide.Integration.Tests.TestControllers;

[ApiController]
[Route("test/pass-protected")]
[RequireAccessPass]
public class PassTestController : ControllerBase
{
    [HttpGet]
    public IActionResult GetProtected()
    {
        var passContext = HttpContext.GetPassContext();
        return Ok(new
        {
            pass_id = passContext?.PassId,
            area_id = passContext?.AreaId
        });
    }
}
