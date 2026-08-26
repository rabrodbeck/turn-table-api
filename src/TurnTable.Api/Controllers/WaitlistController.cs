using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TurnTable.Api.Models.DTOs;
using TurnTable.Api.Services;
using Microsoft.AspNetCore.SignalR;
using TurnTable.Api.Hubs;

namespace TurnTable.Api.Controllers;

/// <summary>
/// REST Controller providing endpoints for managing waiting guest lists.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class WaitlistController : ControllerBase
{
    private readonly ITurnTableService _service;
    private readonly IHubContext<TurnTableHub> _hubContext;    
    
    /// <summary>
    /// Initializes a new instance of the <see cref="WaitlistController"/> class.
    /// </summary>
    /// <param name="service">The business logic seating service.</param>
    /// <param name="hubContext">The hub context for SignalR.</param>
    public WaitlistController(ITurnTableService service, IHubContext<TurnTableHub> hubContext)
    {
        _service = service;
        _hubContext = hubContext;
    }

    /// <summary>
    /// Retrieves active waiting parties sorted chronologically by check-in time.
    /// </summary>
    /// <returns>A list of active waitlist entries.</returns>
    /// <response code="200">Returns the waitlist.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<WaitlistEntryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<WaitlistEntryDto>>> GetActive()
    {
        var waitlist = await _service.GetWaitlistAsync();
        return Ok(waitlist);
    }

        /// <summary>
    /// Adds a new party to the waiting list.
    /// </summary>
    /// <param name="dto">The waitlist check-in payload.</param>
    /// <returns>The newly created waitlist entry.</returns>
    /// <response code="201">Returns the newly checked-in entry.</response>
    /// <response code="400">If validation fails or party size is invalid.</response>
    [HttpPost]
    [ProducesResponseType(typeof(WaitlistEntryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<WaitlistEntryDto>> Create([FromBody] CreateWaitlistEntryDto dto)
    {
        var entry = await _service.AddWaitlistEntryAsync(dto);
        await _hubContext.Clients.All.SendAsync("ReceiveUpdate");
        return StatusCode(StatusCodes.Status201Created, entry);
    }

    /// <summary>
    /// Updates the status of an existing waitlist party (e.g. Texted, Seated, Canceled, No Show).
    /// </summary>
    /// <param name="waitlistId">The unique identifier of the waitlist entry.</param>
    /// <param name="dto">The payload containing the new status value.</param>
    /// <returns>The updated waitlist entry details.</returns>
    /// <response code="200">If the status update was successful.</response>
    /// <response code="404">If the waitlist entry does not exist.</response>
    [HttpPatch("{waitlistId}")]
    [ProducesResponseType(typeof(WaitlistEntryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WaitlistEntryDto>> UpdateStatus(string waitlistId, [FromBody] UpdateWaitlistStatusDto dto)
    {
        var entry = await _service.UpdateWaitlistStatusAsync(waitlistId, dto);
        await _hubContext.Clients.All.SendAsync("ReceiveUpdate");
        return Ok(entry);
    }
}