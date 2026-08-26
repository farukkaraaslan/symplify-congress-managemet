using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Symplify.BackOffice.Persistence.Contexts;

#nullable disable

namespace Symplify.BackOffice.Persistence.Migrations;

[DbContext(typeof(BackOfficeDbContext))]
[Migration("20260621190000_AddAutomatedCongressProgram")]
public partial class AddAutomatedCongressProgram : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "CongressProgramPlans",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CongressId = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Status = table.Column<int>(type: "integer", nullable: false),
                VersionNo = table.Column<int>(type: "integer", nullable: false),
                DefaultPresentationDurationMinutes = table.Column<int>(type: "integer", nullable: false),
                DefaultSessionDurationMinutes = table.Column<int>(type: "integer", nullable: false),
                DefaultQuestionAnswerDurationMinutes = table.Column<int>(type: "integer", nullable: false),
                DefaultBreakDurationMinutes = table.Column<int>(type: "integer", nullable: false),
                LastGeneratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                LastGeneratedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedBy = table.Column<string>(type: "text", nullable: true),
                UpdatedBy = table.Column<string>(type: "text", nullable: true),
                DeletedBy = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CongressProgramPlans", x => x.Id);
                table.ForeignKey(
                    name: "FK_CongressProgramPlans_Congresses_CongressId",
                    column: x => x.CongressId,
                    principalTable: "Congresses",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "CongressProgramDays",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ProgramPlanId = table.Column<Guid>(type: "uuid", nullable: false),
                Date = table.Column<DateOnly>(type: "date", nullable: false),
                StartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                EndTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                Order = table.Column<int>(type: "integer", nullable: false),
                CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedBy = table.Column<string>(type: "text", nullable: true),
                UpdatedBy = table.Column<string>(type: "text", nullable: true),
                DeletedBy = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CongressProgramDays", x => x.Id);
                table.ForeignKey(
                    name: "FK_CongressProgramDays_CongressProgramPlans_ProgramPlanId",
                    column: x => x.ProgramPlanId,
                    principalTable: "CongressProgramPlans",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "CongressProgramFixedBlocks",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ProgramDayId = table.Column<Guid>(type: "uuid", nullable: false),
                EventRoomId = table.Column<Guid>(type: "uuid", nullable: true),
                BlockType = table.Column<int>(type: "integer", nullable: false),
                Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                StartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                EndTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                Order = table.Column<int>(type: "integer", nullable: false),
                IsLocked = table.Column<bool>(type: "boolean", nullable: false),
                CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedBy = table.Column<string>(type: "text", nullable: true),
                UpdatedBy = table.Column<string>(type: "text", nullable: true),
                DeletedBy = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CongressProgramFixedBlocks", x => x.Id);
                table.ForeignKey(
                    name: "FK_CongressProgramFixedBlocks_CongressProgramDays_ProgramDayId",
                    column: x => x.ProgramDayId,
                    principalTable: "CongressProgramDays",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_CongressProgramFixedBlocks_EventRooms_EventRoomId",
                    column: x => x.EventRoomId,
                    principalTable: "EventRooms",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "CongressProgramSessions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ProgramDayId = table.Column<Guid>(type: "uuid", nullable: false),
                EventRoomId = table.Column<Guid>(type: "uuid", nullable: false),
                Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                StartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                EndTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                QuestionAnswerDurationMinutes = table.Column<int>(type: "integer", nullable: false),
                Order = table.Column<int>(type: "integer", nullable: false),
                IsLocked = table.Column<bool>(type: "boolean", nullable: false),
                CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedBy = table.Column<string>(type: "text", nullable: true),
                UpdatedBy = table.Column<string>(type: "text", nullable: true),
                DeletedBy = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CongressProgramSessions", x => x.Id);
                table.ForeignKey(
                    name: "FK_CongressProgramSessions_CongressProgramDays_ProgramDayId",
                    column: x => x.ProgramDayId,
                    principalTable: "CongressProgramDays",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_CongressProgramSessions_EventRooms_EventRoomId",
                    column: x => x.EventRoomId,
                    principalTable: "EventRooms",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "CongressProgramItems",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ProgramSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                SubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                Order = table.Column<int>(type: "integer", nullable: false),
                DurationMinutes = table.Column<int>(type: "integer", nullable: false),
                IsLocked = table.Column<bool>(type: "boolean", nullable: false),
                Source = table.Column<int>(type: "integer", nullable: false),
                CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedBy = table.Column<string>(type: "text", nullable: true),
                UpdatedBy = table.Column<string>(type: "text", nullable: true),
                DeletedBy = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CongressProgramItems", x => x.Id);
                table.ForeignKey(
                    name: "FK_CongressProgramItems_CongressProgramSessions_ProgramSessionId",
                    column: x => x.ProgramSessionId,
                    principalTable: "CongressProgramSessions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_CongressProgramItems_Submissions_SubmissionId",
                    column: x => x.SubmissionId,
                    principalTable: "Submissions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_CongressProgramPlans_CongressId",
            table: "CongressProgramPlans",
            column: "CongressId",
            unique: true,
            filter: "\"DeletedDate\" IS NULL");

        migrationBuilder.CreateIndex(
            name: "IX_CongressProgramDays_ProgramPlanId_Date",
            table: "CongressProgramDays",
            columns: new[] { "ProgramPlanId", "Date" },
            unique: true,
            filter: "\"DeletedDate\" IS NULL");

        migrationBuilder.CreateIndex(
            name: "IX_CongressProgramFixedBlocks_EventRoomId",
            table: "CongressProgramFixedBlocks",
            column: "EventRoomId");

        migrationBuilder.CreateIndex(
            name: "IX_CongressProgramFixedBlocks_ProgramDayId_EventRoomId_StartTime",
            table: "CongressProgramFixedBlocks",
            columns: new[] { "ProgramDayId", "EventRoomId", "StartTime" });

        migrationBuilder.CreateIndex(
            name: "IX_CongressProgramSessions_EventRoomId",
            table: "CongressProgramSessions",
            column: "EventRoomId");

        migrationBuilder.CreateIndex(
            name: "IX_CongressProgramSessions_ProgramDayId_EventRoomId_StartTime",
            table: "CongressProgramSessions",
            columns: new[] { "ProgramDayId", "EventRoomId", "StartTime" });

        migrationBuilder.CreateIndex(
            name: "IX_CongressProgramItems_ProgramSessionId_Order",
            table: "CongressProgramItems",
            columns: new[] { "ProgramSessionId", "Order" });

        migrationBuilder.CreateIndex(
            name: "IX_CongressProgramItems_SubmissionId",
            table: "CongressProgramItems",
            column: "SubmissionId",
            unique: true,
            filter: "\"DeletedDate\" IS NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "CongressProgramFixedBlocks");
        migrationBuilder.DropTable(name: "CongressProgramItems");
        migrationBuilder.DropTable(name: "CongressProgramSessions");
        migrationBuilder.DropTable(name: "CongressProgramDays");
        migrationBuilder.DropTable(name: "CongressProgramPlans");
    }
}
