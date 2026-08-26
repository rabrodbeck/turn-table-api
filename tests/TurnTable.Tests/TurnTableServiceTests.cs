using Microsoft.EntityFrameworkCore;
using TurnTable.Api.Data;
using TurnTable.Api.Exceptions;
using TurnTable.Api.Models.DTOs;
using TurnTable.Api.Models.Domain;
using TurnTable.Api.Services;
using System.Reflection;

namespace TurnTable.Tests;

/// <summary>
/// Unit test suite for the <see cref="InMemoryTurnTableStore"/> and <see cref="ITurnTableService"/> logic.
/// </summary>
public class TurnTableServiceTests
{
        /// <summary>
    /// Helper to instantiate a fresh, isolated database-backed store using EF Core InMemory.
    /// </summary>
    private static ITurnTableService CreateService()
    {
        // 1. Setup a fresh, isolated database for each test execution
        var options = new DbContextOptionsBuilder<TurnTableDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new TurnTableDbContext(options);
        context.Database.EnsureCreated(); // Creates schema and seeds the 17 static physical tables

        // 2. Seed Mock Servers for testing
        context.Servers.AddRange(
            new Server { Id = "srv-101", Name = "Sarah K.", Active = true, Section = "Section 1" },
            new Server { Id = "srv-102", Name = "David M.", Active = true, Section = "Section 2" },
            new Server { Id = "srv-103", Name = "Kelly R.", Active = true, Section = "Section 3" }
        );

        // 3. Seed Mock Waitlist Entries for testing
        context.WaitlistEntries.AddRange(
            new WaitlistEntry 
            { 
                Id = "wt-201", 
                PartyName = "Johnson Party", 
                PartySize = 4, 
                CheckedInAt = DateTimeOffset.UtcNow.AddMinutes(-15), 
                QuotedWaitInMinutes = 30, 
                Status = "waiting" 
            },
            new WaitlistEntry 
            { 
                Id = "wt-202", 
                PartyName = "Alex Party", 
                PartySize = 2, 
                CheckedInAt = DateTimeOffset.UtcNow.AddMinutes(-12), 
                QuotedWaitInMinutes = 20, 
                Status = "waiting" 
            },
            new WaitlistEntry 
            { 
                Id = "wt-203", 
                PartyName = "Smith Party", 
                PartySize = 4, 
                CheckedInAt = DateTimeOffset.UtcNow.AddMinutes(-33), 
                QuotedWaitInMinutes = 30, 
                Status = "waiting" 
            }
        );

        // 4. Seed Mock Seated Tables (Table 11: Davis Party; Table 22A: Miller Party)
        var table11 = context.Tables.Find("11");
        if (table11 != null)
        {
            table11.Status = "seated";
            table11.PartyName = "Davis Party";
            table11.PartySize = 4;
            table11.ServerId = "srv-101";
            table11.SeatedAt = DateTimeOffset.UtcNow.AddMinutes(-54);
        }

        var table22A = context.Tables.Find("22A");
        if (table22A != null)
        {
            table22A.Status = "seated";
            table22A.PartyName = "Miller Party";
            table22A.PartySize = 2;
            table22A.ServerId = "srv-102";
            table22A.SeatedAt = DateTimeOffset.UtcNow.AddMinutes(-23);
        }

                // 5. Seed Mock Rotation Queue
        context.RotationQueue.AddRange(
            new RotationQueueItem { Section = "Section 1", SortOrder = 0 },
            new RotationQueueItem { Section = "Section 2", SortOrder = 1 },
            new RotationQueueItem { Section = "Section 3", SortOrder = 2 }
        );

        // 6. Assign initial sections to the physical tables matching the 3-server layout
        foreach (var table in context.Tables)
        {
            var section1Tables = new[] { "11", "12", "13", "15", "21" };
            var section2Tables = new[] { "16", "22A", "22B", "23", "25", "35" };

            if (section1Tables.Contains(table.Id)) table.CurrentServerSection = "Section 1";
            else if (section2Tables.Contains(table.Id)) table.CurrentServerSection = "Section 2";
            else table.CurrentServerSection = "Section 3";
        }

        context.SaveChanges();

        return new DbTurnTableStore(context);
    }

    #region Dining Tables Seating & Clearing Tests
    
