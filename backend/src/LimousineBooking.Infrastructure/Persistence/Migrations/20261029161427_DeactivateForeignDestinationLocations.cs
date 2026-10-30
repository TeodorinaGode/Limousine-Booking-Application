using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LimousineBooking.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DeactivateForeignDestinationLocations : Migration
    {
        private static readonly Guid[] LocationIds =
        {
            new("10000000-0000-0000-0000-000000000008"), // Milan
            new("10000000-0000-0000-0000-000000000009"), // Munich
            new("10000000-0000-0000-0000-000000000010"), // Stuttgart
            new("10000000-0000-0000-0000-000000000011"), // Frankfurt
            new("10000000-0000-0000-0000-000000000012"), // Paris
            new("10000000-0000-0000-0000-000000000013"), // Luxembourg
            new("10000000-0000-0000-0000-000000000014"), // Brussels
            new("10000000-0000-0000-0000-000000000015"), // Innsbruck
        };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var id in LocationIds)
            {
                migrationBuilder.UpdateData(
                    table: "Locations",
                    keyColumn: "Id",
                    keyValue: id,
                    column: "IsActive",
                    value: false);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var id in LocationIds)
            {
                migrationBuilder.UpdateData(
                    table: "Locations",
                    keyColumn: "Id",
                    keyValue: id,
                    column: "IsActive",
                    value: true);
            }
        }
    }
}
