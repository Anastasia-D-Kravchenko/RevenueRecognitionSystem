using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RevenueRecognitionSystem.Api.DTOs;
using RevenueRecognitionSystem.Api.Exceptions;
using RevenueRecognitionSystem.Api.Services;

namespace RevenueRecognitionSystem.Api.Controllers;

[ApiController]
[Route("api/subscriptions")]
[Authorize]
public class SubscriptionsController : ControllerBase
{
    private readonly ISubscriptionService _service;

    public SubscriptionsController(ISubscriptionService service) => _service = service;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSubscriptionDto dto)
    {
        try
        {
            var result = await _service.CreateAsync(dto);
            return Created($"/api/subscriptions/{result.SubscriptionId}", result);
        }
        catch (NotFoundException ex) { return NotFound(ex.Message); }
        catch (ConflictException ex) { return Conflict(ex.Message); }
        catch (BadRequestException ex) { return BadRequest(ex.Message); }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        try { return Ok(await _service.GetAsync(id)); }
        catch (NotFoundException ex) { return NotFound(ex.Message); }
    }

    [HttpPost("{id:int}/payments")]
    public async Task<IActionResult> PayRenewal(int id, [FromBody] PaySubscriptionDto dto)
    {
        try
        {
            return Ok(await _service.PayRenewalAsync(id, dto));
        }
        catch (NotFoundException ex) { return NotFound(ex.Message); }
        catch (ConflictException ex) { return Conflict(ex.Message); }
        catch (BadRequestException ex) { return BadRequest(ex.Message); }
    }
}
