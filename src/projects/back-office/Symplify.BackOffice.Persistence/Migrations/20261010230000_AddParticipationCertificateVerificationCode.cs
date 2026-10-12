using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Symplify.BackOffice.Persistence.Contexts;

#nullable disable

namespace Symplify.BackOffice.Persistence.Migrations;

[DbContext(typeof(BackOfficeDbContext))]
[Migration("20261010230000_AddParticipationCertificateVerificationCode")]
public partial class AddParticipationCertificateVerificationCode : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "VerificationCode",
            table: "ParticipationCertificates",
            type: "character varying(120)",
            maxLength: 120,
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_ParticipationCertificates_VerificationCode",
            table: "ParticipationCertificates",
            column: "VerificationCode",
            unique: true,
            filter: "\"VerificationCode\" IS NOT NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_ParticipationCertificates_VerificationCode",
            table: "ParticipationCertificates");

        migrationBuilder.DropColumn(
            name: "VerificationCode",
            table: "ParticipationCertificates");
    }
}
