using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TurnTable.Api.Models.DTOs;
using TurnTable.Api.Services;
using Microsoft.AspNetCore.SignalR;
using TurnTable.Api.Hubs;

namespace TurnTable.Api.Controllers;

/// <summary>
/// REST Controller providing endpoints for managing restaurant dining tables.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class TablesController : ControllerBase
{
    private readonly ITurnTableService _service;
    private readonly IHubContext<TurnTableHub> _hubContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="TablesController"/> class.
    /// </summary>
    /// <param name="service">The business logic seating service.</param>
    /// <param name="hubContext">The hub context for SignalR.</param>
    public TablesController(ITurnTableService service, IHubContext<TurnTableHub> hubContext)
    {
        _service = service;
        _hubContext = hubContext;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TableDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<TableDto>>> GetAll()
    {
        var tables = await _service.GetTablesAsync();
        return Ok(tables);
    }

    /// <summary>
    /// Seats a party at a specific table, assigning a server.
    /// </summary>
    /// <param name="tableId">The unique identifier of the table (table number).</param>
    /// <param name="dto">The seating payload containing party size, server, and optional waitlist ID.</param>
    /// <returns>The updated table details.</returns>
    /// <response code="200">If seating was successful.</response>
    /// <response code="400">If the table is already occupied, the server is inactive, or the party size exceeds capacity.</response>
    /// <response code="404">If the table or server does not exist.</response>
    [HttpPost("{tableId}/seat")]
    [ProducesResponseType(typeof(TableDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TableDto>> Seat(string tableId, [FromBody] SeatTableDto dto)
    {
        var table = await _service.SeatTableAsync(tableId, dto);
        await _hubContext.Clients.All.SendAsync("ReceiveUpdate");
        return Ok(table);
    }

    /// <summary>
    /// Manually updates a table's status.
    /// </summary>
    /// <param name="tableId">The unique identifier of the table (table number).</param>
    /// <param name="dto">The payload containing the new status value.</param>
    /// <returns>The updated table details.</returns>
    /// <response code="200">If the status update was successful.</response>
    /// <response code="404">If the table does not exist.</response>
    [HttpPatch("{tableId}/status")]
    [ProducesResponseType(typeof(TableDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TableDto>> UpdateStatus(string tableId, [FromBody] UpdateTableStatusDto dto)
    {
        var table = await _service.UpdateTableStatusAsync(tableId, dto);
        await _hubContext.Clients.All.SendAsync("ReceiveUpdate");
        return Ok(table);
    }

    /// <summary>
    /// Clears and cleans a table, resetting it to Available and removing seated party metadata.
    /// </summary>
    /// <param name="tableId">The unique identifier of the table to clear (table number).</param>
    /// <returns>The reset table details.</returns>
    /// <response code="200">If the table was successfully cleared.</response>
    /// <response code="404">If the table does not exist.</response>
    [HttpPost("{tableId}/clear")]
    [ProducesResponseType(typeof(TableDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TableDto>> Clear(string tableId)
    {
        var table = await _service.ClearTableAsync(tableId);
        await _hubContext.Clients.All.SendAsync("ReceiveUpdate");
        return Ok(table);
    }
    
}