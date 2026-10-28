using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LimousineBooking.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCrmEmailIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "EmailMessageId",
                table: "CrmActivities",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CrmEmailTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    BodyHtml = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrmEmailTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CrmEmailTemplates_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmailProviderConnections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    EmailAddress = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EncryptedRefreshToken = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    AccessToken = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    AccessTokenExpiresAt = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    LastSyncedAt = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    LastSyncError = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailProviderConnections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailProviderConnections_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CrmEmailThreads",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderThreadId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    LastMessageAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    HasUnread = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrmEmailThreads", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CrmEmailThreads_EmailProviderConnections_ConnectionId",
                        column: x => x.ConnectionId,
                        principalTable: "EmailProviderConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmailSendOperations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientOperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    GeneratedMessageIdHeader = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    ProviderMessageId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ToAddressesJson = table.Column<string>(type: "jsonb", nullable: false),
                    CcAddressesJson = table.Column<string>(type: "jsonb", nullable: true),
                    BccAddressesJson = table.Column<string>(type: "jsonb", nullable: true),
                    Subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    BodyHtml = table.Column<string>(type: "text", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: true),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    ContactId = table.Column<Guid>(type: "uuid", nullable: true),
                    DealId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailSendOperations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailSendOperations_CrmCompanies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "CrmCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmailSendOperations_CrmContacts_ContactId",
                        column: x => x.ContactId,
                        principalTable: "CrmContacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmailSendOperations_CrmDeals_DealId",
                        column: x => x.DealId,
                        principalTable: "CrmDeals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmailSendOperations_CrmEmailTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "CrmEmailTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmailSendOperations_EmailProviderConnections_ConnectionId",
                        column: x => x.ConnectionId,
                        principalTable: "EmailProviderConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmailSendOperations_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmailSyncCheckpoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderCursor = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsRunning = table.Column<bool>(type: "boolean", nullable: false),
                    RunStartedAt = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    LastRunStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    LastRunAt = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    LastError = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    MessagesSyncedLastRun = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailSyncCheckpoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailSyncCheckpoints_EmailProviderConnections_ConnectionId",
                        column: x => x.ConnectionId,
                        principalTable: "EmailProviderConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CrmEmailThreadCompanies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ThreadId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrmEmailThreadCompanies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CrmEmailThreadCompanies_CrmCompanies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "CrmCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CrmEmailThreadCompanies_CrmEmailThreads_ThreadId",
                        column: x => x.ThreadId,
                        principalTable: "CrmEmailThreads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CrmEmailThreadContacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ThreadId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContactId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrmEmailThreadContacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CrmEmailThreadContacts_CrmContacts_ContactId",
                        column: x => x.ContactId,
                        principalTable: "CrmContacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CrmEmailThreadContacts_CrmEmailThreads_ThreadId",
                        column: x => x.ThreadId,
                        principalTable: "CrmEmailThreads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CrmEmailThreadDeals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ThreadId = table.Column<Guid>(type: "uuid", nullable: false),
                    DealId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrmEmailThreadDeals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CrmEmailThreadDeals_CrmDeals_DealId",
                        column: x => x.DealId,
                        principalTable: "CrmDeals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CrmEmailThreadDeals_CrmEmailThreads_ThreadId",
                        column: x => x.ThreadId,
                        principalTable: "CrmEmailThreads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CrmEmailMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ThreadId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderMessageId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Direction = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FromAddress = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    FromName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ToAddressesJson = table.Column<string>(type: "jsonb", nullable: false),
                    CcAddressesJson = table.Column<string>(type: "jsonb", nullable: true),
                    BccAddressesJson = table.Column<string>(type: "jsonb", nullable: true),
                    Subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    BodyHtml = table.Column<string>(type: "text", nullable: true),
                    BodyPlainText = table.Column<string>(type: "text", nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    SendOperationId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrmEmailMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CrmEmailMessages_CrmEmailThreads_ThreadId",
                        column: x => x.ThreadId,
                        principalTable: "CrmEmailThreads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CrmEmailMessages_EmailProviderConnections_ConnectionId",
                        column: x => x.ConnectionId,
                        principalTable: "EmailProviderConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CrmEmailMessages_EmailSendOperations_SendOperationId",
                        column: x => x.SendOperationId,
                        principalTable: "EmailSendOperations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CrmEmailAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    ProviderAttachmentId = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrmEmailAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CrmEmailAttachments_CrmEmailMessages_MessageId",
                        column: x => x.MessageId,
                        principalTable: "CrmEmailMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CrmActivities_EmailMessageId",
                table: "CrmActivities",
                column: "EmailMessageId",
                unique: true,
                filter: "\"EmailMessageId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CrmEmailAttachments_MessageId",
                table: "CrmEmailAttachments",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_CrmEmailMessages_ConnectionId_ProviderMessageId",
                table: "CrmEmailMessages",
                columns: new[] { "ConnectionId", "ProviderMessageId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CrmEmailMessages_OccurredAt",
                table: "CrmEmailMessages",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_CrmEmailMessages_SendOperationId",
                table: "CrmEmailMessages",
                column: "SendOperationId");

            migrationBuilder.CreateIndex(
                name: "IX_CrmEmailMessages_ThreadId",
                table: "CrmEmailMessages",
                column: "ThreadId");

            migrationBuilder.CreateIndex(
                name: "IX_CrmEmailTemplates_Category",
                table: "CrmEmailTemplates",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_CrmEmailTemplates_CreatedByUserId",
                table: "CrmEmailTemplates",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CrmEmailTemplates_IsArchived",
                table: "CrmEmailTemplates",
                column: "IsArchived");

            migrationBuilder.CreateIndex(
                name: "IX_CrmEmailThreadCompanies_CompanyId",
                table: "CrmEmailThreadCompanies",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_CrmEmailThreadCompanies_ThreadId_CompanyId",
                table: "CrmEmailThreadCompanies",
                columns: new[] { "ThreadId", "CompanyId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CrmEmailThreadContacts_ContactId",
                table: "CrmEmailThreadContacts",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_CrmEmailThreadContacts_ThreadId_ContactId",
                table: "CrmEmailThreadContacts",
                columns: new[] { "ThreadId", "ContactId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CrmEmailThreadDeals_DealId",
                table: "CrmEmailThreadDeals",
                column: "DealId");

            migrationBuilder.CreateIndex(
                name: "IX_CrmEmailThreadDeals_ThreadId_DealId",
                table: "CrmEmailThreadDeals",
                columns: new[] { "ThreadId", "DealId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CrmEmailThreads_ConnectionId_ProviderThreadId",
                table: "CrmEmailThreads",
                columns: new[] { "ConnectionId", "ProviderThreadId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CrmEmailThreads_LastMessageAt",
                table: "CrmEmailThreads",
                column: "LastMessageAt");

            migrationBuilder.CreateIndex(
                name: "IX_EmailProviderConnections_CreatedByUserId",
                table: "EmailProviderConnections",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailProviderConnections_Status",
                table: "EmailProviderConnections",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_EmailSendOperations_ClientOperationId",
                table: "EmailSendOperations",
                column: "ClientOperationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailSendOperations_CompanyId",
                table: "EmailSendOperations",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailSendOperations_ConnectionId",
                table: "EmailSendOperations",
                column: "ConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailSendOperations_ContactId",
                table: "EmailSendOperations",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailSendOperations_CreatedByUserId",
                table: "EmailSendOperations",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailSendOperations_DealId",
                table: "EmailSendOperations",
                column: "DealId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailSendOperations_TemplateId",
                table: "EmailSendOperations",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailSyncCheckpoints_ConnectionId",
                table: "EmailSyncCheckpoints",
                column: "ConnectionId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CrmActivities_CrmEmailMessages_EmailMessageId",
                table: "CrmActivities",
                column: "EmailMessageId",
                principalTable: "CrmEmailMessages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CrmActivities_CrmEmailMessages_EmailMessageId",
                table: "CrmActivities");

            migrationBuilder.DropTable(
                name: "CrmEmailAttachments");

            migrationBuilder.DropTable(
                name: "CrmEmailThreadCompanies");

            migrationBuilder.DropTable(
                name: "CrmEmailThreadContacts");

            migrationBuilder.DropTable(
                name: "CrmEmailThreadDeals");

            migrationBuilder.DropTable(
                name: "EmailSyncCheckpoints");

            migrationBuilder.DropTable(
                name: "CrmEmailMessages");

            migrationBuilder.DropTable(
                name: "CrmEmailThreads");

            migrationBuilder.DropTable(
                name: "EmailSendOperations");

            migrationBuilder.DropTable(
                name: "CrmEmailTemplates");

            migrationBuilder.DropTable(
                name: "EmailProviderConnections");

            migrationBuilder.DropIndex(
                name: "IX_CrmActivities_EmailMessageId",
                table: "CrmActivities");

            migrationBuilder.DropColumn(
                name: "EmailMessageId",
                table: "CrmActivities");
        }
    }
}
