using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Symplify.BackOffice.Persistence.Contexts;

#nullable disable

namespace Symplify.BackOffice.Persistence.Migrations;

[DbContext(typeof(BackOfficeDbContext))]
[Migration("20260730161000_AddCongressMailConfigurations")]
public partial class AddCongressMailConfigurations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "FromEmail",
            table: "MailOutboxMessages",
            type: "character varying(250)",
            maxLength: 250,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "FromName",
            table: "MailOutboxMessages",
            type: "character varying(250)",
            maxLength: 250,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ReplyToEmail",
            table: "MailOutboxMessages",
            type: "character varying(250)",
            maxLength: 250,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ReplyToName",
            table: "MailOutboxMessages",
            type: "character varying(250)",
            maxLength: 250,
            nullable: true);

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
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "CongressMailConfigurations");

        migrationBuilder.DropColumn(name: "FromEmail", table: "MailOutboxMessages");
        migrationBuilder.DropColumn(name: "FromName", table: "MailOutboxMessages");
        migrationBuilder.DropColumn(name: "ReplyToEmail", table: "MailOutboxMessages");
        migrationBuilder.DropColumn(name: "ReplyToName", table: "MailOutboxMessages");
    }
}
