using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LimousineBooking.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Locations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CountryCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Locations", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Locations",
                columns: new[] { "Id", "CountryCode", "CreatedAt", "Description", "IsActive", "Latitude", "Longitude", "Name", "SortOrder", "Type", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000001"), "CH", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Major Swiss city", true, 47.559600000000003, 7.5885999999999996, "Basel", 1, "City", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("10000000-0000-0000-0000-000000000002"), "CH", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Major Swiss city", true, 47.376899999999999, 8.5417000000000005, "Zurich", 2, "City", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("10000000-0000-0000-0000-000000000003"), "CH", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Swiss capital", true, 46.948, 7.4474, "Bern", 3, "City", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("10000000-0000-0000-0000-000000000004"), "CH", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Major Swiss city", true, 46.2044, 6.1432000000000002, "Geneva", 4, "City", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("10000000-0000-0000-0000-000000000005"), "CH", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "International airport", true, 47.464700000000001, 8.5492000000000008, "Zurich Airport", 5, "Airport", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("10000000-0000-0000-0000-000000000006"), "CH", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "International airport", true, 47.589599999999997, 7.5298999999999996, "Basel Airport", 6, "Airport", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("10000000-0000-0000-0000-000000000007"), "CH", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "International airport", true, 46.238100000000003, 6.1089000000000002, "Geneva Airport", 7, "Airport", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("10000000-0000-0000-0000-000000000008"), "IT", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Nearby European destination", true, 45.464199999999998, 9.1899999999999995, "Milan", 8, "Destination", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("10000000-0000-0000-0000-000000000009"), "DE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Nearby European destination", true, 48.135100000000001, 11.582000000000001, "Munich", 9, "Destination", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("10000000-0000-0000-0000-000000000010"), "DE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Nearby European destination", true, 48.775799999999997, 9.1829000000000001, "Stuttgart", 10, "Destination", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("10000000-0000-0000-0000-000000000011"), "DE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Nearby European destination", true, 50.110900000000001, 8.6821000000000002, "Frankfurt", 11, "Destination", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("10000000-0000-0000-0000-000000000012"), "FR", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Nearby European destination", true, 48.8566, 2.3521999999999998, "Paris", 12, "Destination", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("10000000-0000-0000-0000-000000000013"), "LU", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Nearby European destination", true, 49.611600000000003, 6.1318999999999999, "Luxembourg", 13, "Destination", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("10000000-0000-0000-0000-000000000014"), "BE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Nearby European destination", true, 50.850299999999997, 4.3517000000000001, "Brussels", 14, "Destination", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("10000000-0000-0000-0000-000000000015"), "AT", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Nearby European destination", true, 47.269199999999998, 11.4041, "Innsbruck", 15, "Destination", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Locations_IsActive",
                table: "Locations",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Locations");
        }
    }
}
