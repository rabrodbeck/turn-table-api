using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TurnTable.Api.Hubs;
using TurnTable.Api.Models.DTOs;
using TurnTable.Api.Services;

namespace TurnTable.Api.Controllers;

/// <summary>
/// REST Controller providing the endpoints for managing clocked-in server profiles.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ServersController : ControllerBase
{
    private readonly ITurnTableService _service;
    private readonly IHubContext<TurnTableHub> _hubContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServersController"/> class.
    /// </summary>
    /// <param name="service">The business logic seating service.</param>
    /// <param name="hubContext">The SignalR HubContext.</param>
    public ServersController(ITurnTableService service, IHubContext<TurnTableHub> hubContext)
    {
        _service = service;
        _hubContext = hubContext;
    }

    /// <summary>
    /// Retrieves all active clocked-in servers working the current shift.
    /// </summary>
    /// <returns>A list of active servers.</returns>
    /// <response code="200">Returns the server roster.</response>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ServerDto>>> GetActive()
    {
        var servers = await _service.GetServersAsync();
        return Ok(servers);
    }

    /// <summary>
    /// Clocks in and registers a server for the shift, assigning them a section.
    /// </summary>
    /// <param name="dto">The clocked-in server payload.</param>
    /// <returns>The registered server profile details.</returns>
    /// <response code="201">Returnes the clocked-in server profile.</response>
    /// <response code="400">If the server name/section is duplicate or empty.</response>
    [HttpPost]
    [ProducesResponseType(typeof(ServerDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ServerDto>> ClockIn([FromBody] ClockInServerDto dto)
    {
        var server = await _service.ClockInServerAsync(dto);
        await _hubContext.Clients.All.SendAsync("ReceiveUpdate");
        return StatusCode(StatusCodes.Status201Created, server);
    }

    /// <summary>
    /// Clocks out a server, marking them inactive and reassigning the remaining servers to new layout sections.
    /// </summary>
    /// <param name="dto">The clock-out payload containing server ID and section reassignments.</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">If the clock-out succeeds.</response>
    /// <response code="400">If validation checks fail.</response>
    /// <response code="404">If the server is not found.</response>
    [HttpPost("clock-out")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ClockOut([FromBody] ClockOutServerDto dto)
    {
        await _service.ClockOutServerAsync(dto);
        await _hubContext.Clients.All.SendAsync("ReceiveUpdate");
        return NoContent();
    }
}