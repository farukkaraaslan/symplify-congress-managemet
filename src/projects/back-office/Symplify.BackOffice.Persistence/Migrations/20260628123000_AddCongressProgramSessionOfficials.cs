using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Symplify.BackOffice.Persistence.Contexts;

#nullable disable

namespace Symplify.BackOffice.Persistence.Migrations;

[DbContext(typeof(BackOfficeDbContext))]
[Migration("20260628123000_AddCongressProgramSessionOfficials")]
public partial class AddCongressProgramSessionOfficials : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "ChairAuthorId",
            table: "CongressProgramSessions",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "ViceChairAuthorId",
            table: "CongressProgramSessions",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_CongressProgramSessions_ChairAuthorId",
            table: "CongressProgramSessions",
            column: "ChairAuthorId");

        migrationBuilder.CreateIndex(
            name: "IX_CongressProgramSessions_ViceChairAuthorId",
            table: "CongressProgramSessions",
            column: "ViceChairAuthorId");

        migrationBuilder.AddForeignKey(
            name: "FK_CongressProgramSessions_Authors_ChairAuthorId",
            table: "CongressProgramSessions",
            column: "ChairAuthorId",
            principalTable: "Authors",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);

        migrationBuilder.AddForeignKey(
            name: "FK_CongressProgramSessions_Authors_ViceChairAuthorId",
            table: "CongressProgramSessions",
            column: "ViceChairAuthorId",
            principalTable: "Authors",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_CongressProgramSessions_Authors_ChairAuthorId",
            table: "CongressProgramSessions");

        migrationBuilder.DropForeignKey(
            name: "FK_CongressProgramSessions_Authors_ViceChairAuthorId",
            table: "CongressProgramSessions");

        migrationBuilder.DropIndex(
            name: "IX_CongressProgramSessions_ChairAuthorId",
            table: "CongressProgramSessions");

        migrationBuilder.DropIndex(
            name: "IX_CongressProgramSessions_ViceChairAuthorId",
            table: "CongressProgramSessions");

        migrationBuilder.DropColumn(
            name: "ChairAuthorId",
            table: "CongressProgramSessions");

        migrationBuilder.DropColumn(
            name: "ViceChairAuthorId",
            table: "CongressProgramSessions");
    }
}
