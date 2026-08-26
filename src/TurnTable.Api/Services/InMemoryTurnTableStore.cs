using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using TurnTable.Api.Exceptions;
using TurnTable.Api.Models.Domain;
using TurnTable.Api.Models.DTOs;

namespace TurnTable.Api.Services;

/// <summary>
/// Thread-safe in-memory store managing all restaurant layout configurations, 
/// server shift check-ins, waitlists, and round-robin section rotations.
/// </summary>
public class InMemoryTurnTableStore : ITurnTableService
{
    private readonly object _lock = new();
    private readonly List<Table> _tables = new();
    private readonly List<Server> _servers = new();
    private readonly List<WaitlistEntry> _waitlist = new();
    private readonly List<string> _rotationQueue = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryTurnTableStore"/> class and seeds default restaurant layout configurations.
    /// </summary>
    public InMemoryTurnTableStore()
    {
        // 1. Seed the 17 tables based on layout configurations (Table 35 set to 2-Seats Square)
        _tables.AddRange(new List<Table>
        {
            // Row 1
            new() 
            { 
                Id = "11", 
                TypeOrShape = "rectangle", 
                MaxSeats = 4,
                Status = "seated",
                PartyName = "Davis Party",
                PartySize = 3,
                SeatedAt = DateTimeOffset.UtcNow.AddMinutes(-54), // Seated 54m ago
                ServerId = "srv-101" // Sarah K. (Section 1)
            },
            new() { Id = "21", TypeOrShape = "rectangle", MaxSeats = 6 },
            new() { Id = "31", TypeOrShape = "rectangle", MaxSeats = 6 },
            new() { Id = "51", TypeOrShape = "rectangle", MaxSeats = 4 },

            // Row 2
            new() { Id = "12", TypeOrShape = "rectangle", MaxSeats = 4 },
            new() 
            { 
                Id = "22A", 
                TypeOrShape = "square", 
                MaxSeats = 2,
                Status = "seated",
                PartyName = "Miller Party",
                PartySize = 2,
                SeatedAt = DateTimeOffset.UtcNow.AddMinutes(-23), // Seated 23m ago
                ServerId = "srv-102" // David M. (Section 2)
            },
            new() { Id = "22B", TypeOrShape = "square", MaxSeats = 2 },
            new() { Id = "32", TypeOrShape = "circle", MaxSeats = 5 },
            new() { Id = "52", TypeOrShape = "rectangle", MaxSeats = 4 },

            // Row 3
            new() { Id = "13", TypeOrShape = "square", MaxSeats = 2 },
            new() { Id = "23", TypeOrShape = "circle", MaxSeats = 5 },
            new() { Id = "33", TypeOrShape = "square", MaxSeats = 2 },
            new() { Id = "53", TypeOrShape = "rectangle", MaxSeats = 4 },

            // Row 4
            new() { Id = "15", TypeOrShape = "rectangle", MaxSeats = 4 },
            new() { Id = "25", TypeOrShape = "rectangle", MaxSeats = 4 },
            new() { Id = "35", TypeOrShape = "square", MaxSeats = 2 }, // Adjusted to 2-person square

            // Row 5
            new() { Id = "16", TypeOrShape = "rectangle", MaxSeats = 4 }
        });

        // 2. Seed 3 default active servers
        _servers.AddRange(new List<Server>
        {
            new() { Id = "srv-101", Name = "Sarah K.", Active = true, Section = "Section 1" },
            new() { Id = "srv-102", Name = "David M.", Active = true, Section = "Section 2" },
            new() { Id = "srv-103", Name = "Kelly R.", Active = true, Section = "Section 3" }
        });

        // 3. Seed initial waitlist entries matching design
        _waitlist.AddRange(new List<WaitlistEntry>
        {
            new()
            {
                Id = "wt-201",
                PartyName = "Johnson Party",
                PartySize = 4,
                PhoneNumber = "555-0199",
                CheckedInAt = DateTimeOffset.UtcNow.AddMinutes(-8), // Seated/Added 8m ago (Green/Yellow border check)
                QuotedWaitInMinutes = 30,
                Status = "waiting"
            },
            new()
            {
                Id = "wt-202",
                PartyName = "Alex Party",
                PartySize = 2,
                PhoneNumber = "555-0122",
                CheckedInAt = DateTimeOffset.UtcNow.AddMinutes(-12), // Seated/Added 12m ago (Yellow warning check)
                QuotedWaitInMinutes = 20,
                Status = "contacted"
            },
            new()
            {
                Id = "wt-203",
                PartyName = "Smith Party",
                PartySize = 3,
                PhoneNumber = "555-0188",
                CheckedInAt = DateTimeOffset.UtcNow.AddMinutes(-33), // Seated/Added 33m ago (Overdue Red alert check)
                QuotedWaitInMinutes = 30,
                Status = "waiting"
            }
        });

        // Calculate initial dynamic sections and set up queue
        UpdateTableSectionsAndQueue();
    }

