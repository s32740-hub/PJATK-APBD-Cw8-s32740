using Microsoft.AspNetCore.Mvc;
using zd8.DTOs;
using zd8.Exceptions;
using zd8.Service;

namespace zd8.Controllers;

[ApiController]
[Route("api/patients")]
public class PatientsController(IPatientsService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetPatients([FromQuery] string? search, CancellationToken cancellationToken)
    {
        var patients = await service.GetPatientsAsync(search, cancellationToken);
        return Ok(patients);
    }

    [HttpPost("{pesel}/bedassignments")]
    public async Task<IActionResult> AssignBed([FromRoute] string pesel, [FromBody] AssignBedRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await service.AssignBedAsync(pesel, request, cancellationToken);
            return Created();
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
    }
}