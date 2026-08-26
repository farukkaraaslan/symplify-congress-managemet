using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Symplify.BackOffice.Persistence.Contexts;

#nullable disable

namespace Symplify.BackOffice.Persistence.Migrations;

[DbContext(typeof(BackOfficeDbContext))]
[Migration("20260730173000_MoveMailConfigurationToOrganization")]
public partial class MoveMailConfigurationToOrganization : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "OrganizationId",
            table: "MailOutboxMessages",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "OrganizationMailConfigurations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                Host = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                Port = table.Column<int>(type: "integer", nullable: false, defaultValue: 587),
                EnableSsl = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                Username = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                PasswordCipherText = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                FromEmail = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                FromName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                ReplyToEmail = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                ReplyToName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                LastTestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                LastTestSucceeded = table.Column<bool>(type: "boolean", nullable: true),
                LastTestError = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                DeletedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_OrganizationMailConfigurations", x => x.Id);
                table.ForeignKey(
                    name: "FK_OrganizationMailConfigurations_Organizations_OrganizationId",
                    column: x => x.OrganizationId,
                    principalTable: "Organizations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_OrganizationMailConfigurations_OrganizationId",
            table: "OrganizationMailConfigurations",
            column: "OrganizationId",
            unique: true,
            filter: "\"DeletedDate\" IS NULL");

        migrationBuilder.AddCheckConstraint(
            name: "CK_OrganizationMailConfigurations_Port",
            table: "OrganizationMailConfigurations",
            sql: "\"Port\" BETWEEN 1 AND 65535");

        // When more than one congress of the same organization has a configuration,
        // keep the newest active record as the organization-level configuration.
        migrationBuilder.Sql(
            """
            INSERT INTO "OrganizationMailConfigurations"
            (
                "Id", "OrganizationId", "Host", "Port", "EnableSsl", "Username",
                "PasswordCipherText", "FromEmail", "FromName", "ReplyToEmail", "ReplyToName",
                "IsActive", "LastTestedAt", "LastTestSucceeded", "LastTestError",
                "CreatedDate", "UpdatedDate", "DeletedDate", "CreatedBy", "UpdatedBy", "DeletedBy"
            )
            SELECT DISTINCT ON (c."OrganizationId")
                cmc."Id", c."OrganizationId", cmc."Host", cmc."Port", cmc."EnableSsl", cmc."Username",
                cmc."PasswordCipherText", cmc."FromEmail", cmc."FromName", cmc."ReplyToEmail", cmc."ReplyToName",
                cmc."IsActive", cmc."LastTestedAt", cmc."LastTestSucceeded", cmc."LastTestError",
                cmc."CreatedDate", cmc."UpdatedDate", cmc."DeletedDate", cmc."CreatedBy", cmc."UpdatedBy", cmc."DeletedBy"
            FROM "CongressMailConfigurations" cmc
            INNER JOIN "Congresses" c ON c."Id" = cmc."CongressId"
            WHERE c."DeletedDate" IS NULL AND cmc."DeletedDate" IS NULL
            ORDER BY
                c."OrganizationId",
                cmc."IsActive" DESC,
                COALESCE(cmc."UpdatedDate", cmc."CreatedDate") DESC,
                cmc."CreatedDate" DESC;
            """);

        migrationBuilder.Sql(
            """
            UPDATE "MailOutboxMessages" AS message
            SET "OrganizationId" = congress."OrganizationId"
            FROM "Congresses" AS congress
            WHERE message."OrganizationId" IS NULL
              AND message."CongressId" = congress."Id";
            """);

        migrationBuilder.DropTable(name: "CongressMailConfigurations");

        migrationBuilder.CreateIndex(
            name: "IX_MailOutboxMessages_OrganizationId_CreatedDate",
            table: "MailOutboxMessages",
            columns: new[] { "OrganizationId", "CreatedDate" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_MailOutboxMessages_OrganizationId_CreatedDate",
            table: "MailOutboxMessages");

        migrationBuilder.CreateTable(
            name: "CongressMailConfigurations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CongressId = table.Column<Guid>(type: "uuid", nullable: false),
                Host = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                Port = table.Column<int>(type: "integer", nullable: false, defaultValue: 587),
                EnableSsl = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                Username = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                PasswordCipherText = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                FromEmail = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                FromName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                ReplyToEmail = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                ReplyToName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                LastTestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                LastTestSucceeded = table.Column<bool>(type: "boolean", nullable: true),
                LastTestError = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                DeletedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CongressMailConfigurations", x => x.Id);
                table.ForeignKey(
                    name: "FK_CongressMailConfigurations_Congresses_CongressId",
                    column: x => x.CongressId,
                    principalTable: "Congresses",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_CongressMailConfigurations_CongressId",
            table: "CongressMailConfigurations",
            column: "CongressId",
            unique: true,
            filter: "\"DeletedDate\" IS NULL");

        migrationBuilder.AddCheckConstraint(
            name: "CK_CongressMailConfigurations_Port",
            table: "CongressMailConfigurations",
            sql: "\"Port\" BETWEEN 1 AND 65535");

        migrationBuilder.Sql(
            """
            INSERT INTO "CongressMailConfigurations"
            (
                "Id", "CongressId", "Host", "Port", "EnableSsl", "Username",
                "PasswordCipherText", "FromEmail", "FromName", "ReplyToEmail", "ReplyToName",
                "IsActive", "LastTestedAt", "LastTestSucceeded", "LastTestError",
                "CreatedDate", "UpdatedDate", "DeletedDate", "CreatedBy", "UpdatedBy", "DeletedBy"
            )
            SELECT
                c."Id", c."Id", omc."Host", omc."Port", omc."EnableSsl", omc."Username",
                omc."PasswordCipherText", omc."FromEmail", omc."FromName", omc."ReplyToEmail", omc."ReplyToName",
                omc."IsActive", omc."LastTestedAt", omc."LastTestSucceeded", omc."LastTestError",
                omc."CreatedDate", omc."UpdatedDate", omc."DeletedDate", omc."CreatedBy", omc."UpdatedBy", omc."DeletedBy"
            FROM "OrganizationMailConfigurations" omc
            INNER JOIN "Congresses" c ON c."OrganizationId" = omc."OrganizationId"
            WHERE c."DeletedDate" IS NULL AND omc."DeletedDate" IS NULL;
            """);

        migrationBuilder.DropTable(name: "OrganizationMailConfigurations");

        migrationBuilder.DropColumn(
            name: "OrganizationId",
            table: "MailOutboxMessages");
    }
}
