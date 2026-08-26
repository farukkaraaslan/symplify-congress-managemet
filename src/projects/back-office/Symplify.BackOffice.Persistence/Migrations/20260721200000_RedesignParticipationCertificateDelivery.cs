using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Symplify.BackOffice.Persistence.Contexts;

#nullable disable

namespace Symplify.BackOffice.Persistence.Migrations;

[DbContext(typeof(BackOfficeDbContext))]
[Migration("20260721200000_RedesignParticipationCertificateDelivery")]
public partial class RedesignParticipationCertificateDelivery : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "MailSubject",
            table: "ParticipationCertificateTemplates",
            type: "character varying(300)",
            maxLength: 300,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "MailTitle",
            table: "ParticipationCertificateTemplates",
            type: "character varying(300)",
            maxLength: 300,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "MailBodyHtml",
            table: "ParticipationCertificateTemplates",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "PublicId",
            table: "ParticipationCertificates",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "PublicAccessTokenHash",
            table: "ParticipationCertificates",
            type: "character varying(128)",
            maxLength: 128,
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "PublishedAt",
            table: "ParticipationCertificates",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "RevokedAt",
            table: "ParticipationCertificates",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "RevokedByUserId",
            table: "ParticipationCertificates",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "RevocationReason",
            table: "ParticipationCertificates",
            type: "character varying(1000)",
            maxLength: 1000,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "CandidateSearch",
            table: "ParticipationCertificateGenerationJobs",
            type: "character varying(500)",
            maxLength: 500,
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "SelectAllFiltered",
            table: "ParticipationCertificateGenerationJobs",
            type: "boolean",
            nullable: false,
            defaultValue: true);

        migrationBuilder.AddColumn<string>(
            name: "SelectedCandidateKeysJson",
            table: "ParticipationCertificateGenerationJobs",
            type: "text",
            nullable: false,
            defaultValue: "[]");

        migrationBuilder.AddColumn<Guid>(
            name: "ParticipationCertificateId",
            table: "MailOutboxMessages",
            type: "uuid",
            nullable: true);

        migrationBuilder.Sql("DROP INDEX IF EXISTS public.\"IX_ParticipationCertificates_Congress_Submission_Author\";");
        migrationBuilder.Sql("DROP INDEX IF EXISTS public.\"IX_ParticipationCertificates_Congress_Submission_Author_Culture\";");
        migrationBuilder.Sql("DROP INDEX IF EXISTS public.\"IX_ParticipationCertificates_CongressId_SubmissionId_AuthorId_Culture\";");

        migrationBuilder.CreateIndex(
            name: "IX_ParticipationCertificates_CongressId_SubmissionId_AuthorId_Culture",
            table: "ParticipationCertificates",
            columns: new[] { "CongressId", "SubmissionId", "AuthorId", "Culture" },
            unique: true,
            filter: "\"DeletedDate\" IS NULL");

        migrationBuilder.CreateIndex(
            name: "IX_ParticipationCertificates_PublicId",
            table: "ParticipationCertificates",
            column: "PublicId",
            unique: true,
            filter: "\"PublicId\" IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_ParticipationCertificates_CongressId_PublishedAt_RevokedAt",
            table: "ParticipationCertificates",
            columns: new[] { "CongressId", "PublishedAt", "RevokedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_MailOutboxMessages_ParticipationCertificateId",
            table: "MailOutboxMessages",
            column: "ParticipationCertificateId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_ParticipationCertificates_CongressId_SubmissionId_AuthorId_Culture",
            table: "ParticipationCertificates");

        migrationBuilder.DropIndex(
            name: "IX_ParticipationCertificates_PublicId",
            table: "ParticipationCertificates");

        migrationBuilder.DropIndex(
            name: "IX_ParticipationCertificates_CongressId_PublishedAt_RevokedAt",
            table: "ParticipationCertificates");

        migrationBuilder.DropIndex(
            name: "IX_MailOutboxMessages_ParticipationCertificateId",
            table: "MailOutboxMessages");

        migrationBuilder.DropColumn(name: "MailSubject", table: "ParticipationCertificateTemplates");
        migrationBuilder.DropColumn(name: "MailTitle", table: "ParticipationCertificateTemplates");
        migrationBuilder.DropColumn(name: "MailBodyHtml", table: "ParticipationCertificateTemplates");
        migrationBuilder.DropColumn(name: "PublicId", table: "ParticipationCertificates");
        migrationBuilder.DropColumn(name: "PublicAccessTokenHash", table: "ParticipationCertificates");
        migrationBuilder.DropColumn(name: "PublishedAt", table: "ParticipationCertificates");
        migrationBuilder.DropColumn(name: "RevokedAt", table: "ParticipationCertificates");
        migrationBuilder.DropColumn(name: "RevokedByUserId", table: "ParticipationCertificates");
        migrationBuilder.DropColumn(name: "RevocationReason", table: "ParticipationCertificates");
        migrationBuilder.DropColumn(name: "CandidateSearch", table: "ParticipationCertificateGenerationJobs");
        migrationBuilder.DropColumn(name: "SelectAllFiltered", table: "ParticipationCertificateGenerationJobs");
        migrationBuilder.DropColumn(name: "SelectedCandidateKeysJson", table: "ParticipationCertificateGenerationJobs");
        migrationBuilder.DropColumn(name: "ParticipationCertificateId", table: "MailOutboxMessages");
    }
}
