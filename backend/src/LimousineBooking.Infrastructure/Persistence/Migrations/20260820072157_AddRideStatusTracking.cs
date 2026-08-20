using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LimousineBooking.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRideStatusTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RideStatus",
                table: "Bookings",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Upcoming");

            migrationBuilder.CreateTable(
                name: "RideStatusHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: false),
                    PreviousStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    NewStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ChangedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RideStatusHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RideStatusHistories_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_RideStatus",
                table: "Bookings",
                column: "RideStatus");

            migrationBuilder.CreateIndex(
                name: "IX_RideStatusHistories_BookingId_ChangedAt",
                table: "RideStatusHistories",
                columns: new[] { "BookingId", "ChangedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RideStatusHistories");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_RideStatus",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "RideStatus",
                table: "Bookings");
        }
    }
}