    #region Dining Tables Implementation

    /// <inheritdoc />
    public Task<IEnumerable<TableDto>> GetTablesAsync()
    {
        lock (_lock)
        {
            return Task.FromResult(_tables.Select(MapToTableDto));
        }
    }

    /// <inheritdoc />
    public Task<TableDto> SeatTableAsync(string tableId, SeatTableDto dto)
    {
        lock (_lock)
        {
            var table = _tables.FirstOrDefault(t => t.Id == tableId)
                ?? throw new NotFoundException($"Table '{tableId}' not found.");

            if (table.Status != "available")
            {
                throw new BusinessValidationException($"Table '{tableId}' cannot be seated because its current status is '{table.Status}'.");
            }

            if (dto.PartySize > table.MaxSeats)
            {
                throw new BusinessValidationException($"Cannot seat party of size {dto.PartySize} at Table '{tableId}' (Max Capacity: {table.MaxSeats}).");
            }

            var server = _servers.FirstOrDefault(s => s.Id == dto.ServerId && s.Active)
                ?? throw new NotFoundException($"Active server with ID '{dto.ServerId}' not found.");

            // Seating properties
            table.Status = "seated";
            table.PartySize = dto.PartySize;
            table.ServerId = server.Id;
            table.SeatedAt = DateTimeOffset.UtcNow;

            // Optional waitlist correlation
            if (!string.IsNullOrEmpty(dto.WaitlistId))
            {
                var waitlistEntry = _waitlist.FirstOrDefault(w => w.Id == dto.WaitlistId)
                    ?? throw new NotFoundException($"Waitlist entry '{dto.WaitlistId}' not found.");

                if (waitlistEntry.Status == "seated")
                {
                    throw new BusinessValidationException($"Waitlist entry '{dto.WaitlistId}' is already marked as seated.");
                }

                waitlistEntry.Status = "seated";
                table.PartyName = waitlistEntry.PartyName;
            }
            else
            {
                table.PartyName = "Walk-in";
            }

            // Move the seated table's section to the back of the rotation queue
            RotateSectionToBack(table.CurrentServerSection);

            return Task.FromResult(MapToTableDto(table));
        }
    }

    /// <inheritdoc />
    public Task<TableDto> UpdateTableStatusAsync(string tableId, UpdateTableStatusDto dto)
    {
        lock (_lock)
        {
            var table = _tables.FirstOrDefault(t => t.Id == tableId)
                ?? throw new NotFoundException($"Table '{tableId}' not found.");

            table.Status = dto.Status.ToLowerInvariant();

            // Clear occupant details if table status returns to available
            if (table.Status == "available")
            {
                ClearSeatingDetails(table);
            }

            return Task.FromResult(MapToTableDto(table));
        }
    }

    /// <inheritdoc />
    public Task<TableDto> ClearTableAsync(string tableId)
    {
        lock (_lock)
        {
            var table = _tables.FirstOrDefault(t => t.Id == tableId)
                ?? throw new NotFoundException($"Table '{tableId}' not found.");

            table.Status = "available";
            ClearSeatingDetails(table);

            return Task.FromResult(MapToTableDto(table));
        }
    }

    #endregion

    #region Waitlist Implementation

    /// <inheritdoc />
    public Task<IEnumerable<WaitlistEntryDto>> GetWaitlistAsync()
    {
        lock (_lock)
        {
            // Sorted chronologically by check-in time, returning active waitlist items
            var list = _waitlist
                .OrderBy(w => w.CheckedInAt)
                .Select(MapToWaitlistDto);

            return Task.FromResult(list);
        }
    }

