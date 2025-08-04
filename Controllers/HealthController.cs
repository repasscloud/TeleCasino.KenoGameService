using Microsoft.AspNetCore.Mvc;
namespace TeleCasino.KenoGameService.Controllers;
[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok("Healthy");
    }
}