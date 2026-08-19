using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LimousineBooking.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingAutomaticAssignmentFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bookings_VehicleId",
                table: "Bookings");

            migrationBuilder.AddColumn<string>(
                name: "AssignmentType",
                table: "Bookings",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ManualAssignmentReason",
                table: "Bookings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresManualAssignment",
                table: "Bookings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_RequiresManualAssignment",
                table: "Bookings",
                column: "RequiresManualAssignment");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_VehicleId_TravelDate",
                table: "Bookings",
                columns: new[] { "VehicleId", "TravelDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bookings_RequiresManualAssignment",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_VehicleId_TravelDate",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "AssignmentType",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ManualAssignmentReason",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "RequiresManualAssignment",
                table: "Bookings");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_VehicleId",
                table: "Bookings",
                column: "VehicleId");
        }
    }
}
