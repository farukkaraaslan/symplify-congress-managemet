using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Symplify.BackOffice.Persistence.Contexts;

#nullable disable

namespace Symplify.BackOffice.Persistence.Migrations;

[DbContext(typeof(BackOfficeDbContext))]
[Migration("20260630230000_AddProgramSubmissionFilterSnapshot")]
public partial class AddProgramSubmissionFilterSnapshot : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "SubmissionFilterJson",
            table: "CongressProgramPlans",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "EligibleSubmissionIdsJson",
            table: "CongressProgramPlans",
            type: "text",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "SubmissionFilterJson",
            table: "CongressProgramPlans");

        migrationBuilder.DropColumn(
            name: "EligibleSubmissionIdsJson",
            table: "CongressProgramPlans");
    }
}
