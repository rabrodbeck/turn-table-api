using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TurnTable.Api.Hubs;
using TurnTable.Api.Models.DTOs;
using TurnTable.Api.Services;

namespace TurnTable.Api.Controllers;

/// <summary>
/// REST Controller providing the endpoints for managing guest reservations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ReservationsController : ControllerBase
{
    private readonly ITurnTableService _service;
    private readonly IHubContext<TurnTableHub> _hubContext;

    public ReservationsController(ITurnTableService service, IHubContext<TurnTableHub> hubContext)
    {
        _service = service;
        _hubContext = hubContext;
    }

    /// <summary>
    /// Retrieves all reservations scheduled for today.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ReservationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ReservationDto>>> GetToday()
    {
        var reservations = await _service.GetTodayReservationsAsync();
        return Ok(reservations);
    }

    /// <summary>
    /// Books a new guest reservation (for today or a future date).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ReservationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ReservationDto>> Book([FromBody] CreateReservationDto dto)
    {
        var reservation = await _service.AddReservationAsync(dto);
        await _hubContext.Clients.All.SendAsync("ReceiveUpdate");
        return StatusCode(StatusCodes.Status201Created, reservation);
    }

    /// <summary>
    /// Updates the status of an existing reservation (ex: no_show, canceled).
    /// </summary>
    [HttpPatch("{id}/status")]
    [ProducesResponseType(typeof(ReservationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReservationDto>> UpdateStatus(string id, [FromBody] UpdateReservationStatusDto dto)
    {
        var reservation = await _service.UpdateReservationStatusAsync(id, dto.Status);
        await _hubContext.Clients.All.SendAsync("ReceiveUpdate");
        return Ok(reservation);
    }

    /// <summary>
    /// Converts a reservation into a high-priority waitlist entry.
    /// </summary>
    [HttpPost("{id}/waitlist")]
    [ProducesResponseType(typeof(WaitlistEntryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WaitlistEntryDto>> SendToWaitlist(string id)
    {
        var waitlistEntry = await _service.MoveReservationToWaitlistAsync(id);
        await _hubContext.Clients.All.SendAsync("ReceiveUpdate");
        return Ok(waitlistEntry);
    }
}