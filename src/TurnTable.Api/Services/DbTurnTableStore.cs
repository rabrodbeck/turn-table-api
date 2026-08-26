using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.Extensions.FileSystemGlobbing.Internal.PathSegments;
using Npgsql.Internal;
using TurnTable.Api.Data;
using TurnTable.Api.Exceptions;
using TurnTable.Api.Models.Domain;
using TurnTable.Api.Models.DTOs;

namespace TurnTable.Api.Services;

/// <summary>
/// Database-backed store utilizing Entity Framework Core to manage state in Supabase PostgreSQL.
/// </summary>
public class DbTurnTableStore : ITurnTableService
{
    private readonly TurnTableDbContext _context;

    public DbTurnTableStore(TurnTableDbContext context)
    {
        _context = context;
    }

    #region Dining Tables Implementation
    
    public async Task<IEnumerable<TableDto>> GetTablesAsync()
    {
        var tables = await _context.Tables.ToListAsync();
        var activeServers = await _context.Servers.Where(s => s.Active).ToListAsync();

        return tables.Select(t => MapToTableDto(t, activeServers));
    }

    public async Task<TableDto> SeatTableAsync(string tableId, SeatTableDto dto)
    {
        var table = await _context.Tables.FirstOrDefaultAsync(t => t.Id == tableId) ?? throw new NotFoundException($"Table '{tableId}' not found.");

        if (table.Status == "seated")
        {
            throw new BusinessValidationException($"Table '{tableId}' cannot be seated because its current status is 'seated'.");
        }

        if (dto.PartySize > table.MaxSeats)
        {
            throw new BusinessValidationException($"Guest count ({dto.PartySize}) exceeds Table {tableId} Max Capacity of {table.MaxSeats}.");
        }

        var server = await _context.Servers.FirstOrDefaultAsync(s => s.Id == dto.ServerId && s.Active) ?? throw new NotFoundException($"Active server '{dto.ServerId}' not found.");

        if (!SectionMatches(server.Section ?? "", table.CurrentServerSection))
        {
            throw new BusinessValidationException($"Server {server.Name} ({server.Section}) is not assigned to Table {tableId} ({table.CurrentServerSection}).");
        }

        // Link waitlist party if provided
        if (!string.IsNullOrEmpty(dto.WaitlistId))
        {
            var waitlistEntry = await _context.WaitlistEntries.FirstOrDefaultAsync(w => w.Id == dto.WaitlistId) ?? throw new NotFoundException($"Waitlist party '{dto.WaitlistId}' not found.");

            waitlistEntry.Status = "seated";
            table.PartyName = waitlistEntry.PartyName;
        }
        else
        {
            table.PartyName = "Walk-in";
        }

        table.Status = "seated";
        table.PartySize = dto.PartySize;
        table.ServerId = dto.ServerId;
        table.SeatedAt = DateTimeOffset.UtcNow;

        // Rotate next-up server's section to the back of the queue
        await RotateSectionToBackAsync(server.Section ?? "");

        await _context.SaveChangesAsync();

        var activeServers = await _context.Servers.Where(s => s.Active).ToListAsync();
        return MapToTableDto(table, activeServers);
    }

    public async Task<TableDto> UpdateTableStatusAsync(string tableId, UpdateTableStatusDto dto)
    {
        var table = await _context.Tables.FirstOrDefaultAsync(t => t.Id == tableId) ?? throw new NotFoundException($"Table '{tableId}' not found.");

        if (dto.Status.ToLowerInvariant() == "available")
        {
            ClearSeatingDetails(table);
        }
        else
        {
            table.Status = dto.Status;
        }

        await _context.SaveChangesAsync();

        var activeServers = await _context.Servers.Where(s => s.Active).ToListAsync();
        return MapToTableDto(table, activeServers);
    }

    public async Task<TableDto> ClearTableAsync(string tableId)
    {
        var table = await _context.Tables.FirstOrDefaultAsync(t => t.Id == tableId) ?? throw new NotFoundException($"Table '{tableId}' not found.");

        ClearSeatingDetails(table);

        await _context.SaveChangesAsync();

        var activeServers = await _context.Servers.Where(s => s.Active).ToListAsync();
        return MapToTableDto(table, activeServers);
    }
    
