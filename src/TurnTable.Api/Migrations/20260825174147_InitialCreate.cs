using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TurnTable.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RotationQueue",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Section = table.Column<string>(type: "text", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RotationQueue", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Servers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    Section = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Servers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tables",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    TypeOrShape = table.Column<string>(type: "text", nullable: false),
                    MaxSeats = table.Column<int>(type: "integer", nullable: false),
                    CurrentServerSection = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    PartyName = table.Column<string>(type: "text", nullable: true),
                    PartySize = table.Column<int>(type: "integer", nullable: true),
                    ServerId = table.Column<string>(type: "text", nullable: true),
                    SeatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tables", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WaitlistEntries",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    PartyName = table.Column<string>(type: "text", nullable: false),
                    PartySize = table.Column<int>(type: "integer", nullable: false),
                    PhoneNumber = table.Column<string>(type: "text", nullable: false),
                    CheckedInAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    QuotedWaitInMinutes = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WaitlistEntries", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Tables",
                columns: new[] { "Id", "CurrentServerSection", "MaxSeats", "PartyName", "PartySize", "SeatedAt", "ServerId", "Status", "TypeOrShape" },
                values: new object[,]
                {
                    { "11", "", 4, null, null, null, null, "available", "rectangle" },
                    { "12", "", 4, null, null, null, null, "available", "rectangle" },
                    { "13", "", 2, null, null, null, null, "available", "square" },
                    { "15", "", 4, null, null, null, null, "available", "rectangle" },
                    { "16", "", 4, null, null, null, null, "available", "rectangle" },
                    { "21", "", 6, null, null, null, null, "available", "rectangle" },
                    { "22A", "", 2, null, null, null, null, "available", "square" },
                    { "22B", "", 2, null, null, null, null, "available", "square" },
                    { "23", "", 5, null, null, null, null, "available", "circle" },
                    { "25", "", 4, null, null, null, null, "available", "rectangle" },
                    { "31", "", 6, null, null, null, null, "available", "rectangle" },
                    { "32", "", 5, null, null, null, null, "available", "circle" },
                    { "33", "", 2, null, null, null, null, "available", "square" },
                    { "35", "", 2, null, null, null, null, "available", "square" },
                    { "51", "", 4, null, null, null, null, "available", "rectangle" },
                    { "52", "", 4, null, null, null, null, "available", "rectangle" },
                    { "53", "", 4, null, null, null, null, "available", "rectangle" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RotationQueue");

            migrationBuilder.DropTable(
                name: "Servers");

            migrationBuilder.DropTable(
                name: "Tables");

            migrationBuilder.DropTable(
                name: "WaitlistEntries");
        }
    }
}
