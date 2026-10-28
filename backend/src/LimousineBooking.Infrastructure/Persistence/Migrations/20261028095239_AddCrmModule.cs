using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LimousineBooking.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCrmModule : Migration
    {
        // Fixed ids/timestamp so this seed is reproducible across environments
        // (every `dotnet ef database update` run creates the identical default
        // pipeline row rather than a fresh random one each time).
        private static readonly Guid DefaultPipelineId = new("9f6a1b1e-9b0a-4e0a-8a7d-000000000001");
        private static readonly Guid StageNewLeadId = new("9f6a1b1e-9b0a-4e0a-8a7d-000000000011");
        private static readonly Guid StageContactedId = new("9f6a1b1e-9b0a-4e0a-8a7d-000000000012");
        private static readonly Guid StageQualifiedId = new("9f6a1b1e-9b0a-4e0a-8a7d-000000000013");
        private static readonly Guid StageNeedsAnalysisId = new("9f6a1b1e-9b0a-4e0a-8a7d-000000000014");
        private static readonly Guid StageProposalSentId = new("9f6a1b1e-9b0a-4e0a-8a7d-000000000015");
        private static readonly Guid StageNegotiationId = new("9f6a1b1e-9b0a-4e0a-8a7d-000000000016");
        private static readonly Guid StageVerbalCommitmentId = new("9f6a1b1e-9b0a-4e0a-8a7d-000000000017");
        private static readonly Guid StageClosedWonId = new("9f6a1b1e-9b0a-4e0a-8a7d-000000000018");
        private static readonly Guid StageClosedLostId = new("9f6a1b1e-9b0a-4e0a-8a7d-000000000019");
        private static readonly DateTime SeedTimestamp = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CrmCompanies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CompanyType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Website = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Domain = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Industry = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Address = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Priority = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AssignedOwnerId = table.Column<Guid>(type: "uuid", nullable: true),
                    LeadSource = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LastContactedAt = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    NextFollowUpAt = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    Notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrmCompanies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CrmCompanies_Users_AssignedOwnerId",
                        column: x => x.AssignedOwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CrmContacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    JobTitle = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    PreferredContactMethod = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    AssignedOwnerId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastContactedAt = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    NextFollowUpAt = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    Notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrmContacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CrmContacts_Users_AssignedOwnerId",
                        column: x => x.AssignedOwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CrmPipelines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrmPipelines", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "CrmPipelines",
                columns: new[] { "Id", "Name", "IsDefault", "CreatedAt", "UpdatedAt" },
                values: new object[] { DefaultPipelineId, "Sales Pipeline", true, SeedTimestamp, SeedTimestamp });

            migrationBuilder.CreateTable(
                name: "CrmCompanyContacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContactId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrmCompanyContacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CrmCompanyContacts_CrmCompanies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "CrmCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CrmCompanyContacts_CrmContacts_ContactId",
                        column: x => x.ContactId,
                        principalTable: "CrmContacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CrmPipelineStages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PipelineId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsWon = table.Column<bool>(type: "boolean", nullable: false),
                    IsLost = table.Column<bool>(type: "boolean", nullable: false),
                    DefaultProbability = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrmPipelineStages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CrmPipelineStages_CrmPipelines_PipelineId",
                        column: x => x.PipelineId,
                        principalTable: "CrmPipelines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "CrmPipelineStages",
                columns: new[] { "Id", "PipelineId", "Name", "SortOrder", "IsWon", "IsLost", "DefaultProbability", "CreatedAt", "UpdatedAt" },
                values: new object[,]
                {
                    { StageNewLeadId, DefaultPipelineId, "New Lead", 1, false, false, 10, SeedTimestamp, SeedTimestamp },
                    { StageContactedId, DefaultPipelineId, "Contacted", 2, false, false, 20, SeedTimestamp, SeedTimestamp },
                    { StageQualifiedId, DefaultPipelineId, "Qualified", 3, false, false, 35, SeedTimestamp, SeedTimestamp },
                    { StageNeedsAnalysisId, DefaultPipelineId, "Needs Analysis", 4, false, false, 45, SeedTimestamp, SeedTimestamp },
                    { StageProposalSentId, DefaultPipelineId, "Proposal Sent", 5, false, false, 60, SeedTimestamp, SeedTimestamp },
                    { StageNegotiationId, DefaultPipelineId, "Negotiation", 6, false, false, 75, SeedTimestamp, SeedTimestamp },
                    { StageVerbalCommitmentId, DefaultPipelineId, "Verbal Commitment", 7, false, false, 85, SeedTimestamp, SeedTimestamp },
                    { StageClosedWonId, DefaultPipelineId, "Closed Won", 8, true, false, 100, SeedTimestamp, SeedTimestamp },
                    { StageClosedLostId, DefaultPipelineId, "Closed Lost", 9, false, true, 0, SeedTimestamp, SeedTimestamp }
                });

            migrationBuilder.CreateTable(
                name: "CrmDeals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    DealType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Value = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    PipelineId = table.Column<Guid>(type: "uuid", nullable: false),
                    StageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Probability = table.Column<int>(type: "integer", nullable: false),
                    ExpectedCloseDate = table.Column<DateOnly>(type: "date", nullable: true),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: true),
                    NextAction = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    LossReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrmDeals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CrmDeals_CrmCompanies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "CrmCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CrmDeals_CrmPipelineStages_StageId",
                        column: x => x.StageId,
                        principalTable: "CrmPipelineStages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CrmDeals_CrmPipelines_PipelineId",
                        column: x => x.PipelineId,
                        principalTable: "CrmPipelines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CrmDeals_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CrmActivities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActivityType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Subject = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    ContactId = table.Column<Guid>(type: "uuid", nullable: true),
                    DealId = table.Column<Guid>(type: "uuid", nullable: true),
                    Outcome = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrmActivities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CrmActivities_CrmCompanies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "CrmCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CrmActivities_CrmContacts_ContactId",
                        column: x => x.ContactId,
                        principalTable: "CrmContacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CrmActivities_CrmDeals_DealId",
                        column: x => x.DealId,
                        principalTable: "CrmDeals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CrmActivities_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CrmDealContacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DealId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContactId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrmDealContacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CrmDealContacts_CrmContacts_ContactId",
                        column: x => x.ContactId,
                        principalTable: "CrmContacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CrmDealContacts_CrmDeals_DealId",
                        column: x => x.DealId,
                        principalTable: "CrmDeals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CrmTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DueTime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    AssignedUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Priority = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    ContactId = table.Column<Guid>(type: "uuid", nullable: true),
                    DealId = table.Column<Guid>(type: "uuid", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrmTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CrmTasks_CrmCompanies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "CrmCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CrmTasks_CrmContacts_ContactId",
                        column: x => x.ContactId,
                        principalTable: "CrmContacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CrmTasks_CrmDeals_DealId",
                        column: x => x.DealId,
                        principalTable: "CrmDeals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CrmTasks_Users_AssignedUserId",
                        column: x => x.AssignedUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CrmActivities_CompanyId",
                table: "CrmActivities",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_CrmActivities_ContactId",
                table: "CrmActivities",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_CrmActivities_DealId",
                table: "CrmActivities",
                column: "DealId");

            migrationBuilder.CreateIndex(
                name: "IX_CrmActivities_OccurredAt",
                table: "CrmActivities",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_CrmActivities_UserId",
                table: "CrmActivities",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CrmCompanies_AssignedOwnerId",
                table: "CrmCompanies",
                column: "AssignedOwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_CrmCompanies_CompanyType",
                table: "CrmCompanies",
                column: "CompanyType");

            migrationBuilder.CreateIndex(
                name: "IX_CrmCompanies_IsArchived",
                table: "CrmCompanies",
                column: "IsArchived");

            migrationBuilder.CreateIndex(
                name: "IX_CrmCompanies_NextFollowUpAt",
                table: "CrmCompanies",
                column: "NextFollowUpAt");

            migrationBuilder.CreateIndex(
                name: "IX_CrmCompanies_Status",
                table: "CrmCompanies",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CrmCompanyContacts_CompanyId_ContactId",
                table: "CrmCompanyContacts",
                columns: new[] { "CompanyId", "ContactId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CrmCompanyContacts_ContactId",
                table: "CrmCompanyContacts",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_CrmContacts_AssignedOwnerId",
                table: "CrmContacts",
                column: "AssignedOwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_CrmContacts_Email",
                table: "CrmContacts",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_CrmContacts_IsArchived",
                table: "CrmContacts",
                column: "IsArchived");

            migrationBuilder.CreateIndex(
                name: "IX_CrmContacts_Status",
                table: "CrmContacts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CrmDealContacts_ContactId",
                table: "CrmDealContacts",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_CrmDealContacts_DealId_ContactId",
                table: "CrmDealContacts",
                columns: new[] { "DealId", "ContactId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CrmDeals_CompanyId",
                table: "CrmDeals",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_CrmDeals_ExpectedCloseDate",
                table: "CrmDeals",
                column: "ExpectedCloseDate");

            migrationBuilder.CreateIndex(
                name: "IX_CrmDeals_IsArchived",
                table: "CrmDeals",
                column: "IsArchived");

            migrationBuilder.CreateIndex(
                name: "IX_CrmDeals_OwnerId",
                table: "CrmDeals",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_CrmDeals_PipelineId",
                table: "CrmDeals",
                column: "PipelineId");

            migrationBuilder.CreateIndex(
                name: "IX_CrmDeals_StageId",
                table: "CrmDeals",
                column: "StageId");

            migrationBuilder.CreateIndex(
                name: "IX_CrmPipelineStages_PipelineId_SortOrder",
                table: "CrmPipelineStages",
                columns: new[] { "PipelineId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_CrmTasks_AssignedUserId",
                table: "CrmTasks",
                column: "AssignedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CrmTasks_CompanyId",
                table: "CrmTasks",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_CrmTasks_ContactId",
                table: "CrmTasks",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_CrmTasks_DealId",
                table: "CrmTasks",
                column: "DealId");

            migrationBuilder.CreateIndex(
                name: "IX_CrmTasks_DueDate",
                table: "CrmTasks",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_CrmTasks_Status",
                table: "CrmTasks",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CrmActivities");

            migrationBuilder.DropTable(
                name: "CrmCompanyContacts");

            migrationBuilder.DropTable(
                name: "CrmDealContacts");

            migrationBuilder.DropTable(
                name: "CrmTasks");

            migrationBuilder.DropTable(
                name: "CrmContacts");

            migrationBuilder.DropTable(
                name: "CrmDeals");

            migrationBuilder.DropTable(
                name: "CrmCompanies");

            migrationBuilder.DropTable(
                name: "CrmPipelineStages");

            migrationBuilder.DropTable(
                name: "CrmPipelines");
        }
    }
}
