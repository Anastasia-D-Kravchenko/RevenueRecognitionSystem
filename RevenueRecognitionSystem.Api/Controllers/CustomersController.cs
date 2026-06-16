using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RevenueRecognitionSystem.Api.DTOs;
using RevenueRecognitionSystem.Api.Exceptions;
using RevenueRecognitionSystem.Api.Services;

namespace RevenueRecognitionSystem.Api.Controllers;

[ApiController]
[Route("api/customers")]
[Authorize]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _service;

    public CustomersController(ICustomerService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _service.ListAsync());

    [HttpPost("individual")]
    public async Task<IActionResult> AddIndividual([FromBody] CreateIndividualCustomerDto dto)
    {
        try
        {
            var id = await _service.AddIndividualAsync(dto);
            return Created($"/api/customers/{id}", new { id });
        }
        catch (ConflictException ex) { return Conflict(ex.Message); }
    }

    [HttpPost("company")]
    public async Task<IActionResult> AddCompany([FromBody] CreateCompanyCustomerDto dto)
    {
        try
        {
            var id = await _service.AddCompanyAsync(dto);
            return Created($"/api/customers/{id}", new { id });
        }
        catch (ConflictException ex) { return Conflict(ex.Message); }
    }

    [HttpPut("individual/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateIndividual(int id, [FromBody] UpdateIndividualCustomerDto dto)
    {
        try
        {
            await _service.UpdateIndividualAsync(id, dto);
            return NoContent();
        }
        catch (NotFoundException ex) { return NotFound(ex.Message); }
        catch (ConflictException ex) { return Conflict(ex.Message); }
    }

    [HttpPut("company/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateCompany(int id, [FromBody] UpdateCompanyCustomerDto dto)
    {
        try
        {
            await _service.UpdateCompanyAsync(id, dto);
            return NoContent();
        }
        catch (NotFoundException ex) { return NotFound(ex.Message); }
    }

    [HttpDelete("individual/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SoftDeleteIndividual(int id)
    {
        try
        {
            await _service.SoftDeleteIndividualAsync(id);
            return NoContent();
        }
        catch (NotFoundException ex) { return NotFound(ex.Message); }
    }
}