    #endregion

    #region Waitlist Implementation
    
    public async Task<IEnumerable<WaitlistEntryDto>> GetWaitlistAsync()
    {
        var list = await _context.WaitlistEntries
            .Where(w => w.Status == "waiting" || w.Status == "contacted" || w.Status == "seated")
            .OrderBy(w => w.CheckedInAt)
            .Select(w => MapToWaitlistDto(w))
            .ToListAsync();

        return list;
    }

    public async Task<WaitlistEntryDto> AddWaitlistEntryAsync(CreateWaitlistEntryDto dto)
    {
        if (dto.PartySize <= 0)
        {
            throw new BusinessValidationException("Party size must be greater than zero.");
        }

        var count = await _context.WaitlistEntries.CountAsync();
        var nextId = $"wt-{count + 201}";

        var entry = new WaitlistEntry
        {
            Id = nextId,
            PartyName = dto.PartyName,
            PartySize = dto.PartySize,
            PhoneNumber = dto.PhoneNumber,
            CheckedInAt = DateTimeOffset.UtcNow,
            QuotedWaitInMinutes = dto.QuotedWaitInMinutes,
            Status = "waiting"
        };

        _context.WaitlistEntries.Add(entry);
        await _context.SaveChangesAsync();

        return MapToWaitlistDto(entry);
    }

    public async Task<WaitlistEntryDto> UpdateWaitlistStatusAsync(string waitlistId, UpdateWaitlistStatusDto dto)
    {
        var entry = await _context.WaitlistEntries.FirstOrDefaultAsync(w => w.Id == waitlistId) ?? throw new NotFoundException($"Waitlist party '{waitlistId}' not found.");

        entry.Status = dto.Status.ToLowerInvariant();
        await _context.SaveChangesAsync();

        return MapToWaitlistDto(entry);
    }
    
    #endregion

    #region Servers & Rotation Implementation

    public async Task<IEnumerable<ServerDto>> GetServersAsync()
    {
        var servers = await _context.Servers.Where(s => s.Active).ToListAsync();
        return servers.Select(MapToServerDto);
    }