    /// <inheritdoc />
    public Task<WaitlistEntryDto> AddWaitlistEntryAsync(CreateWaitlistEntryDto dto)
    {
        lock (_lock)
        {
            if (dto.PartySize <= 0)
            {
                throw new BusinessValidationException("Party size must be greater than zero.");
            }

            var nextId = $"wt-{_waitlist.Count + 201}"; // Start waitlist IDs at wt-201
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

            _waitlist.Add(entry);
            return Task.FromResult(MapToWaitlistDto(entry));
        }
    }

    /// <inheritdoc />
    public Task<WaitlistEntryDto> UpdateWaitlistStatusAsync(string waitlistId, UpdateWaitlistStatusDto dto)
    {
        lock (_lock)
        {
            var entry = _waitlist.FirstOrDefault(w => w.Id == waitlistId)
                ?? throw new NotFoundException($"Waitlist entry '{waitlistId}' not found.");

            entry.Status = dto.Status.ToLowerInvariant();
            return Task.FromResult(MapToWaitlistDto(entry));
        }
    }

    #endregion

    #region Servers & Rotation Implementation

    /// <inheritdoc />
    public Task<IEnumerable<ServerDto>> GetServersAsync()
    {
        lock (_lock)
        {
            return Task.FromResult(_servers.Select(MapToServerDto));
        }
    }

    /// <inheritdoc />
    public Task<ServerDto> ClockInServerAsync(ClockInServerDto dto)
    {
        lock (_lock)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                throw new BusinessValidationException("Server name cannot be empty.");
            }

            if (string.IsNullOrWhiteSpace(dto.Section))
            {
                throw new BusinessValidationException("Section name cannot be empty.");
            }

            var existingServer = _servers.FirstOrDefault(s => s.Name.Equals(dto.Name, StringComparison.OrdinalIgnoreCase));
            if (existingServer != null && existingServer.Active)
            {
                throw new BusinessValidationException($"Server '{dto.Name}' is already clocked in.");
            }

            var sectionConflict = _servers.FirstOrDefault(s => s.Section != null && s.Section.Equals(dto.Section, StringComparison.OrdinalIgnoreCase) && s.Active);
            if (sectionConflict != null)
            {
                throw new BusinessValidationException($"Section '{dto.Section}' is already assigned to active server '{sectionConflict.Name}'.");
            }

            Server server;
            if (existingServer != null)
            {
                // Re-activate existing server profile
                server = existingServer;
                server.Active = true;
                server.Section = dto.Section;
            }
            else
            {
                // Register a new server profile
                var nextId = $"srv-{_servers.Count + 101}";
                server = new Server
                {
                    Id = nextId,
                    Name = dto.Name,
                    Active = true,
                    Section = dto.Section
                };
                _servers.Add(server);
            }

            // Recalculate dynamic sections and rotation list
            UpdateTableSectionsAndQueue();

