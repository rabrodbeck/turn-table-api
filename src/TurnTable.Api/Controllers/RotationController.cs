using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TurnTable.Api.Hubs;
using TurnTable.Api.Models.DTOs;
using TurnTable.Api.Services;

namespace TurnTable.Api.Controllers;

/// <summary>
/// REST Controller providing endpoints for managing the server seating rotation.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class RotationController : ControllerBase
{
    private readonly ITurnTableService _service;
    private readonly IHubContext<TurnTableHub> _hubContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="RotationController"/> class.
    /// </summary>
    /// <param name="service">The business logic seating service.</param>
    /// <param name="hubContext">The SignalR HubContext.</param>
    public RotationController(ITurnTableService service, IHubContext<TurnTableHub> hubContext)
    {
        _service = service;
        _hubContext = hubContext;
    }

    /// <summary>
    /// Retrieves the current order of the server rotation queue and who is next up to seat.
    /// </summary>
    /// <returns>The current rotation queue state.</returns>
    /// <response code="200">Returns the rotation queue state.</response>
    [HttpGet]
    public async Task<ActionResult<RotationDto>> GetRotation()
    {
        var rotation = await _service.GetRotationAsync();
        return Ok(rotation);
    }

    /// <summary>
    /// Skips the next-up section in the queue, rotating them to the back.
    /// </summary>
    /// <returns>No content on success.</returns>
    /// <response code="200">If the server skip was successful.</response>
    [HttpPost("skip")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Skip()
    {
        await _service.SkipRotationAsync();
        await _hubContext.Clients.All.SendAsync("ReceiveUpdate");
        return Ok(new { message = "Queue skipped successfully. "});
    }
    
}