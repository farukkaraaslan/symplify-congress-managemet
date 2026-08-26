using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Symplify.BackOffice.Persistence.Contexts;

#nullable disable

namespace Symplify.BackOffice.Persistence.Migrations;

[DbContext(typeof(BackOfficeDbContext))]
[Migration("20260721010000_AddParticipationCertificateGenerationJobs")]
public partial class AddParticipationCertificateGenerationJobs : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ParticipationCertificateGenerationJobs",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CongressId = table.Column<Guid>(type: "uuid", nullable: false),
                Culture = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                SubmissionStatusCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                PaymentStatusCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                ExcludedCandidateKeysJson = table.Column<string>(type: "text", nullable: false),
                ExcludedCount = table.Column<int>(type: "integer", nullable: false),
                Status = table.Column<int>(type: "integer", nullable: false),
                TotalCount = table.Column<int>(type: "integer", nullable: false),
                ProcessedCount = table.Column<int>(type: "integer", nullable: false),
                SucceededCount = table.Column<int>(type: "integer", nullable: false),
                FailedCount = table.Column<int>(type: "integer", nullable: false),
                SkippedCount = table.Column<int>(type: "integer", nullable: false),
                StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                MaterializedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                HeartbeatAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                LastError = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedBy = table.Column<string>(type: "text", nullable: true),
                UpdatedBy = table.Column<string>(type: "text", nullable: true),
                DeletedBy = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ParticipationCertificateGenerationJobs", x => x.Id);
                table.ForeignKey(
                    name: "FK_ParticipationCertificateGenerationJobs_Congresses_CongressId",
                    column: x => x.CongressId,
                    principalTable: "Congresses",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ParticipationCertificateGenerationJobItems",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                JobId = table.Column<Guid>(type: "uuid", nullable: false),
                SubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                SubmissionNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                SubmissionTitle = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                SubmissionTypeName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                AuthorDisplayName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                AuthorEmail = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                AuthorInstitution = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                IsVideoPresentation = table.Column<bool>(type: "boolean", nullable: false),
                Status = table.Column<int>(type: "integer", nullable: false),
                AttemptCount = table.Column<int>(type: "integer", nullable: false),
                StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                LastError = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                CertificateId = table.Column<Guid>(type: "uuid", nullable: true),
                CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedBy = table.Column<string>(type: "text", nullable: true),
                UpdatedBy = table.Column<string>(type: "text", nullable: true),
                DeletedBy = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ParticipationCertificateGenerationJobItems", x => x.Id);
                table.ForeignKey(
                    name: "FK_ParticipationCertificateGenerationJobItems_ParticipationCertificateGenerationJobs_JobId",
                    column: x => x.JobId,
                    principalTable: "ParticipationCertificateGenerationJobs",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(name: "IX_ParticipationCertificateGenerationJobs_CongressId_Culture_Status", table: "ParticipationCertificateGenerationJobs", columns: new[] { "CongressId", "Culture", "Status" });
        migrationBuilder.CreateIndex(name: "IX_ParticipationCertificateGenerationJobs_Status_CreatedDate", table: "ParticipationCertificateGenerationJobs", columns: new[] { "Status", "CreatedDate" });
        migrationBuilder.CreateIndex(name: "IX_ParticipationCertificateGenerationJobItems_JobId_Status_Id", table: "ParticipationCertificateGenerationJobItems", columns: new[] { "JobId", "Status", "Id" });
        migrationBuilder.CreateIndex(name: "IX_ParticipationCertificateGenerationJobItems_JobId_SubmissionId_AuthorId", table: "ParticipationCertificateGenerationJobItems", columns: new[] { "JobId", "SubmissionId", "AuthorId" }, unique: true);
        migrationBuilder.CreateIndex(name: "UX_ParticipationCertificateGenerationJobs_Active", table: "ParticipationCertificateGenerationJobs", columns: new[] { "CongressId", "Culture" }, unique: true, filter: "\"DeletedDate\" IS NULL AND \"Status\" IN (1, 2, 3, 7)");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ParticipationCertificateGenerationJobItems");
        migrationBuilder.DropTable(name: "ParticipationCertificateGenerationJobs");
    }
}
