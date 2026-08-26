using Microsoft.AspNetCore.SignalR;

namespace TurnTable.Api.Hubs;

/// <summary>
/// SignalR Hub for broadcasting seating, server roster, and waitlist changes to all active clients in real-time.
/// </summary>
public class TurnTableHub : Hub
{
    // Leave empty for now, as the server only needs to broadcast
    // events downstream to clients (don't need to customize incoming client-to-server socket calls yet).
}