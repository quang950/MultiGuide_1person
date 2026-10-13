using Microsoft.AspNetCore.Mvc;
using MultiGuide.Api.Controllers;

namespace MultiGuide.Integration.Tests.TestControllers;

/// <summary>
/// Controller kiểm thử cho các endpoint admin kế thừa AdminBaseController.
/// </summary>
public class AdminTestController : AdminBaseController
{
    [HttpGet("ping")]
    public IActionResult Ping() => Ok(new { message = "pong", role = "admin" });
}