            return Task.FromResult(MapToServerDto(server));
        }
    }

    /// <inheritdoc />
    public Task<RotationDto> GetRotationAsync()
    {
        lock (_lock)
        {
            var rotation = new RotationDto();

            if (_rotationQueue.Any())
            {
                var nextSection = _rotationQueue.First();
                var nextServer = _servers.FirstOrDefault(s => s.Active && SectionMatches(s.Section ?? "", nextSection));
                if (nextServer != null)
                {
                    rotation.NextServerUp = MapToServerDto(nextServer);
                }

                foreach (var section in _rotationQueue)
                {
                    var server = _servers.FirstOrDefault(s => s.Active && SectionMatches(s.Section ?? "", section));
                    if (server != null)
                    {
                        rotation.Queue.Add(MapToServerDto(server));
                    }
                }
            }

            return Task.FromResult(rotation);
        }
    }

    /// <inheritdoc />
    public Task SkipRotationAsync()
    {
        lock (_lock)
        {
            if (_rotationQueue.Any())
            {
                var skippedSection = _rotationQueue[0];
                _rotationQueue.RemoveAt(0);
                _rotationQueue.Add(skippedSection);
            }
            return Task.CompletedTask;
        }
    }

    /// <inheritdoc />
    public Task ClockOutServerAsync(ClockOutServerDto dto)
    {
        lock (_lock)
        {
            // 1. Find the server to clock out
            var serverToClockOut = _servers.FirstOrDefault(s => s.Id == dto.ServerId && s.Active)
                ?? throw new NotFoundException($"Server with ID '{dto.ServerId}' is not clocked in.");

            // 2. Identify remaining active servers
            var remainingServers = _servers.Where(s => s.Active && s.Id != dto.ServerId).ToList();

            // 3. Mark the server as inactive
            serverToClockOut.Active = false;
            serverToClockOut.Section = string.Empty;

            // 4. Handle section assignments for remaining servers
            if (remainingServers.Count == 1)
            {
                // Edge Case: If only 1 server remains, they cover the entire restaurant
                remainingServers[0].Section = "Main Floor";
            }
            else if (remainingServers.Count > 1)
            {
                var reassignments = dto.Reassignments ?? new List<ServerReassignmentDto>();

                // Ensure all remaining active servers are accounted for in reassignments
                var reassignedIds = reassignments.Select(r => r.ServerId).ToHashSet();
                if (remainingServers.Any(s => !reassignedIds.Contains(s.Id)))
                {
                    throw new BusinessValidationException("All remaining active servers must be reassigned a section.");
                }

                // Check that target sections are unique
                var uniqueSections = reassignments.Select(r => r.NewSection.ToLowerInvariant()).ToHashSet();
                if (uniqueSections.Count != remainingServers.Count)
                {
                    throw new BusinessValidationException("Duplicate section assignments are not allowed.");
                }

                // Validate that section names are legal for the target count
                var validSections = GetValidSectionsForCount(remainingServers.Count);
                if (reassignments.Any(r => !validSections.Contains(r.NewSection.ToLowerInvariant())))
                {
                    throw new BusinessValidationException($"Invalid section assignment. Valid sections for {remainingServers.Count} servers are: {string.Join(", ", validSections)}");
                }

                // Apply new section assignments
                foreach (var r in reassignments)
                {
                    var srv = remainingServers.First(s => s.Id == r.ServerId);
                    srv.Section = r.NewSection;
                }
            }

            // 5. Update table sections and rotation queue sections
            UpdateTableSectionsAndQueue();

            // 6. Transfer any active seated tables to the new server covering their section
            foreach (var table in _tables.Where(t => t.Status == "seated"))
            {
                var newServer = _servers.FirstOrDefault(s => s.Active && SectionMatches(s.Section ?? "", table.CurrentServerSection));
                if (newServer != null)
                {
                    table.ServerId = newServer.Id;
                }
            }

            return Task.CompletedTask;
        }
    }

    /// <inheritdoc />
    public Task ResetShiftAsync()
    {
        lock (_lock)
        {
            // 1. Reset all tables to available and clear details
            foreach (var table in _tables)
            {
                table.Status = "available";
                table.PartyName = null;
                table.PartySize = null;
                table.SeatedAt = null;
                table.ServerId = null;
                table.CurrentServerSection = string.Empty;
            }

            // 2. Clock out all servers
            foreach (var server in _servers)
            {
                server.Active = false;
                server.Section = string.Empty;
            }

            // 3. Complete active waitlist entries
            foreach (var entry in _waitlist)
            {
                if (entry.Status == "waiting" || entry.Status == "contacted")
                {
                    entry.Status = "completed";
                }
            }

            // 4. Clear the roation queue
            _rotationQueue.Clear();

            return Task.CompletedTask;
        }
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

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Divides tables into sections dynamically based on active server count, and resets the rotation queue order.
    /// </summary>
    private void UpdateTableSectionsAndQueue()
    {
        var activeServers = _servers.Where(s => s.Active == true).ToList();
        
        // Apply dynamic grid layout mappings
        foreach (var table in _tables)
        {
            table.CurrentServerSection = GetSectionForTable(table.Id, activeServers.Count);
        }

        // Rebuild rotation queue sections based on active server section assignments
        var activeSections = activeServers
            .Select(s => s.Section)
            .Where(sec => !string.IsNullOrEmpty(sec))
            .Cast<string>()
            .ToList();

        // Remove sections that are no longer active
        _rotationQueue.RemoveAll(q => !activeSections.Contains(q));

        // Add newly activated sections to the end of the rotation queue
        foreach (var section in activeSections)
        {
            if (!_rotationQueue.Contains(section))
            {
                _rotationQueue.Add(section);
            }
        }
    }

    /// <summary>
    /// Gets the assigned section for a table, mapping based on the active server count.
    /// </summary>
    /// <param name="tableId">The unique identifier of the table.</param>
    /// <param name="activeServerCount">Active number of servers on shift.</param>
    /// <returns></returns>
    private static string GetSectionForTable(string tableId, int activeServerCount)
    {
        // Section breakdown per server count:
        // 1: Main Floor = All tables
        // 2: Front = 11, 12, 21, 22A, 22B, 31, 32, 51, 52, 53
        // 2: Back = 12, 15, 16, 23, 25, 33, 35
        // 3: Section 1 = 11, 12, 13, 15, 21
        // 3: Section 2 = 16, 22A, 22B, 23, 25, 35
        // 3: Section 3 - 31, 32, 33 51, 52, 53
        // 4: Section 1 = 11, 12, 21, 22A, 22B
        // 4: Section 2 = 13, 15, 16, 23
        // 4: Section 3 = 25, 32, 33, 35
        // 4: Section 4 = 31, 51, 52, 53
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
            var section1Tables = new [] { "11", "12", "13", "15", "21" };
            var section2Tables = new [] { "16", "22A", "22B", "23", "25", "35" };

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

    /// <summary>
    /// Moves the specified section to the back of the rotation queue list.
    /// </summary>
    /// <param name="section">Section to be moved to the back of the queue.</param>
    private void RotateSectionToBack(string section)
    {
        var matchingSection = _rotationQueue.FirstOrDefault(q => SectionMatches(q, section));
        if (matchingSection != null)
        {
            _rotationQueue.Remove(matchingSection);
            _rotationQueue.Add(matchingSection);
        }
    }

    /// <summary>
    /// Helper to compare server and table sections dynamically, supporting Front/Back aliases for Section 1/2.
    /// </summary>
    /// <param name="serverSection">The section name assigned to the server.</param>
    /// <param name="tableSection">The section name assigned to the table.</param>
    /// <returns>True if they match directly or via fallback aliases; false otherwise.</returns>
    private static bool SectionMatches(string serverSection, string tableSection)
    {
        if (serverSection.Equals(tableSection, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // 2-Server Case: Section 1 <=> Front, Section 2 <=> Back
        if (serverSection.Equals("Section 1", StringComparison.OrdinalIgnoreCase) && tableSection.Equals("Front", StringComparison.OrdinalIgnoreCase)) return true;
        if (serverSection.Equals("Section 2", StringComparison.OrdinalIgnoreCase) && tableSection.Equals("Back", StringComparison.OrdinalIgnoreCase)) return true;
        if (serverSection.Equals("Front", StringComparison.OrdinalIgnoreCase) && tableSection.Equals("Section 1", StringComparison.OrdinalIgnoreCase)) return true;
        if (serverSection.Equals("Back", StringComparison.OrdinalIgnoreCase) && tableSection.Equals("Section 2", StringComparison.OrdinalIgnoreCase)) return true;

        return false;
    }

    /// <summary>
    /// Clears the guest and timing occupancy properties from a table.
    /// </summary>
    /// <param name="table">Table to be cleared.</param>
    private static void ClearSeatingDetails(Table table)
    {
        table.PartyName = null;
        table.PartySize = null;
        table.SeatedAt = null;
        table.ServerId = null;
    }

    /// <summary>
    /// Maps a Table domain model to its corresponding TableDto response model.
    /// </summary>
    /// <param name="table">Table domain model to be mapped.</param>
    /// <returns></returns>
    private TableDto MapToTableDto(Table table)
    {
        // Dynamically look up the server name based on ServerId
        var server = string.IsNullOrEmpty(table.ServerId)
            ? null
            : _servers.FirstOrDefault(s => s.Id == table.ServerId);

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

    /// <summary>
    /// Maps a WaitlistEntry domain model to its corresponding WaitlistEntryDto response model.
    /// </summary>
    /// <param name="entry">WaitlistEntry domain model to be mapped.</param>
    /// <returns></returns>
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

    /// <summary>
    /// Maps a Server domain model to its corresponding ServerDto response model.
    /// </summary>
    /// <param name="server">Server domain model to be mapped.</param>
    /// <returns></returns>
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