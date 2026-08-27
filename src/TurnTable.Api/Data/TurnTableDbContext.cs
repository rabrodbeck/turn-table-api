using Microsoft.EntityFrameworkCore;
using TurnTable.Api.Models.Domain;

namespace TurnTable.Api.Data;

/// <summary>
/// Entity Framework DbContext for persisting the restaurant host stand data to PostgreSQL.
/// </summary>
public class TurnTableDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TurnTableDbContext"/> class.
    /// </summary>
    /// <param name="options"></param>
    public TurnTableDbContext(DbContextOptions<TurnTableDbContext> options) : base(options)
    {            
    }

    public DbSet<Table> Tables { get; set; } = null!;
    public DbSet<Server> Servers { get; set; } = null!;
    public DbSet<WaitlistEntry> WaitlistEntries { get; set; } = null!;
    public DbSet<RotationQueueItem> RotationQueue { get; set; } = null!;
    public DbSet<Reservation> Reservations { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 1. Configure Primary Keys
        modelBuilder.Entity<Table>().HasKey(t => t.Id);
        modelBuilder.Entity<Server>().HasKey(s => s.Id);
        modelBuilder.Entity<WaitlistEntry>().HasKey(w => w.Id);
        modelBuilder.Entity<RotationQueueItem>().HasKey(r => r.Id);
        modelBuilder.Entity<Reservation>().HasKey(r => r.Id);

        // 2. Seed Static Table Metadata (Empty/Available layout)
        modelBuilder.Entity<Table>().HasData(
            new Table { Id = "11", TypeOrShape = "rectangle", MaxSeats = 4, Status = "available", CurrentServerSection = "" },
            new Table { Id = "21", TypeOrShape = "rectangle", MaxSeats = 6, Status = "available", CurrentServerSection = "" },
            new Table { Id = "31", TypeOrShape = "rectangle", MaxSeats = 6, Status = "available", CurrentServerSection = "" },
            new Table { Id = "51", TypeOrShape = "rectangle", MaxSeats = 4, Status = "available", CurrentServerSection = "" },
            new Table { Id = "12", TypeOrShape = "rectangle", MaxSeats = 4, Status = "available", CurrentServerSection = "" },
            new Table { Id = "22A", TypeOrShape = "square", MaxSeats = 2, Status = "available", CurrentServerSection = "" },
            new Table { Id = "22B", TypeOrShape = "square", MaxSeats = 2, Status = "available", CurrentServerSection = "" },
            new Table { Id = "32", TypeOrShape = "circle", MaxSeats = 5, Status = "available", CurrentServerSection = "" },
            new Table { Id = "52", TypeOrShape = "rectangle", MaxSeats = 4, Status = "available", CurrentServerSection = "" },
            new Table { Id = "13", TypeOrShape = "square", MaxSeats = 2, Status = "available", CurrentServerSection = "" },
            new Table { Id = "23", TypeOrShape = "circle", MaxSeats = 5, Status = "available", CurrentServerSection = "" },
            new Table { Id = "33", TypeOrShape = "square", MaxSeats = 2, Status = "available", CurrentServerSection = "" },
            new Table { Id = "53", TypeOrShape = "rectangle", MaxSeats = 4, Status = "available", CurrentServerSection = "" },
            new Table { Id = "15", TypeOrShape = "rectangle", MaxSeats = 4, Status = "available", CurrentServerSection = "" },
            new Table { Id = "25", TypeOrShape = "rectangle", MaxSeats = 4, Status = "available", CurrentServerSection = "" },
            new Table { Id = "35", TypeOrShape = "square", MaxSeats = 2, Status = "available", CurrentServerSection = "" },
            new Table { Id = "16", TypeOrShape = "rectangle", MaxSeats = 4, Status = "available", CurrentServerSection = "" }
        );
    }
}