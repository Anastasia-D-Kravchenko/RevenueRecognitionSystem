using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RevenueRecognitionSystem.Api.Services;

namespace RevenueRecognitionSystem.Api.Controllers;

[ApiController]
[Route("api/revenue")]
[Authorize]
public class RevenueController : ControllerBase
{
    private readonly IRevenueService _service;

    public RevenueController(IRevenueService service) => _service = service;

    [HttpGet("current")]
    public async Task<IActionResult> Current(
        [FromQuery] int? softwareId,
        [FromQuery] string currency = "PLN",
        CancellationToken ct = default)
        => Ok(await _service.GetCurrentAsync(softwareId, currency, ct));

    [HttpGet("predicted")]
    public async Task<IActionResult> Predicted(
        [FromQuery] int? softwareId,
        [FromQuery] string currency = "PLN",
        CancellationToken ct = default)
        => Ok(await _service.GetPredictedAsync(softwareId, currency, ct));
}