    /// <summary>
    /// Verifies that seating a walk-in party successfully updates the table state.
    /// </summary>
    [Fact]
    public async Task SeatTableAsync_ValidWalkIn_UpdatesTableStatusAndServerId()
    {
        // Arrange
        var service = CreateService();
        var seatDto = new SeatTableDto
        {
            PartySize = 2,
            ServerId = "srv-101", // Sarah K.
            WaitlistId = null
        };

        // Act
        var result = await service.SeatTableAsync("13", seatDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("seated", result.Status);
        Assert.Equal(2, result.PartySize);
        Assert.Equal("srv-101", result.ServerId);
        Assert.Equal("Sarah K.", result.ServerName);
        Assert.Equal("Walk-in", result.PartyName);
        Assert.NotNull(result.SeatedAt);
    }

    /// <summary>
    /// Verifies that seating a guest from the waitlist updates both table and waitlist entry states.
    /// </summary>
    [Fact]
    public async Task SeatTableAsync_ValidWaitlistParty_UpdatesTableAndWaitlistStatus()
    {
        // Arrange
        var service = CreateService();
        await service.ClearTableAsync("11"); // Clear default seeded table 11
        var seatDto = new SeatTableDto
        {
            PartySize = 4,
            ServerId = "srv-101", // Sarah K.
            WaitlistId = "wt-201" // Johnson Party (size 4)
        };

        // Act
        var result = await service.SeatTableAsync("11", seatDto);
        var waitlist = (await service.GetWaitlistAsync()).ToList();
        var seatedWaitlistEntry = waitlist.FirstOrDefault(w => w.Id == "wt-201");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("seated", result.Status);
        Assert.Equal("Johnson Party", result.PartyName);
        Assert.NotNull(seatedWaitlistEntry);
        Assert.Equal("seated", seatedWaitlistEntry.Status);
    }

    /// <summary>
    /// Verifies that seating a party exceeding table capacity throws a validation exception.
    /// </summary>
    [Fact]
    public async Task SeatTableAsync_PartySizeExceedsCapacity_ThrowsBusinessValidationException()
    {
        // Arrange
        var service = CreateService();
        var seatDto = new SeatTableDto
        {
            PartySize = 4, // Table 13 max capacity is 2
            ServerId = "srv-101",
            WaitlistId = null
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BusinessValidationException>(() =>
            service.SeatTableAsync("13", seatDto));

        Assert.Contains("Max Capacity", exception.Message);
    }

    [Fact]
    public async Task ClearTableAsync_OccupiedTable_ResetsSeatingProperties()
    {
        // Arrange
        var service = CreateService();

        // Seat a table first
        var seatDto = new SeatTableDto { PartySize = 2, ServerId = "srv-101" };
        await service.SeatTableAsync("13", seatDto);

        // Act
        var result = await service.ClearTableAsync("13");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("available", result.Status);
        Assert.Null(result.PartyName);
        Assert.Null(result.PartySize);
        Assert.Null(result.SeatedAt);
        Assert.Null(result.ServerId);
        Assert.Null(result.ServerName);
    }
    
    #endregion

    #region Waitlist Tests

    /// <summary>
    /// Verifies checking in a new waitlist party adds them to the list.
    /// </summary>
    [Fact]
    public async Task AddWaitlistEntryAsync_ValidEntry_AddsToWaitlist()
    {
        // Arrange
        var service = CreateService();
        var createDto = new CreateWaitlistEntryDto
        {
            PartyName = "Smith Party",
            PartySize = 3,
            PhoneNumber = "555-0987",
            QuotedWaitInMinutes = 20
        };

        // Act
        var result = await service.AddWaitlistEntryAsync(createDto);
        var activeWaitlist = (await service.GetWaitlistAsync()).ToList();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Smith Party", result.PartyName);
        Assert.Equal("waiting", result.Status);
        Assert.Contains(activeWaitlist, w => w.Id == result.Id);
    }

    #endregion

    #region Server Rotation Tests
    
    /// <summary>
    /// Verifies that seating a table automatically rotates its section to the back of the queue.
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task SeatTableAsync_SeatingTable_RotatesSectionToBackOfQueue()
    {
        // Arrange
        var service = CreateService();

        // Before seating: Section 1 is next up (Sarah K. is in Section 1, David M. in Section 2, Kelly R. in Section 3)
        var initialRotation = await service.GetRotationAsync();
        Assert.Equal("srv-101", initialRotation.NextServerUp?.Id); // Section 1 is first

        var seatDto = new SeatTableDto
        {
            PartySize = 2,
            ServerId = "srv-101"
        };

        // Act
        // Seat Table 13, whci is in "Section 1" (active servers = 3)
        await service.SeatTableAsync("13", seatDto);
        var postSeatRotation = await service.GetRotationAsync();

        // Assert
        // Section 1 should move to the back of the queue, making Section 2 (David M.) next up
        Assert.Equal("srv-102", postSeatRotation.NextServerUp?.Id);
        Assert.Equal("srv-101", postSeatRotation.Queue.Last().Id); // Section 1 is now last
    }

    /// <summary>
    /// Verifies that skipping rotation shifts the next-up section to the back of the queue.
    /// </summary>
    [Fact]
    public async Task SkipRotationAsync_QueueWithSections_RotatesFirstToLast()
    {
        // Arrange
        var service = CreateService();
        var initialRotation = await service.GetRotationAsync();
        Assert.Equal("srv-101", initialRotation.NextServerUp?.Id); // Section 1 is next up

        // Act
        await service.SkipRotationAsync();
        var postSkipRotation = await service.GetRotationAsync();

        // Assert
        Assert.Equal("srv-102", postSkipRotation.NextServerUp?.Id);  // Section 2 is now next up
        Assert.Equal("srv-101", postSkipRotation.Queue.Last().Id); // Section 1 is moved to back
    }


    /// <summary>
    /// Varifies clocking in a server assigned to an already active section throws a validation exception.
    /// </summary>
     [Fact]
    public async Task ClockInServerAsync_DuplicateActiveSection_ThrowsBusinessValidationException()
    {
        // Arrange
        var service = CreateService();
        var clockInDto = new ClockInServerDto
        {
            Name = "John D.",
            Section = "Section 1" // Section 1 is already assigned to server Sarah K.
        };
        // Act & Assert
        var exception = await Assert.ThrowsAsync<BusinessValidationException>(() =>
            service.ClockInServerAsync(clockInDto));
    }

    /// <summary>
    /// Verifies clocking out a server on a 4-server shift reassigns the remaining 3 servers
    /// and auto-transfers any active seated tables to the new section owners.
    /// </summary>
    [Fact]
    public async Task ClockOutServerAsync_4to3Servers_ReassignsSectionsAndTransfersSeatedTables()
    {
        // Arrange
        var service = CreateService();

        // 1. Clock in a 4th server to create a 4-server shift
        var fourthServer = await service.ClockInServerAsync(new ClockInServerDto
        {
            Name = "John S.",
            Section = "Section 4"
        });

        // 2. Seat Table 13 under David M. (srv-102, Section 2 in 4-server shift)
        await service.SeatTableAsync("13", new SeatTableDto
        {
            PartySize = 2,
            ServerId = "srv-102",
            WaitlistId = null
        });

        // 3. Clock out David M. (srv-102), shifting remaining servers to Section 1, 2, and 3
        var clockOutDto = new ClockOutServerDto
        {
            ServerId = "srv-102",
            Reassignments = new List<ServerReassignmentDto>
            {
                new() { ServerId = "srv-101", NewSection = "Section 1" }, // Sarah
                new() { ServerId = "srv-103", NewSection = "Section 2" }, // Kelly
                new() { ServerId = fourthServer.Id, NewSection = "Section 3" } // John
            }
        };

        // Act
        await service.ClockOutServerAsync(clockOutDto);

        // Assert
        var servers = (await service.GetServersAsync()).ToList();
        var tables = (await service.GetTablesAsync()).ToList();

        // David is now inactive
        var davidActive = servers.FirstOrDefault(s => s.Id == "srv-102" && s.Active);
        Assert.Null(davidActive);

        // Sarah is assigned to Section 1
        var sarah = servers.First(s => s.Id == "srv-101");
        Assert.Equal("Section 1", sarah.Section);

        // Kelly is assigned to Section 2
        var kelly = servers.First(s => s.Id == "srv-103");
        Assert.Equal("Section 2", kelly.Section);

        // Table 13 (which was in Section 2 under 4 servers) is now in Section 1 under 3 servers.
        // Therefore, it should have been automatically transferred to Sarah K. (srv-101)
        var table13 = tables.First(t => t.Id == "13");
        Assert.Equal("srv-101", table13.ServerId);
        Assert.Equal("Sarah K.", table13.ServerName);
    }

    /// <summary>
    /// Verifies clocking out a server on a 2-server shift automatically collapses the layout
    /// to "Main Floor" for the single remaining server, and transfers all active seated tables.
    /// </summary>
    [Fact]
    public async Task ClockOutServerAsync_2to1Server_CollapsesToMainFloorAndTransfersTables()
    {
        // Arrange
        var service = CreateService();

        // 1. Clock out Kelly (srv-103) to go from 3 servers down to a 2-server shift (Front/Back)
        await service.ClockOutServerAsync(new ClockOutServerDto
        {
            ServerId = "srv-103",
            Reassignments = new List<ServerReassignmentDto>
            {
                new() { ServerId = "srv-101", NewSection = "Front" }, // Sarah
                new() { ServerId = "srv-102", NewSection = "Back" }   // David
            }
        });

        // 2. Seat Table 11 under Sarah (srv-101, Front)
        await service.ClearTableAsync("11"); // Clear default seeded table 11
        await service.SeatTableAsync("11", new SeatTableDto
        {
            PartySize = 2,
            ServerId = "srv-101",
            WaitlistId = null
        });

        // 3. Seat Table 23 under David (srv-102, Back)
        await service.SeatTableAsync("23", new SeatTableDto
        {
            PartySize = 2,
            ServerId = "srv-102",
            WaitlistId = null
        });

        // 4. Clock out David (srv-102). Since only 1 server (Sarah) remains, she inherits all tables under "Main Floor"
        var clockOutDto = new ClockOutServerDto
        {
            ServerId = "srv-102",
            Reassignments = new List<ServerReassignmentDto>() // Ignored for 1-server transition
        };

        // Act
        await service.ClockOutServerAsync(clockOutDto);

        // Assert
        var servers = (await service.GetServersAsync()).ToList();
        var tables = (await service.GetTablesAsync()).ToList();

        // Only Sarah remains active, and she is assigned to "Main Floor"
        var activeServers = servers.Where(s => s.Active).ToList();
        Assert.Single(activeServers);
        var sarah = activeServers.First();
        Assert.Equal("srv-101", sarah.Id);
        Assert.Equal("Main Floor", sarah.Section);

        // Both Table 11 and Table 23 should be assigned to Sarah K.
        var table11 = tables.First(t => t.Id == "11");
        var table23 = tables.First(t => t.Id == "23");

        Assert.Equal("srv-101", table11.ServerId);
        Assert.Equal("Sarah K.", table11.ServerName);
        Assert.Equal("Main Floor", table11.CurrentServerSection);

        Assert.Equal("srv-101", table23.ServerId);
        Assert.Equal("Sarah K.", table23.ServerName);
        Assert.Equal("Main Floor", table23.CurrentServerSection);
    }

    /// <summary>
    /// Verifies that ResetShiftAsync clocks out all servers, archives waitlists, empties rotation queue, and resets all tables to available.
    /// </summary>
    [Fact]
    public async Task ResetShiftAsync_ClearsAllRostersTablesAndWaitlists()
    {
        // Arrange
        var service = CreateService();

        // Act
        await service.ResetShiftAsync();

        // Assert
        var servers = (await service.GetServersAsync()).ToList();
        var tables = (await service.GetTablesAsync()).ToList();
        var waitlist = (await service.GetWaitlistAsync()).ToList();
        var rotation = await service.GetRotationAsync();

        // 1. Verify no active servers
        Assert.Empty(servers);

        // 2. Verify all 17 tables are available and cleared
        Assert.Equal(17, tables.Count);
        Assert.All(tables, t =>
        {
            Assert.Equal("available", t.Status);
            Assert.Null(t.PartyName);
            Assert.Null(t.PartySize);
            Assert.Null(t.SeatedAt);
            Assert.Null(t.ServerId);
            Assert.Null(t.ServerName);
            Assert.Equal(string.Empty, t.CurrentServerSection);
        });

        // 3. Verify waitlist is empty (all active entries are completed/archived)
        Assert.Empty(waitlist);

        // 4. Verify rotation queue is empty
        Assert.Null(rotation.NextServerUp);
        Assert.Empty(rotation.Queue);
    }
    
    #endregion
}