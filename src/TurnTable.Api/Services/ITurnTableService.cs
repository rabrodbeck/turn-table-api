using TurnTable.Api.Models.DTOs;

namespace TurnTable.Api.Services;

/// <summary>
/// Service contract defining all host stand management and seating rotation operations.
/// </summary>
public interface ITurnTableService
{
    #region Dining Tables

    /// <summary>
    /// Retrieves the list of all dining tables and their active seating states.
    /// </summary>
    /// <returns>A collection of table DTOs.</returns>
    Task<IEnumerable<TableDto>> GetTablesAsync();

    /// <summary>
    /// Seats a party at a table, assigning a server, updating statuses, and moving the table's section to the back of the rotation queue.
    /// </summary>
    /// <param name="tableId">The unique identifier of the table to seat.</param>
    /// <param name="dto">The seating payload containing party size, server, and optional waitlist ID.</param>
    /// <returns>The updated table DTO.</returns>
    /// <exception cref="Exceptions.NotFoundException">Thrown if the table or server does not exist.</exception>
    /// <exception cref="Exceptions.BusinessValidationException">Thrown if the table is already occupied or guest size exceeds capacity.</exception> 
    Task<TableDto> SeatTableAsync(string tableId, SeatTableDto dto);

    /// <summary>
    /// Manually updates a table's status (ex: from Seated to Available).
    /// </summary>
    /// <param name="tableId">The unique identifier of the table.</param>
    /// <param name="dto">The payload containing the new status.</param>
    /// <returns>The updated table DTO.</returns>
    /// <exception cref="Exceptions.NotFoundException">Thrown if the table does not exist.</exception>
    Task<TableDto> UpdateTableStatusAsync(string tableId, UpdateTableStatusDto dto);
    
    /// <summary>
    /// Clears and cleans a table, resetting it to Available and clearing all seated party and server metadata.
    /// </summary>
    /// <param name="tableId"></param>
    /// <returns>The reset table DTO.</returns>
    /// <exception cref="Exceptions.NotFoundException">Thrown if the table does not exist.</exception>
    Task<TableDto> ClearTableAsync(string tableId);

    #endregion

    #region Waitlist

    /// <summary>
    /// Retrieves all active waiting parties sorted by arrival time.
    /// </summary>
    /// <returns>A collection of waitlist entry DTOs.</returns>
    Task<IEnumerable<WaitlistEntryDto>> GetWaitlistAsync();

    /// <summary>
    /// Adds a new party to the waiting list and calculates their estimated wait time.
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    Task<WaitlistEntryDto> AddWaitlistEntryAsync(CreateWaitlistEntryDto dto);

    /// <summary>
    /// Updates the status of a waitlist entry (ex: "waiting", "contacted", "seated", "canceled", "no_show")
    /// </summary>
    /// <param name="waitlistId">The unique identifier of the waitlist entry.</param>
    /// <param name="dto">The payload containing the new status.</param>
    /// <returns>The updated waitlist entry DTO.</returns>
    /// <exception cref="Exceptions.NotFoundException">Thrown if the entry does not exist.</exception>
    Task<WaitlistEntryDto> UpdateWaitlistStatusAsync(string waitlistId, UpdateWaitlistStatusDto dto);

    #endregion

    #region Servers & Rotation

    /// <summary>
    /// Retrieves all clocked-in/active servers for the shift.
    /// </summary>
    /// <returns>A collection of server DTOs.</returns>
    Task<IEnumerable<ServerDto>> GetServersAsync();

    /// <summary>
    /// Clocks in and registers a server, placing their assigned section at the end of the rotation queue.
    /// </summary>
    /// <param name="dto">The payload containing the server name and section.</param>
    /// <returns>The clocked-in server DTO.</returns>
    /// <exception cref="Exceptions.BusinessValidationException">Thrown if a server with the same name or section is already clocked in.</exception>
    Task<ServerDto> ClockInServerAsync(ClockInServerDto dto);

    /// <summary>
    /// Clocks out a server, marks them as inactive, reassigns sections for remaining active servers, and dynamically
    /// transfers active seated tables to the new section owners.
    /// </summary>
    /// <param name="dto">The payload containing the server ID to clock out and section reassignments.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="Exceptions.NotFoundException">Thrown if the server does not exist.</exception>
    /// <exception cref="Exceptions.BusinessValidationException">Thrown if reassignments are duplicate or invalid.</exception>
    Task ClockOutServerAsync(ClockOutServerDto dto);

    /// <summary>
    /// Retrieves the current order of the server rotation queue.
    /// </summary>
    /// <returns>The rotation DTO outlining queue sequence and who is next up.</returns>
    Task<RotationDto> GetRotationAsync();

    /// <summary>
    /// Skips the next-up section in the queue, moving them to the end.
    /// </summary>
    /// <returns></returns>
    Task SkipRotationAsync();

    /// <summary>
    /// Resets the restaurant system for the next shift: clears all tables, clocks out all servers, archives waitlists, and clears rotation queue.
    /// </summary>
    /// <returns></returns>
    Task ResetShiftAsync();

    #endregion

    #region 

    /// <summary>
    /// Retrieves all reservations scheduled for today.
    /// </summary>
    /// <returns></returns>
    Task<IEnumerable<ReservationDto>> GetTodayReservationsAsync();

    /// <summary>
    /// Adds a new reservation for today or a future date.
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    Task<ReservationDto> AddReservationAsync(CreateReservationDto dto);

    /// <summary>
    /// Updates the status of an existing reservation (ex: booked, seated, no_show, canceled).
    /// </summary>
    /// <param name="reservationId"></param>
    /// <param name="status"></param>
    /// <returns></returns>
    Task<ReservationDto> UpdateReservationStatusAsync(string reservationId, string status);

    /// <summary>
    /// Cancel today's reservation and transfer the party to the top of the active waitlist.
    /// </summary>
    /// <param name="reservationId"></param>
    /// <returns></returns>
    Task<WaitlistEntryDto> MoveReservationToWaitlistAsync(string reservationId);

    #endregion
    
}