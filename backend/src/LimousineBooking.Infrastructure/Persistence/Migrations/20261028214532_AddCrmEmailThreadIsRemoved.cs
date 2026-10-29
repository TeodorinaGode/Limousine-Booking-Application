using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LimousineBooking.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCrmEmailThreadIsRemoved : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRemoved",
                table: "CrmEmailThreads",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_CrmEmailThreads_IsRemoved",
                table: "CrmEmailThreads",
                column: "IsRemoved");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CrmEmailThreads_IsRemoved",
                table: "CrmEmailThreads");

            migrationBuilder.DropColumn(
                name: "IsRemoved",
                table: "CrmEmailThreads");
        }
    }
}
