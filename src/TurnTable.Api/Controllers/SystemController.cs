using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TurnTable.Api.Hubs;
using TurnTable.Api.Services;

namespace TurnTable.Api.Controllers;

/// <summary>
/// API Controller for handling global system-level operations (ex: shift resets).
/// </summary>
[ApiController]
[Route("api/system")]
public class SystemController : ControllerBase
{
    private readonly ITurnTableService _service;
    private readonly IHubContext<TurnTableHub> _hubContext;

    public SystemController(ITurnTableService service, IHubContext<TurnTableHub> hubContext)
    {
        _service = service;
        _hubContext = hubContext;
    }

    /// <summary>
    /// Resets the restaurant system for the next shift: clears all tables, clocks out all servers, and archives active waitlists.
    /// </summary>
    /// <returns>HTTP 200 OK message on successful reset.</returns>
    [HttpPost("reset")]
    public async Task<IActionResult> ResetShift()
    {
        await _service.ResetShiftAsync();
        await _hubContext.Clients.All.SendAsync("ReceiveUpdate");
        return Ok(new { message = "Shift successfully ended and system reset." });
    }
}