    public async Task<ServerDto> ClockInServerAsync(ClockInServerDto dto)
    {
        if (string.IsNullOrEmpty(dto.Name) || string.IsNullOrEmpty(dto.Section))
        {
            throw new BusinessValidationException("Server name and section assignment are required.");
        }
        var activeServers = await _context.Servers.Where(s => s.Active).ToListAsync();
        if (activeServers.Any(s => s.Name.Equals(dto.Name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new BusinessValidationException($"Server '{dto.Name}' is already clocked in.");
        }
        if (activeServers.Any(s => s.Section != null && s.Section.Equals(dto.Section, StringComparison.OrdinalIgnoreCase)))
        {
            throw new BusinessValidationException($"Section '{dto.Section}' is already assigned to an active server.");
        }
        var count = await _context.Servers.CountAsync();
        var nextId = $"srv-{count + 101}";
        var server = new Server
        {
            Id = nextId,
            Name = dto.Name,
            Active = true,
            Section = dto.Section
        };
        _context.Servers.Add(server);
        
        // Add new server to list so the redistribution helper can update all active sections together
        activeServers.Add(server);
        RedistributeServerSections(activeServers, server.Id, dto.Section);
        await _context.SaveChangesAsync();
        // Recalculate dynamic sections and rotation list
        await UpdateTableSectionsAndQueueAsync();
        await _context.SaveChangesAsync();
        return MapToServerDto(server);
    }

    public async Task<RotationDto> GetRotationAsync()
    {
        var rotation = new RotationDto();
        var queue = await _context.RotationQueue.OrderBy(q => q.SortOrder).ToListAsync();
        var activeServers = await _context.Servers.Where(s => s.Active).ToListAsync();

        if (queue.Any())
        {
            var nextSection = queue.First().Section;
            var nextServer = activeServers.FirstOrDefault(s => SectionMatches(s.Section ?? "", nextSection));
            if (nextServer != null)
            {
                rotation.NextServerUp = MapToServerDto(nextServer);
            }

            foreach (var qItem in queue)
            {
                var server = activeServers.FirstOrDefault(s => SectionMatches(s.Section ?? "", qItem.Section));
                if (server != null)
                {
                    rotation.Queue.Add(MapToServerDto(server));
                }
            }
        }

        return rotation;
    }

    public async Task SkipRotationAsync()
    {
        var queue = await _context.RotationQueue.OrderBy(q => q.SortOrder).ToListAsync();
        if (queue.Any())
        {
            var skippedItem = queue[0];
            queue.RemoveAt(0);
            queue.Add(skippedItem);

            for (int i = 0; i < queue.Count; i++)
            {
                queue[i].SortOrder = i;
            }

            await _context.SaveChangesAsync();
        }
    }

    public async Task ClockOutServerAsync(ClockOutServerDto dto)
    {
        // 1. Find the server to clock out
        var serverToClockOut = await _context.Servers.FirstOrDefaultAsync(s => s.Id == dto.ServerId && s.Active)
            ?? throw new NotFoundException($"Server with ID '{dto.ServerId}' is not clocked in.");

        // 2. Identify remainingn active servers
        var remainingServers = await _context.Servers.Where(s => s.Active && s.Id != dto.ServerId).ToListAsync();

        // 3. Mark the server as inactive
        serverToClockOut.Active = false;
        serverToClockOut.Section = string.Empty;

        // 4. Handle section assignments for remaining servers
        if (remainingServers.Count == 1)
        {
            remainingServers[0].Section = "Main Floor";
        }
        else if (remainingServers.Count > 1)
        {
            var reassignments = dto.Reassignments ?? new List<ServerReassignmentDto>();

            var reassignedIds = reassignments.Select(r => r.ServerId).ToHashSet();
            if (remainingServers.Any(s => !reassignedIds.Contains(s.Id)))
            {
                throw new BusinessValidationException("All remaining active servers must be reassigned a section.");
            }

            var uniqueSections = reassignments.Select(r => r.NewSection.ToLowerInvariant()).ToHashSet();
            if (uniqueSections.Count != remainingServers.Count)
            {
                throw new BusinessValidationException("Duplicate section assignments are not allowed.");
            }

            var validSections = GetValidSectionsForCount(remainingServers.Count);
            if (reassignments.Any(r => !validSections.Contains(r.NewSection.ToLowerInvariant())))
            {
                throw new BusinessValidationException($"Invalid section assignment. Valid sections for {remainingServers.Count} servers are: {string.Join(", ", validSections)}");
            }

            foreach (var r in reassignments)
            {
                var srv = remainingServers.First(s => s.Id == r.ServerId);
                srv.Section = r.NewSection;
            }
        }

        // Save server changes first so the database is updated before recalculating layouts
        await _context.SaveChangesAsync();

        // 5. Update table sections and rotation queue sections
        await UpdateTableSectionsAndQueueAsync();

        // 6. Transfer active seated tables to the new server covering their section
        var seatedTables = await _context.Tables.Where(t => t.Status == "seated").ToListAsync();
        var allActiveServers = await _context.Servers.Where(s => s.Active).ToListAsync();

        foreach (var table in seatedTables)
        {
            var newServer = allActiveServers.FirstOrDefault(s => SectionMatches(s.Section ?? "", table.CurrentServerSection));
            if (newServer != null)
            {
                table.ServerId = newServer.Id;
            }
        }

        await _context.SaveChangesAsync();
    }

    public async Task ResetShiftAsync()
    {
        // 1. Reset all physical tables to available and clear occupants
        var tables = await _context.Tables.ToListAsync();
        foreach (var table in tables)
        {
            table.Status = "available";
            table.PartyName = null;
            table.PartySize = null;
            table.SeatedAt = null;
            table.ServerId = null;
            table.CurrentServerSection = string.Empty;
        }

        // 2. Clock out all active servers
        var activeServers = await _context.Servers.Where(s => s.Active).ToListAsync();
        foreach (var server in activeServers)
        {
            server.Active = false;
            server.Section = string.Empty;
        }

        // 3. Complete and archive all active waitlist entries
        var activeWaitlist = await _context.WaitlistEntries
            .Where(w => w.Status == "waiting" || w.Status == "contacted")
            .ToListAsync();
        foreach (var entry in activeWaitlist)
        {
            entry.Status = "completed";
        }

        // 4. Empty the rotation queue
        var queueItems = await _context.RotationQueue.ToListAsync();
        _context.RotationQueue.RemoveRange(queueItems);

        await _context.SaveChangesAsync();
    }

    #endregion

    #region Private Helper Methods

    private async Task UpdateTableSectionsAndQueueAsync()
    {
        var activeServers = await _context.Servers.Where(s => s.Active).ToListAsync();
        var tables = await _context.Tables.ToListAsync();

        // 1. Update table section boundaries
        foreach (var table in tables)
        {
            table.CurrentServerSection = GetSectionForTable(table.Id, activeServers.Count);
        }

        // 2. Load the current database rotation queue
        var currentQueue = await _context.RotationQueue.OrderBy(q => q.SortOrder).ToListAsync();

        var activeSections = activeServers
            .Select(s => s.Section)
            .Where(sec => !string.IsNullOrEmpty(sec))
            .Cast<string>()
            .ToList();

        // 3. Remove sections that are no longer active
        var itemsToRemove = currentQueue
            .Where(q => !activeSections.Any(asName => SectionMatches(asName, q.Section)))
            .ToList();

        if (itemsToRemove.Any())
        {
            _context.RotationQueue.RemoveRange(itemsToRemove);
            foreach (var item in itemsToRemove)
            {
                currentQueue.Remove(item);
            }
        }

        // 4. Add newly activated sections to the end of the queue
        foreach (var section in activeSections)
        {
            if (!currentQueue.Any(q => SectionMatches(q.Section, section)))
            {
                var newItem = new RotationQueueItem
                {
                    Section = section,
                    SortOrder = currentQueue.Count
                };
                _context.RotationQueue.Add(newItem);
                currentQueue.Add(newItem);
            }
        }

        // 5. Re-normalize sort order indexes
        for (int i = 0; i < currentQueue.Count; i++)
        {
            currentQueue[i].SortOrder = i;
        }
    }

    private static string GetSectionForTable(string tableId, int activeServerCount)
    {
        if (activeServerCount <= 1)
        {
            return "Main Floor";
        }
        if (activeServerCount == 2)
        {
            var sectionTables = new[] { "11", "12", "21", "22A", "22B", "31", "32", "51", "52", "53" };
            return sectionTables.Contains(tableId) ? "Front" : "Back";
        }
        else if (activeServerCount == 3)
        {
            var section1Tables = new[] { "11", "12", "13", "15", "21" };
            var section2Tables = new[] { "16", "22A", "22B", "23", "25", "35" };
            if (section1Tables.Contains(tableId)) return "Section 1";
            if (section2Tables.Contains(tableId)) return "Section 2";
            return "Section 3";
        }
        else
        {
            var section1Tables = new[] { "11", "12", "21", "22A", "22B" };
            var section2Tables = new[] { "13", "15", "16", "23" };
            var section3Tables = new[] { "25", "32", "33", "35" };
            if (section1Tables.Contains(tableId)) return "Section 1";
            if (section2Tables.Contains(tableId)) return "Section 2";
            if (section3Tables.Contains(tableId)) return "Section 3";
            return "Section 4";
        }
    }

    private static void RedistributeServerSections(List<Server> activeServers, string newServerId, string newSection)
    {
        var count = activeServers.Count;
        if (count <= 1)
        {
            foreach (var s in activeServers)
            {
                s.Section = "Main Floor";
            }
            return;
        }
        if (count == 2)
        {
            var newServer = activeServers.First(s => s.Id == newServerId);
            newServer.Section = newSection;
            var otherServer = activeServers.First(s => s.Id != newServerId);
            otherServer.Section = newSection.Equals("Front", StringComparison.OrdinalIgnoreCase) ? "Back" : "Front";
            return;
        }
        if (count == 3)
        {
            var newServer = activeServers.First(s => s.Id == newServerId);
            newServer.Section = newSection;
            var remaining = activeServers.Where(s => s.Id != newServerId).ToList();
            var targetSections = new List<string> { "Section 1", "Section 2", "Section 3" };
            targetSections.Remove(newSection);
            var firstRemaining = remaining[0];
            var secondRemaining = remaining[1];
            // Sort so that Section 1/Front is processed first
            bool firstIsFront = firstRemaining.Section != null && 
                (firstRemaining.Section.Equals("Front", StringComparison.OrdinalIgnoreCase) || 
                 firstRemaining.Section.Equals("Section 1", StringComparison.OrdinalIgnoreCase));
            
            var sortedRemaining = firstIsFront 
                ? new List<Server> { firstRemaining, secondRemaining } 
                : new List<Server> { secondRemaining, firstRemaining };
            sortedRemaining[0].Section = targetSections[0];
            sortedRemaining[1].Section = targetSections[1];
            return;
        }
        if (count == 4)
        {
            var newServer = activeServers.First(s => s.Id == newServerId);
            newServer.Section = newSection;
            var remaining = activeServers.Where(s => s.Id != newServerId).ToList();
            var targetSections = new List<string> { "Section 1", "Section 2", "Section 3", "Section 4" };
            targetSections.Remove(newSection);
            var sortedRemaining = remaining.OrderBy(s => s.Section ?? "").ToList();
            for (int i = 0; i < sortedRemaining.Count; i++)
            {
                sortedRemaining[i].Section = targetSections[i];
            }
            return;
        }
    }

    private async Task RotateSectionToBackAsync(string section)
    {
        var queue = await _context.RotationQueue.OrderBy(q => q.SortOrder).ToListAsync();
        var match = queue.FirstOrDefault(q => SectionMatches(q.Section, section));
        if (match != null)
        {
            queue.Remove(match);
            queue.Add(match);

            for (int i = 0; i < queue.Count; i++)
            {
                queue[i].SortOrder = i;
            }
        }
    }

    private static bool SectionMatches(string serverSection, string tableSection)
    {
        if (serverSection.Equals(tableSection, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var s = serverSection.ToLowerInvariant();
        var t = tableSection.ToLowerInvariant();
        if (s == "section 1" && t == "front") return true;
        if (s == "section 2" && t == "back") return true;
        if (s == "front" && t == "section 1") return true;
        if (s == "back" && t == "section 2") return true;

        return false;
    }

    private static void ClearSeatingDetails(Table table)
    {
        table.Status = "available";
        table.PartyName = null;
        table.PartySize = null;
        table.SeatedAt = null;
        table.ServerId = null;
    }

    private static HashSet<string> GetValidSectionsForCount(int serverCount)
    {
        if (serverCount == 2)
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "front", "back" };
        }
        if (serverCount == 3)
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "section 1", "section 2", "section 3" };
        }
        if (serverCount == 4)
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "section 1", "section 2", "section 3", "section 4" };
        }
        return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }
    private TableDto MapToTableDto(Table table, List<Server> activeServers)
    {
        var server = string.IsNullOrEmpty(table.ServerId)
            ? null
            : activeServers.FirstOrDefault(s => s.Id == table.ServerId);
        return new TableDto
        {
            Id = table.Id,
            TypeOrShape = table.TypeOrShape,
            MaxSeats = table.MaxSeats,
            CurrentServerSection = table.CurrentServerSection,
            Status = table.Status,
            PartyName = table.PartyName,
            PartySize = table.PartySize,
            SeatedAt = table.SeatedAt,
            ServerId = table.ServerId,
            ServerName = server?.Name
        };
    }
    private static WaitlistEntryDto MapToWaitlistDto(WaitlistEntry entry)
    {
        return new WaitlistEntryDto
        {
            Id = entry.Id,
            PartyName = entry.PartyName,
            PartySize = entry.PartySize,
            PhoneNumber = entry.PhoneNumber,
            CheckedInAt = entry.CheckedInAt,
            QuotedWaitInMinutes = entry.QuotedWaitInMinutes,
            Status = entry.Status
        };
    }
    private static ServerDto MapToServerDto(Server server)
    {
        return new ServerDto
        {
            Id = server.Id,
            Name = server.Name,
            Active = server.Active,
            Section = server.Section
        };
    }
    #endregion
}