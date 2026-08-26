using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Symplify.BackOffice.Persistence.Contexts;

#nullable disable

namespace Symplify.BackOffice.Persistence.Migrations;

[DbContext(typeof(BackOfficeDbContext))]
[Migration("20260628150000_AddCongressProgramSessionBoardOfficials")]
public partial class AddCongressProgramSessionBoardOfficials : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "ChairBoardMemberId",
            table: "CongressProgramSessions",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "ViceChairBoardMemberId",
            table: "CongressProgramSessions",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_CongressProgramSessions_ChairBoardMemberId",
            table: "CongressProgramSessions",
            column: "ChairBoardMemberId");

        migrationBuilder.CreateIndex(
            name: "IX_CongressProgramSessions_ViceChairBoardMemberId",
            table: "CongressProgramSessions",
            column: "ViceChairBoardMemberId");

        migrationBuilder.AddForeignKey(
            name: "FK_CongressProgramSessions_CongressBoardMembers_ChairBoardMemberId",
            table: "CongressProgramSessions",
            column: "ChairBoardMemberId",
            principalTable: "CongressBoardMembers",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);

        migrationBuilder.AddForeignKey(
            name: "FK_CongressProgramSessions_CongressBoardMembers_ViceChairBoardMemberId",
            table: "CongressProgramSessions",
            column: "ViceChairBoardMemberId",
            principalTable: "CongressBoardMembers",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_CongressProgramSessions_CongressBoardMembers_ChairBoardMemberId",
            table: "CongressProgramSessions");

        migrationBuilder.DropForeignKey(
            name: "FK_CongressProgramSessions_CongressBoardMembers_ViceChairBoardMemberId",
            table: "CongressProgramSessions");

        migrationBuilder.DropIndex(
            name: "IX_CongressProgramSessions_ChairBoardMemberId",
            table: "CongressProgramSessions");

        migrationBuilder.DropIndex(
            name: "IX_CongressProgramSessions_ViceChairBoardMemberId",
            table: "CongressProgramSessions");

        migrationBuilder.DropColumn(
            name: "ChairBoardMemberId",
            table: "CongressProgramSessions");

        migrationBuilder.DropColumn(
            name: "ViceChairBoardMemberId",
            table: "CongressProgramSessions");
    }
}
