using Microsoft.AspNetCore.Mvc;
using Solicitudes.Api.Contracts;
using Solicitudes.Core.Domain;
using Solicitudes.Core.Services;

namespace Solicitudes.Api.Controllers;

[ApiController]
[Route("api/requests")]
public class RequestsController : ControllerBase
{
    private readonly ServiceRequestService _service;

    public RequestsController(ServiceRequestService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateServiceRequestDto dto, CancellationToken cancellationToken)
    {
        var result = await _service.CreateAsync(dto.ExternalReference, dto.CustomerId, dto.ServiceCode, dto.Description, cancellationToken);
        var response = ServiceRequestResponse.From(result.Request);

        if (result.Outcome == CreateOutcome.Created)
        {
            return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
        }

        if (result.Outcome == CreateOutcome.AlreadyExists)
        {
            return Ok(response);
        }

        return Problem(
            title: "La referencia externa ya existe con datos diferentes.",
            statusCode: StatusCodes.Status409Conflict);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] RequestStatus? status, CancellationToken cancellationToken)
    {
        var requests = await _service.GetByStatusAsync(status, cancellationToken);
        return Ok(requests.Select(ServiceRequestResponse.From));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var request = await _service.GetByIdAsync(id, cancellationToken);

        if (request is null)
        {
            return NotFound();
        }

        return Ok(ServiceRequestResponse.From(request));
    }
}