using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LimousineBooking.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class WidenCrmEmailAttachmentProviderAttachmentId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ProviderAttachmentId",
                table: "CrmEmailAttachments",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(300)",
                oldMaxLength: 300);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ProviderAttachmentId",
                table: "CrmEmailAttachments",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
