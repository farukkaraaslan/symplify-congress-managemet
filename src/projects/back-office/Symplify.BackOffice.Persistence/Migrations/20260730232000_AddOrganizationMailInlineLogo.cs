using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Symplify.BackOffice.Persistence.Contexts;

#nullable disable

namespace Symplify.BackOffice.Persistence.Migrations;

[DbContext(typeof(BackOfficeDbContext))]
[Migration("20260730232000_AddOrganizationMailInlineLogo")]
public partial class AddOrganizationMailInlineLogo : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "MailLogoBucketName",
            table: "OrganizationMailConfigurations",
            type: "character varying(150)",
            maxLength: 150,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "MailLogoContentType",
            table: "OrganizationMailConfigurations",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "MailLogoFileName",
            table: "OrganizationMailConfigurations",
            type: "character varying(255)",
            maxLength: 255,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "MailLogoObjectName",
            table: "OrganizationMailConfigurations",
            type: "character varying(1000)",
            maxLength: 1000,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "MailLogoBucketName",
            table: "OrganizationMailConfigurations");

        migrationBuilder.DropColumn(
            name: "MailLogoContentType",
            table: "OrganizationMailConfigurations");

        migrationBuilder.DropColumn(
            name: "MailLogoFileName",
            table: "OrganizationMailConfigurations");

        migrationBuilder.DropColumn(
            name: "MailLogoObjectName",
            table: "OrganizationMailConfigurations");
    }
}
