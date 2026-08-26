using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Symplify.BackOffice.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Inital : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Countrys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: true),
                    PhoneCode = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_Countrys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DocumentTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EvaluationCriterions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false, defaultValue: 10),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluationCriterions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EvaluationScoreOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Value = table.Column<decimal>(type: "numeric(8,2)", nullable: false),
                    Label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluationScoreOptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EventRooms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventRooms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Languages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Culture = table.Column<string>(type: "text", nullable: false),
                    TwoLetterIsoCode = table.Column<string>(type: "text", nullable: true),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_Languages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MailOutboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ToEmail = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    ToName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    Subject = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    HtmlBody = table.Column<string>(type: "text", nullable: false),
                    AttachmentPath = table.Column<string>(type: "character varying(750)", maxLength: 750, nullable: true),
                    AttachmentBucketName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    AttachmentObjectName = table.Column<string>(type: "character varying(750)", maxLength: 750, nullable: true),
                    AttachmentFileName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: true),
                    AttachmentContentType = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    LastAttemptAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastError = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    RelatedSubmissionId = table.Column<Guid>(type: "uuid", nullable: true),
                    BulkEmailBatchId = table.Column<Guid>(type: "uuid", nullable: true),
                    CongressId = table.Column<Guid>(type: "uuid", nullable: true),
                    BulkEmailAudienceType = table.Column<int>(type: "integer", nullable: true),
                    BulkEmailCulture = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    TrackingToken = table.Column<Guid>(type: "uuid", nullable: true),
                    FirstOpenedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastOpenedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OpenCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MailOutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Organizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ShortName = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    WebsiteUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    HostUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ContactName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ContactTitle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ContactEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ContactPhone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ContactNote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    LogoLightPath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    LogoDarkPath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BrandColor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organizations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentStatuss",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentStatuss", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ResourceKeys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    KeyName = table.Column<string>(type: "text", nullable: false),
                    AreaName = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceKeys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShortLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    TargetType = table.Column<int>(type: "integer", nullable: false),
                    TargetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Culture = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClickCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    LastAccessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShortLinks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SubmissionTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    FormProfile = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubmissionTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Titles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Titles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Topics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Topics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TransactionStatusPhases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionStatusPhases", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Regions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CountryId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_Regions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Regions_Countrys_CountryId",
                        column: x => x.CountryId,
                        principalTable: "Countrys",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "States",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CountryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_States", x => x.Id);
                    table.ForeignKey(
                        name: "FK_States_Countrys_CountryId",
                        column: x => x.CountryId,
                        principalTable: "Countrys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CountryTranslations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CountryId = table.Column<Guid>(type: "uuid", nullable: false),
                    LanguageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CountryTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CountryTranslations_Countrys_CountryId",
                        column: x => x.CountryId,
                        principalTable: "Countrys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CountryTranslations_Languages_LanguageId",
                        column: x => x.LanguageId,
                        principalTable: "Languages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DocumentTypeTranslations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    LanguageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentTypeTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentTypeTranslations_DocumentTypes_DocumentTypeId",
                        column: x => x.DocumentTypeId,
                        principalTable: "DocumentTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DocumentTypeTranslations_Languages_LanguageId",
                        column: x => x.LanguageId,
                        principalTable: "Languages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EvaluationCriterionTranslations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EvaluationCriterionId = table.Column<Guid>(type: "uuid", nullable: false),
                    LanguageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluationCriterionTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EvaluationCriterionTranslations_EvaluationCriterions_Evalua~",
                        column: x => x.EvaluationCriterionId,
                        principalTable: "EvaluationCriterions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EvaluationCriterionTranslations_Languages_LanguageId",
                        column: x => x.LanguageId,
                        principalTable: "Languages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EventRoomTranslations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventRoomId = table.Column<Guid>(type: "uuid", nullable: false),
                    LanguageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventRoomTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventRoomTranslations_EventRooms_EventRoomId",
                        column: x => x.EventRoomId,
                        principalTable: "EventRooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventRoomTranslations_Languages_LanguageId",
                        column: x => x.LanguageId,
                        principalTable: "Languages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationApiKeys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Environment = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    KeyType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    KeyPrefix = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    KeyHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Scopes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false, defaultValue: ""),
                    AllowedIpAddresses = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    AllowedDomains = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastUsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationApiKeys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationApiKeys_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PaymentStatusTranslations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentStatusId = table.Column<int>(type: "integer", nullable: false),
                    LanguageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentStatusTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentStatusTranslations_Languages_LanguageId",
                        column: x => x.LanguageId,
                        principalTable: "Languages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PaymentStatusTranslations_PaymentStatuss_PaymentStatusId",
                        column: x => x.PaymentStatusId,
                        principalTable: "PaymentStatuss",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ResourceValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ResourceKeyId = table.Column<Guid>(type: "uuid", nullable: false),
                    LanguageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResourceValues_Languages_LanguageId",
                        column: x => x.LanguageId,
                        principalTable: "Languages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ResourceValues_ResourceKeys_ResourceKeyId",
                        column: x => x.ResourceKeyId,
                        principalTable: "ResourceKeys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ClaimValue = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Module = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoleClaims_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SubmissionTypeTranslations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmissionTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    LanguageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubmissionTypeTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubmissionTypeTranslations_Languages_LanguageId",
                        column: x => x.LanguageId,
                        principalTable: "Languages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SubmissionTypeTranslations_SubmissionTypes_SubmissionTypeId",
                        column: x => x.SubmissionTypeId,
                        principalTable: "SubmissionTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Authors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: true),
                    Institution = table.Column<string>(type: "text", nullable: true),
                    Orcid = table.Column<string>(type: "text", nullable: true),
                    TitleId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsCorrespondingAuthor = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Authors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Authors_Titles_TitleId",
                        column: x => x.TitleId,
                        principalTable: "Titles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TitleTranslations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TitleId = table.Column<Guid>(type: "uuid", nullable: false),
                    LanguageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TitleTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TitleTranslations_Languages_LanguageId",
                        column: x => x.LanguageId,
                        principalTable: "Languages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TitleTranslations_Titles_TitleId",
                        column: x => x.TitleId,
                        principalTable: "Titles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TopicTranslations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TopicId = table.Column<Guid>(type: "uuid", nullable: false),
                    LanguageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TopicTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TopicTranslations_Languages_LanguageId",
                        column: x => x.LanguageId,
                        principalTable: "Languages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TopicTranslations_Topics_TopicId",
                        column: x => x.TopicId,
                        principalTable: "Topics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TransactionStatuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TransactionStatusPhaseId = table.Column<int>(type: "integer", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    IsEditable = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    IsFinal = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionStatuses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransactionStatuses_TransactionStatusPhases_TransactionStat~",
                        column: x => x.TransactionStatusPhaseId,
                        principalTable: "TransactionStatusPhases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TransactionStatusPhaseTranslations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TransactionStatusPhaseId = table.Column<int>(type: "integer", nullable: false),
                    LanguageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionStatusPhaseTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransactionStatusPhaseTranslations_Languages_LanguageId",
                        column: x => x.LanguageId,
                        principalTable: "Languages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransactionStatusPhaseTranslations_TransactionStatusPhases_~",
                        column: x => x.TransactionStatusPhaseId,
                        principalTable: "TransactionStatusPhases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RegionTranslations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RegionId = table.Column<Guid>(type: "uuid", nullable: false),
                    LanguageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegionTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegionTranslations_Languages_LanguageId",
                        column: x => x.LanguageId,
                        principalTable: "Languages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RegionTranslations_Regions_RegionId",
                        column: x => x.RegionId,
                        principalTable: "Regions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Citys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StateId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_Citys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Citys_States_StateId",
                        column: x => x.StateId,
                        principalTable: "States",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StateTranslations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StateId = table.Column<Guid>(type: "uuid", nullable: false),
                    LanguageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StateTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StateTranslations_Languages_LanguageId",
                        column: x => x.LanguageId,
                        principalTable: "Languages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StateTranslations_States_StateId",
                        column: x => x.StateId,
                        principalTable: "States",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Surname = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TitleId = table.Column<Guid>(type: "uuid", nullable: true),
                    Institution = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    CountryId = table.Column<Guid>(type: "uuid", nullable: true),
                    StateId = table.Column<Guid>(type: "uuid", nullable: true),
                    Orcid = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ProfileImageObjectName = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsBlacklisted = table.Column<bool>(type: "boolean", nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Countrys_CountryId",
                        column: x => x.CountryId,
                        principalTable: "Countrys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Users_States_StateId",
                        column: x => x.StateId,
                        principalTable: "States",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Users_Titles_TitleId",
                        column: x => x.TitleId,
                        principalTable: "Titles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TransactionStatusTransitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FromStatusId = table.Column<int>(type: "integer", nullable: false),
                    ToStatusId = table.Column<int>(type: "integer", nullable: false),
                    IsAuto = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    PermissionKey = table.Column<string>(type: "text", nullable: true),
                    GuardKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ActionKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionStatusTransitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransactionStatusTransitions_TransactionStatuses_FromStatus~",
                        column: x => x.FromStatusId,
                        principalTable: "TransactionStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransactionStatusTransitions_TransactionStatuses_ToStatusId",
                        column: x => x.ToStatusId,
                        principalTable: "TransactionStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TransactionStatusTranslations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TransactionStatusId = table.Column<int>(type: "integer", nullable: false),
                    LanguageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionStatusTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransactionStatusTranslations_Languages_LanguageId",
                        column: x => x.LanguageId,
                        principalTable: "Languages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransactionStatusTranslations_TransactionStatuses_Transacti~",
                        column: x => x.TransactionStatusId,
                        principalTable: "TransactionStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    InitialTransactionStatusId = table.Column<int>(type: "integer", nullable: true),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowTemplates_TransactionStatuses_InitialTransactionSta~",
                        column: x => x.InitialTransactionStatusId,
                        principalTable: "TransactionStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CityTranslations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CityId = table.Column<Guid>(type: "uuid", nullable: false),
                    LanguageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CityTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CityTranslations_Citys_CityId",
                        column: x => x.CityId,
                        principalTable: "Citys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CityTranslations_Languages_LanguageId",
                        column: x => x.LanguageId,
                        principalTable: "Languages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Congresses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    EditionNumber = table.Column<int>(type: "integer", nullable: true),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ContactName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ContactTitle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ContactEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ContactPhone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ContactAddress = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    VenueName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    LogoLightPath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    LogoDarkPath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CountryId = table.Column<Guid>(type: "uuid", nullable: true),
                    CityId = table.Column<Guid>(type: "uuid", nullable: true),
                    StateId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Congresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Congresses_Citys_CityId",
                        column: x => x.CityId,
                        principalTable: "Citys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Congresses_Countrys_CountryId",
                        column: x => x.CountryId,
                        principalTable: "Countrys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Congresses_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Congresses_States_StateId",
                        column: x => x.StateId,
                        principalTable: "States",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Reviewers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reviewers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reviewers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserClaims_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ProviderKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_UserLogins_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserTokens",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LoginProvider = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_UserTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TransactionStatusTransitionTranslations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TransactionStatusTransitionId = table.Column<int>(type: "integer", nullable: false),
                    LanguageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionStatusTransitionTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransactionStatusTransitionTranslations_Languages_LanguageId",
                        column: x => x.LanguageId,
                        principalTable: "Languages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransactionStatusTransitionTranslations_TransactionStatusTr~",
                        column: x => x.TransactionStatusTransitionId,
                        principalTable: "TransactionStatusTransitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowTransitionConditions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TransactionStatusTransitionId = table.Column<int>(type: "integer", nullable: false),
                    Subject = table.Column<int>(type: "integer", nullable: false),
                    Field = table.Column<int>(type: "integer", nullable: false),
                    Operator = table.Column<int>(type: "integer", nullable: false),
                    ExpectedValue = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ExpectedValueSource = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FailureMessageResourceKey = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowTransitionConditions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowTransitionConditions_TransactionStatusTransitions_T~",
                        column: x => x.TransactionStatusTransitionId,
                        principalTable: "TransactionStatusTransitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowTransitionEffects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TransactionStatusTransitionId = table.Column<int>(type: "integer", nullable: false),
                    EffectType = table.Column<int>(type: "integer", nullable: false),
                    ParametersJson = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowTransitionEffects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowTransitionEffects_TransactionStatusTransitions_Tran~",
                        column: x => x.TransactionStatusTransitionId,
                        principalTable: "TransactionStatusTransitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowTemplateTransitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    TransactionStatusTransitionId = table.Column<int>(type: "integer", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    TransactionStatusTranslationId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowTemplateTransitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowTemplateTransitions_TransactionStatusTransitions_Tr~",
                        column: x => x.TransactionStatusTransitionId,
                        principalTable: "TransactionStatusTransitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkflowTemplateTransitions_TransactionStatusTranslations_T~",
                        column: x => x.TransactionStatusTranslationId,
                        principalTable: "TransactionStatusTranslations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WorkflowTemplateTransitions_WorkflowTemplates_WorkflowTempl~",
                        column: x => x.WorkflowTemplateId,
                        principalTable: "WorkflowTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowTemplateTranslations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    LanguageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowTemplateTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowTemplateTranslations_Languages_LanguageId",
                        column: x => x.LanguageId,
                        principalTable: "Languages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkflowTemplateTranslations_WorkflowTemplates_WorkflowTemp~",
                        column: x => x.WorkflowTemplateId,
                        principalTable: "WorkflowTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CongressAnnouncements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CongressId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PublishStartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PublishEndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsPinned = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ShowOnHomePage = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    ShowInTicker = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ExternalUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    AttachmentPath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CongressAnnouncements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CongressAnnouncements_Congresses_CongressId",
                        column: x => x.CongressId,
                        principalTable: "Congresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CongressBoards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CongressId = table.Column<Guid>(type: "uuid", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CongressBoards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CongressBoards_Congresses_CongressId",
                        column: x => x.CongressId,
                        principalTable: "Congresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CongressDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CongressId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentTypeId = table.Column<Guid>(type: "uuid", nullable: true),
                    FilePath = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    StorageProvider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    BucketName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ObjectName = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ContentType = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    FileExtension = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    FileSize = table.Column<long>(type: "bigint", nullable: true),
                    ETag = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CongressDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CongressDocuments_Congresses_CongressId",
                        column: x => x.CongressId,
                        principalTable: "Congresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CongressDocuments_DocumentTypes_DocumentTypeId",
                        column: x => x.DocumentTypeId,
                        principalTable: "DocumentTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CongressEvaluationCriterions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CongressId = table.Column<Guid>(type: "uuid", nullable: false),
                    EvaluationCriterionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CongressEvaluationCriterions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CongressEvaluationCriterions_Congresses_CongressId",
                        column: x => x.CongressId,
                        principalTable: "Congresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CongressEvaluationCriterions_EvaluationCriterions_Evaluatio~",
                        column: x => x.EvaluationCriterionId,
                        principalTable: "EvaluationCriterions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CongressImportantDates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CongressId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CongressImportantDates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CongressImportantDates_Congresses_CongressId",
                        column: x => x.CongressId,
                        principalTable: "Congresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CongressPaymentPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CongressId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    AudienceType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PaymentCategory = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ValidUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    IsPublicVisible = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CongressPaymentPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CongressPaymentPlans_Congresses_CongressId",
                        column: x => x.CongressId,
                        principalTable: "Congresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CongressSections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CongressId = table.Column<Guid>(type: "uuid", nullable: false),
                    BindingKey = table.Column<string>(type: "text", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CongressSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CongressSections_Congresses_CongressId",
                        column: x => x.CongressId,
                        principalTable: "Congresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CongressSliders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CongressId = table.Column<Guid>(type: "uuid", nullable: false),
                    ImagePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CongressSliders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CongressSliders_Congresses_CongressId",
                        column: x => x.CongressId,
                        principalTable: "Congresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CongressSubmissionTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CongressId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmissionTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CongressSubmissionTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CongressSubmissionTypes_Congresses_CongressId",
                        column: x => x.CongressId,
                        principalTable: "Congresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CongressSubmissionTypes_SubmissionTypes_SubmissionTypeId",
                        column: x => x.SubmissionTypeId,
                        principalTable: "SubmissionTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CongressTopics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CongressId = table.Column<Guid>(type: "uuid", nullable: false),
                    TopicId = table.Column<Guid>(type: "uuid", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CongressTopics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CongressTopics_Congresses_CongressId",
                        column: x => x.CongressId,
                        principalTable: "Congresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CongressTopics_Topics_TopicId",
                        column: x => x.TopicId,
                        principalTable: "Topics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CongressTranslations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CongressId = table.Column<Guid>(type: "uuid", nullable: false),
                    LanguageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Subtitle = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ShortDescription = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    WelcomeTitle = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    WelcomeContent = table.Column<string>(type: "text", nullable: true),
                    SeoTitle = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    SeoDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    LogoPath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CongressTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CongressTranslations_Congresses_CongressId",
                        column: x => x.CongressId,
                        principalTable: "Congresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CongressTranslations_Languages_LanguageId",
                        column: x => x.LanguageId,
                        principalTable: "Languages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CongressWorkflowSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CongressId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceWorkflowTemplateId = table.Column<Guid>(type: "uuid", nullable: true),
                    InitialTransactionStatusId = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CongressWorkflowSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CongressWorkflowSettings_Congresses_CongressId",
                        column: x => x.CongressId,
                        principalTable: "Congresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CongressWorkflowSettings_TransactionStatuses_InitialTransac~",
                        column: x => x.InitialTransactionStatusId,
                        principalTable: "TransactionStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CongressWorkflowSettings_WorkflowTemplates_SourceWorkflowTe~",
                        column: x => x.SourceWorkflowTemplateId,
                        principalTable: "WorkflowTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DefaultCongressId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationUsers_Congresses_DefaultCongressId",
                        column: x => x.DefaultCongressId,
                        principalTable: "Congresses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OrganizationUsers_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrganizationUsers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Submissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CongressId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmissionTypeId = table.Column<Guid>(type: "uuid", nullable: true),
                    TopicId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LanguageId = table.Column<Guid>(type: "uuid", nullable: true),
                    PaymentStatusId = table.Column<int>(type: "integer", nullable: true),
                    TransactionStatusId = table.Column<int>(type: "integer", nullable: true),
                    SubmissionNumber = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Orcid = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    TitleEn = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Abstract = table.Column<string>(type: "text", nullable: true),
                    AbstractEn = table.Column<string>(type: "text", nullable: true),
                    Keywords = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    KeywordsEn = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsSubmitted = table.Column<bool>(type: "boolean", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AppUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Submissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Submissions_Congresses_CongressId",
                        column: x => x.CongressId,
                        principalTable: "Congresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Submissions_Languages_LanguageId",
                        column: x => x.LanguageId,
                        principalTable: "Languages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Submissions_PaymentStatuss_PaymentStatusId",
                        column: x => x.PaymentStatusId,
                        principalTable: "PaymentStatuss",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Submissions_SubmissionTypes_SubmissionTypeId",
                        column: x => x.SubmissionTypeId,
                        principalTable: "SubmissionTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Submissions_Topics_TopicId",
                        column: x => x.TopicId,
                        principalTable: "Topics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Submissions_TransactionStatuses_TransactionStatusId",
                        column: x => x.TransactionStatusId,
                        principalTable: "TransactionStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Submissions_Users_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Submissions_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CongressReviewers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CongressId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewerId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    InvitedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AcceptedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RejectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpertiseKeywords = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CongressReviewers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CongressReviewers_Congresses_CongressId",
                        column: x => x.CongressId,
                        principalTable: "Congresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CongressReviewers_Reviewers_ReviewerId",
                        column: x => x.ReviewerId,
                        principalTable: "Reviewers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CongressTransactionStatusTransitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CongressId = table.Column<Guid>(type: "uuid", nullable: false),
                    TransactionStatusTransitionId = table.Column<int>(type: "integer", nullable: false),
                    SourceWorkflowTemplateTransitionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CongressTransactionStatusTransitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CongressTransactionStatusTransitions_Congresses_CongressId",
                        column: x => x.CongressId,
                        principalTable: "Congresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CongressTransactionStatusTransitions_TransactionStatusTrans~",
                        column: x => x.TransactionStatusTransitionId,
                        principalTable: "TransactionStatusTransitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CongressTransactionStatusTransitions_WorkflowTemplateTransi~",
                        column: x => x.SourceWorkflowTemplateTransitionId,
                        principalTable: "WorkflowTemplateTransitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CongressAnnouncementTranslations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CongressAnnouncementId = table.Column<Guid>(type: "uuid", nullable: false),
                    LanguageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Summary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Content = table.Column<string>(type: "text", nullable: true),
                    SeoTitle = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    SeoDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CongressAnnouncementTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CongressAnnouncementTranslations_CongressAnnouncements_Cong~",
                        column: x => x.CongressAnnouncementId,
                        principalTable: "CongressAnnouncements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CongressAnnouncementTranslations_Languages_LanguageId",
                        column: x => x.LanguageId,
                        principalTable: "Languages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CongressBoardMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CongressBoardId = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    AcademicTitle = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Institution = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ImagePath = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ImageStorageProvider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ImageBucketName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    ImageObjectName = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ImageFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ImageContentType = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    ImageFileSize = table.Column<long>(type: "bigint", nullable: true),
                    ImageETag = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    IsAcceptanceLetterSigner = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    SignaturePath = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SignatureStorageProvider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SignatureBucketName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    SignatureObjectName = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SignatureFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    SignatureContentType = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    SignatureFileSize = table.Column<long>(type: "bigint", nullable: true),
                    SignatureETag = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CongressBoardMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CongressBoardMembers_CongressBoards_CongressBoardId",
                        column: x => x.CongressBoardId,
                        principalTable: "CongressBoards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CongressBoardTranslations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CongressBoardId = table.Column<Guid>(type: "uuid", nullable: false),
                    LanguageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CongressBoardTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CongressBoardTranslations_CongressBoards_CongressBoardId",
                        column: x => x.CongressBoardId,
                        principalTable: "CongressBoards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CongressBoardTranslations_Languages_LanguageId",
                        column: x => x.LanguageId,
                        principalTable: "Languages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CongressImportantDateTranslations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CongressImportantDateId = table.Column<Guid>(type: "uuid", nullable: false),
                    LanguageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CongressImportantDateTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CongressImportantDateTranslations_CongressImportantDates_Co~",
                        column: x => x.CongressImportantDateId,
                        principalTable: "CongressImportantDates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CongressImportantDateTranslations_Languages_LanguageId",
                        column: x => x.LanguageId,
                        principalTable: "Languages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CongressPaymentPlanTranslations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CongressPaymentPlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    LanguageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CongressPaymentPlanTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CongressPaymentPlanTranslations_CongressPaymentPlans_Congre~",
                        column: x => x.CongressPaymentPlanId,
                        principalTable: "CongressPaymentPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CongressPaymentPlanTranslations_Languages_LanguageId",
                        column: x => x.LanguageId,
                        principalTable: "Languages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CongressSectionTranslations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CongressSectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    LanguageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CongressSectionTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CongressSectionTranslations_CongressSections_CongressSectio~",
                        column: x => x.CongressSectionId,
                        principalTable: "CongressSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CongressSectionTranslations_Languages_LanguageId",
                        column: x => x.LanguageId,
                        principalTable: "Languages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CongressSliderTranslations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CongressSliderId = table.Column<Guid>(type: "uuid", nullable: false),
                    LanguageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Subtitle = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ButtonText = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    ButtonUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CongressSliderTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CongressSliderTranslations_CongressSliders_CongressSliderId",
                        column: x => x.CongressSliderId,
                        principalTable: "CongressSliders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CongressSliderTranslations_Languages_LanguageId",
                        column: x => x.LanguageId,
                        principalTable: "Languages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AuthorSubmission",
                columns: table => new
                {
                    AuthorsId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmissionsId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthorSubmission", x => new { x.AuthorsId, x.SubmissionsId });
                    table.ForeignKey(
                        name: "FK_AuthorSubmission_Authors_AuthorsId",
                        column: x => x.AuthorsId,
                        principalTable: "Authors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AuthorSubmission_Submissions_SubmissionsId",
                        column: x => x.SubmissionsId,
                        principalTable: "Submissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PaymentDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CongressId = table.Column<Guid>(type: "uuid", nullable: true),
                    FilePath = table.Column<string>(type: "text", nullable: false),
                    OriginalFileName = table.Column<string>(type: "text", nullable: true),
                    ContentType = table.Column<string>(type: "text", nullable: true),
                    Size = table.Column<long>(type: "bigint", nullable: true),
                    IsApproved = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentDocuments_Congresses_CongressId",
                        column: x => x.CongressId,
                        principalTable: "Congresses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PaymentDocuments_Submissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "Submissions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ReviewerSubmission",
                columns: table => new
                {
                    ReviewersId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmissionsId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReviewerSubmission", x => new { x.ReviewersId, x.SubmissionsId });
                    table.ForeignKey(
                        name: "FK_ReviewerSubmission_Reviewers_ReviewersId",
                        column: x => x.ReviewersId,
                        principalTable: "Reviewers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReviewerSubmission_Submissions_SubmissionsId",
                        column: x => x.SubmissionsId,
                        principalTable: "Submissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SubmissionAcceptanceLetters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    LanguageId = table.Column<Guid>(type: "uuid", nullable: true),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LetterNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    AuthorFullNameSnapshot = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    AuthorEmailSnapshot = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    SignerBoardMemberId = table.Column<Guid>(type: "uuid", nullable: true),
                    SignerNameSnapshot = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    SignerTitleSnapshot = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    HtmlSnapshot = table.Column<string>(type: "text", nullable: false),
                    PdfFilePath = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    StorageProvider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PdfBucketName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    PdfObjectName = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PdfContentType = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    PdfFileSize = table.Column<long>(type: "bigint", nullable: true),
                    PdfETag = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    GeneratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SentToEmail = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubmissionAcceptanceLetters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubmissionAcceptanceLetters_Authors_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "Authors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubmissionAcceptanceLetters_Languages_LanguageId",
                        column: x => x.LanguageId,
                        principalTable: "Languages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubmissionAcceptanceLetters_Submissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "Submissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SubmissionEvaluations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Comment = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    EditorComment = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Recommendation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    TotalScore = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubmissionEvaluations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubmissionEvaluations_Reviewers_ReviewerId",
                        column: x => x.ReviewerId,
                        principalTable: "Reviewers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubmissionEvaluations_Submissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "Submissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SubmissionExhibitionDetails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Dimensions = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Technique = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Address = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubmissionExhibitionDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubmissionExhibitionDetails_Submissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "Submissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SubmissionFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileKind = table.Column<int>(type: "integer", nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FilePath = table.Column<string>(type: "character varying(750)", maxLength: 750, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    FileSize = table.Column<long>(type: "bigint", nullable: true),
                    ReviewStatus = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    ReviewedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewNote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsIncludedInProgramBook = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    VersionNo = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubmissionFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubmissionFiles_Submissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "Submissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SubmissionHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromStatusId = table.Column<int>(type: "integer", nullable: true),
                    ToStatusId = table.Column<int>(type: "integer", nullable: true),
                    PerformedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    TransactionStatusTransitionId = table.Column<int>(type: "integer", nullable: true),
                    Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PublicNote = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    InternalNote = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    PerformedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsAutomatic = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubmissionHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubmissionHistories_Submissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "Submissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SubmissionHistories_TransactionStatusTransitions_Transactio~",
                        column: x => x.TransactionStatusTransitionId,
                        principalTable: "TransactionStatusTransitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubmissionHistories_TransactionStatuses_FromStatusId",
                        column: x => x.FromStatusId,
                        principalTable: "TransactionStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubmissionHistories_TransactionStatuses_ToStatusId",
                        column: x => x.ToStatusId,
                        principalTable: "TransactionStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubmissionHistories_Users_PerformedByUserId",
                        column: x => x.PerformedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CongressBoardMemberTranslations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CongressBoardMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    LanguageId = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: true),
                    Institution = table.Column<string>(type: "text", nullable: true),
                    Biography = table.Column<string>(type: "text", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CongressBoardMemberTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CongressBoardMemberTranslations_CongressBoardMembers_Congre~",
                        column: x => x.CongressBoardMemberId,
                        principalTable: "CongressBoardMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CongressBoardMemberTranslations_Languages_LanguageId",
                        column: x => x.LanguageId,
                        principalTable: "Languages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EvaluationScores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmissionEvaluationId = table.Column<Guid>(type: "uuid", nullable: false),
                    EvaluationCriterionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Score = table.Column<decimal>(type: "numeric", nullable: false),
                    Comment = table.Column<string>(type: "text", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluationScores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EvaluationScores_EvaluationCriterions_EvaluationCriterionId",
                        column: x => x.EvaluationCriterionId,
                        principalTable: "EvaluationCriterions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EvaluationScores_SubmissionEvaluations_SubmissionEvaluation~",
                        column: x => x.SubmissionEvaluationId,
                        principalTable: "SubmissionEvaluations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Authors_TitleId",
                table: "Authors",
                column: "TitleId");

            migrationBuilder.CreateIndex(
                name: "IX_AuthorSubmission_SubmissionsId",
                table: "AuthorSubmission",
                column: "SubmissionsId");

            migrationBuilder.CreateIndex(
                name: "IX_Citys_StateId",
                table: "Citys",
                column: "StateId");

            migrationBuilder.CreateIndex(
                name: "IX_CityTranslations_CityId",
                table: "CityTranslations",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_CityTranslations_LanguageId",
                table: "CityTranslations",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_CongressAnnouncements_CongressId",
                table: "CongressAnnouncements",
                column: "CongressId");

            migrationBuilder.CreateIndex(
                name: "IX_CongressAnnouncements_CongressId_IsActive",
                table: "CongressAnnouncements",
                columns: new[] { "CongressId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_CongressAnnouncements_CongressId_IsPinned_Order",
                table: "CongressAnnouncements",
                columns: new[] { "CongressId", "IsPinned", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_CongressAnnouncements_CongressId_ShowInTicker",
                table: "CongressAnnouncements",
                columns: new[] { "CongressId", "ShowInTicker" });

            migrationBuilder.CreateIndex(
                name: "IX_CongressAnnouncements_CongressId_ShowOnHomePage",
                table: "CongressAnnouncements",
                columns: new[] { "CongressId", "ShowOnHomePage" });

            migrationBuilder.CreateIndex(
                name: "IX_CongressAnnouncements_CongressId_Status",
                table: "CongressAnnouncements",
                columns: new[] { "CongressId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CongressAnnouncements_PublishEndDate",
                table: "CongressAnnouncements",
                column: "PublishEndDate");

            migrationBuilder.CreateIndex(
                name: "IX_CongressAnnouncements_PublishStartDate",
                table: "CongressAnnouncements",
                column: "PublishStartDate");

            migrationBuilder.CreateIndex(
                name: "IX_CongressAnnouncementTranslations_CongressAnnouncementId_Lan~",
                table: "CongressAnnouncementTranslations",
                columns: new[] { "CongressAnnouncementId", "LanguageId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CongressAnnouncementTranslations_LanguageId",
                table: "CongressAnnouncementTranslations",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_CongressBoardMembers_CongressBoardId_Order",
                table: "CongressBoardMembers",
                columns: new[] { "CongressBoardId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_CongressBoardMembers_IsAcceptanceLetterSigner",
                table: "CongressBoardMembers",
                column: "IsAcceptanceLetterSigner");

            migrationBuilder.CreateIndex(
                name: "IX_CongressBoardMemberTranslations_CongressBoardMemberId",
                table: "CongressBoardMemberTranslations",
                column: "CongressBoardMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_CongressBoardMemberTranslations_LanguageId",
                table: "CongressBoardMemberTranslations",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_CongressBoards_CongressId",
                table: "CongressBoards",
                column: "CongressId");

            migrationBuilder.CreateIndex(
                name: "IX_CongressBoardTranslations_CongressBoardId",
                table: "CongressBoardTranslations",
                column: "CongressBoardId");

            migrationBuilder.CreateIndex(
                name: "IX_CongressBoardTranslations_LanguageId",
                table: "CongressBoardTranslations",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_CongressDocuments_BucketName_ObjectName",
                table: "CongressDocuments",
                columns: new[] { "BucketName", "ObjectName" },
                filter: "\"BucketName\" IS NOT NULL AND \"ObjectName\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CongressDocuments_CongressId_Order",
                table: "CongressDocuments",
                columns: new[] { "CongressId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_CongressDocuments_DocumentTypeId",
                table: "CongressDocuments",
                column: "DocumentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_CongressDocuments_IsActive",
                table: "CongressDocuments",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Congresses_CityId",
                table: "Congresses",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_Congresses_CountryId",
                table: "Congresses",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_Congresses_EndDate",
                table: "Congresses",
                column: "EndDate");

            migrationBuilder.CreateIndex(
                name: "IX_Congresses_OrganizationId_Code",
                table: "Congresses",
                columns: new[] { "OrganizationId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Congresses_OrganizationId_Slug",
                table: "Congresses",
                columns: new[] { "OrganizationId", "Slug" },
                unique: true,
                filter: "\"Slug\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Congresses_StartDate",
                table: "Congresses",
                column: "StartDate");

            migrationBuilder.CreateIndex(
                name: "IX_Congresses_StateId",
                table: "Congresses",
                column: "StateId");

            migrationBuilder.CreateIndex(
                name: "IX_Congresses_Status",
                table: "Congresses",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CongressEvaluationCriterions_CongressId",
                table: "CongressEvaluationCriterions",
                column: "CongressId");

            migrationBuilder.CreateIndex(
                name: "IX_CongressEvaluationCriterions_EvaluationCriterionId",
                table: "CongressEvaluationCriterions",
                column: "EvaluationCriterionId");

            migrationBuilder.CreateIndex(
                name: "IX_CongressImportantDates_CongressId",
                table: "CongressImportantDates",
                column: "CongressId");

            migrationBuilder.CreateIndex(
                name: "IX_CongressImportantDates_CongressId_Order",
                table: "CongressImportantDates",
                columns: new[] { "CongressId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_CongressImportantDateTranslations_CongressImportantDateId",
                table: "CongressImportantDateTranslations",
                column: "CongressImportantDateId");

            migrationBuilder.CreateIndex(
                name: "IX_CongressImportantDateTranslations_LanguageId",
                table: "CongressImportantDateTranslations",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_CongressPaymentPlans_CongressId_Code",
                table: "CongressPaymentPlans",
                columns: new[] { "CongressId", "Code" },
                unique: true,
                filter: "\"DeletedDate\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CongressPaymentPlans_PublicLookup",
                table: "CongressPaymentPlans",
                columns: new[] { "CongressId", "AudienceType", "IsActive", "IsPublicVisible" });

            migrationBuilder.CreateIndex(
                name: "IX_CongressPaymentPlanTranslations_CongressPaymentPlanId",
                table: "CongressPaymentPlanTranslations",
                column: "CongressPaymentPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_CongressPaymentPlanTranslations_LanguageId",
                table: "CongressPaymentPlanTranslations",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_CongressReviewers_CongressId",
                table: "CongressReviewers",
                column: "CongressId");

            migrationBuilder.CreateIndex(
                name: "IX_CongressReviewers_CongressId_ReviewerId",
                table: "CongressReviewers",
                columns: new[] { "CongressId", "ReviewerId" },
                unique: true,
                filter: "\"DeletedDate\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CongressReviewers_IsActive",
                table: "CongressReviewers",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_CongressReviewers_ReviewerId",
                table: "CongressReviewers",
                column: "ReviewerId");

            migrationBuilder.CreateIndex(
                name: "IX_CongressSections_CongressId",
                table: "CongressSections",
                column: "CongressId");

            migrationBuilder.CreateIndex(
                name: "IX_CongressSectionTranslations_CongressSectionId",
                table: "CongressSectionTranslations",
                column: "CongressSectionId");

            migrationBuilder.CreateIndex(
                name: "IX_CongressSectionTranslations_LanguageId",
                table: "CongressSectionTranslations",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_CongressSliders_CongressId",
                table: "CongressSliders",
                column: "CongressId");

            migrationBuilder.CreateIndex(
                name: "IX_CongressSliders_CongressId_IsActive",
                table: "CongressSliders",
                columns: new[] { "CongressId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_CongressSliders_CongressId_Order",
                table: "CongressSliders",
                columns: new[] { "CongressId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_CongressSliderTranslations_CongressSliderId_LanguageId",
                table: "CongressSliderTranslations",
                columns: new[] { "CongressSliderId", "LanguageId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CongressSliderTranslations_LanguageId",
                table: "CongressSliderTranslations",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_CongressSubmissionTypes_CongressId_Order",
                table: "CongressSubmissionTypes",
                columns: new[] { "CongressId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_CongressSubmissionTypes_CongressId_SubmissionTypeId",
                table: "CongressSubmissionTypes",
                columns: new[] { "CongressId", "SubmissionTypeId" },
                unique: true,
                filter: "\"DeletedDate\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CongressSubmissionTypes_SubmissionTypeId",
                table: "CongressSubmissionTypes",
                column: "SubmissionTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_CongressTopics_CongressId_Order",
                table: "CongressTopics",
                columns: new[] { "CongressId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_CongressTopics_CongressId_TopicId",
                table: "CongressTopics",
                columns: new[] { "CongressId", "TopicId" },
                unique: true,
                filter: "\"DeletedDate\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CongressTopics_TopicId",
                table: "CongressTopics",
                column: "TopicId");

            migrationBuilder.CreateIndex(
                name: "IX_CongressTransactionStatusTransitions_CongressId_Order",
                table: "CongressTransactionStatusTransitions",
                columns: new[] { "CongressId", "Order" },
                filter: "\"DeletedDate\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CongressTransactionStatusTransitions_CongressId_Transaction~",
                table: "CongressTransactionStatusTransitions",
                columns: new[] { "CongressId", "TransactionStatusTransitionId" },
                unique: true,
                filter: "\"DeletedDate\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CongressTransactionStatusTransitions_SourceWorkflowTemplate~",
                table: "CongressTransactionStatusTransitions",
                column: "SourceWorkflowTemplateTransitionId");

            migrationBuilder.CreateIndex(
                name: "IX_CongressTransactionStatusTransitions_TransactionStatusTrans~",
                table: "CongressTransactionStatusTransitions",
                column: "TransactionStatusTransitionId");

            migrationBuilder.CreateIndex(
                name: "IX_CongressTranslations_CongressId_LanguageId",
                table: "CongressTranslations",
                columns: new[] { "CongressId", "LanguageId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CongressTranslations_LanguageId",
                table: "CongressTranslations",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_CongressWorkflowSettings_CongressId",
                table: "CongressWorkflowSettings",
                column: "CongressId",
                unique: true,
                filter: "\"DeletedDate\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CongressWorkflowSettings_InitialTransactionStatusId",
                table: "CongressWorkflowSettings",
                column: "InitialTransactionStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_CongressWorkflowSettings_SourceWorkflowTemplateId",
                table: "CongressWorkflowSettings",
                column: "SourceWorkflowTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_CountryTranslations_CountryId",
                table: "CountryTranslations",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_CountryTranslations_LanguageId",
                table: "CountryTranslations",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentTypeTranslations_DocumentTypeId",
                table: "DocumentTypeTranslations",
                column: "DocumentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentTypeTranslations_LanguageId",
                table: "DocumentTypeTranslations",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationCriterionTranslations_EvaluationCriterionId",
                table: "EvaluationCriterionTranslations",
                column: "EvaluationCriterionId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationCriterionTranslations_LanguageId",
                table: "EvaluationCriterionTranslations",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationScoreOptions_IsActive_Order",
                table: "EvaluationScoreOptions",
                columns: new[] { "IsActive", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationScoreOptions_Value",
                table: "EvaluationScoreOptions",
                column: "Value",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationScores_EvaluationCriterionId",
                table: "EvaluationScores",
                column: "EvaluationCriterionId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationScores_SubmissionEvaluationId",
                table: "EvaluationScores",
                column: "SubmissionEvaluationId");

            migrationBuilder.CreateIndex(
                name: "IX_EventRoomTranslations_EventRoomId",
                table: "EventRoomTranslations",
                column: "EventRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_EventRoomTranslations_LanguageId",
                table: "EventRoomTranslations",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_MailOutboxMessages_BulkEmailBatchId",
                table: "MailOutboxMessages",
                column: "BulkEmailBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_MailOutboxMessages_CongressId_CreatedDate",
                table: "MailOutboxMessages",
                columns: new[] { "CongressId", "CreatedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_MailOutboxMessages_RelatedSubmissionId",
                table: "MailOutboxMessages",
                column: "RelatedSubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_MailOutboxMessages_Status_CreatedDate",
                table: "MailOutboxMessages",
                columns: new[] { "Status", "CreatedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_MailOutboxMessages_TrackingToken",
                table: "MailOutboxMessages",
                column: "TrackingToken",
                unique: true,
                filter: "\"TrackingToken\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationApiKeys_KeyPrefix",
                table: "OrganizationApiKeys",
                column: "KeyPrefix",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationApiKeys_OrganizationId",
                table: "OrganizationApiKeys",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_Code",
                table: "Organizations",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_Slug",
                table: "Organizations",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationUsers_DefaultCongressId",
                table: "OrganizationUsers",
                column: "DefaultCongressId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationUsers_OrganizationId",
                table: "OrganizationUsers",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationUsers_UserId",
                table: "OrganizationUsers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentDocuments_CongressId",
                table: "PaymentDocuments",
                column: "CongressId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentDocuments_SubmissionId",
                table: "PaymentDocuments",
                column: "SubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentStatusTranslations_LanguageId",
                table: "PaymentStatusTranslations",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentStatusTranslations_PaymentStatusId",
                table: "PaymentStatusTranslations",
                column: "PaymentStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Regions_CountryId",
                table: "Regions",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_RegionTranslations_LanguageId",
                table: "RegionTranslations",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_RegionTranslations_RegionId",
                table: "RegionTranslations",
                column: "RegionId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceValues_LanguageId",
                table: "ResourceValues",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceValues_ResourceKeyId",
                table: "ResourceValues",
                column: "ResourceKeyId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviewers_IsActive",
                table: "Reviewers",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Reviewers_UserId",
                table: "Reviewers",
                column: "UserId",
                unique: true,
                filter: "\"DeletedDate\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewerSubmission_SubmissionsId",
                table: "ReviewerSubmission",
                column: "SubmissionsId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleClaims_RoleId",
                table: "RoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "Roles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShortLinks_Code",
                table: "ShortLinks",
                column: "Code",
                unique: true,
                filter: "\"DeletedDate\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ShortLinks_TargetType_TargetId",
                table: "ShortLinks",
                columns: new[] { "TargetType", "TargetId" },
                filter: "\"DeletedDate\" IS NULL AND \"IsActive\"");

            migrationBuilder.CreateIndex(
                name: "IX_States_CountryId",
                table: "States",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_StateTranslations_LanguageId",
                table: "StateTranslations",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_StateTranslations_StateId",
                table: "StateTranslations",
                column: "StateId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionAcceptanceLetters_AuthorId",
                table: "SubmissionAcceptanceLetters",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionAcceptanceLetters_LanguageId",
                table: "SubmissionAcceptanceLetters",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionAcceptanceLetters_SubmissionId_AuthorId_LanguageId",
                table: "SubmissionAcceptanceLetters",
                columns: new[] { "SubmissionId", "AuthorId", "LanguageId" },
                unique: true,
                filter: "\"DeletedDate\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionAcceptanceLetters_SubmissionId_LanguageId_LetterN~",
                table: "SubmissionAcceptanceLetters",
                columns: new[] { "SubmissionId", "LanguageId", "LetterNumber" },
                unique: true,
                filter: "\"DeletedDate\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionEvaluations_ReviewerId",
                table: "SubmissionEvaluations",
                column: "ReviewerId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionEvaluations_SubmissionId_ReviewerId",
                table: "SubmissionEvaluations",
                columns: new[] { "SubmissionId", "ReviewerId" },
                unique: true,
                filter: "\"DeletedDate\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionExhibitionDetails_SubmissionId",
                table: "SubmissionExhibitionDetails",
                column: "SubmissionId",
                unique: true,
                filter: "\"DeletedDate\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionFiles_FileKind_ReviewStatus_IsActive",
                table: "SubmissionFiles",
                columns: new[] { "FileKind", "ReviewStatus", "IsActive" },
                filter: "\"DeletedDate\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionFiles_SubmissionId_FileKind",
                table: "SubmissionFiles",
                columns: new[] { "SubmissionId", "FileKind" },
                filter: "\"DeletedDate\" IS NULL AND \"IsActive\"");

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionHistories_FromStatusId",
                table: "SubmissionHistories",
                column: "FromStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionHistories_PerformedByUserId",
                table: "SubmissionHistories",
                column: "PerformedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionHistories_SubmissionId_PerformedAt",
                table: "SubmissionHistories",
                columns: new[] { "SubmissionId", "PerformedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionHistories_ToStatusId",
                table: "SubmissionHistories",
                column: "ToStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionHistories_TransactionStatusTransitionId",
                table: "SubmissionHistories",
                column: "TransactionStatusTransitionId");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_AppUserId",
                table: "Submissions",
                column: "AppUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_CongressId_SubmissionNumber",
                table: "Submissions",
                columns: new[] { "CongressId", "SubmissionNumber" },
                unique: true,
                filter: "\"DeletedDate\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_CreatedByUserId",
                table: "Submissions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_LanguageId",
                table: "Submissions",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_PaymentStatusId",
                table: "Submissions",
                column: "PaymentStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_SubmissionTypeId",
                table: "Submissions",
                column: "SubmissionTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_TopicId",
                table: "Submissions",
                column: "TopicId");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_TransactionStatusId",
                table: "Submissions",
                column: "TransactionStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionTypeTranslations_LanguageId",
                table: "SubmissionTypeTranslations",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionTypeTranslations_SubmissionTypeId",
                table: "SubmissionTypeTranslations",
                column: "SubmissionTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_TitleTranslations_LanguageId",
                table: "TitleTranslations",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_TitleTranslations_TitleId",
                table: "TitleTranslations",
                column: "TitleId");

            migrationBuilder.CreateIndex(
                name: "IX_TopicTranslations_LanguageId",
                table: "TopicTranslations",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_TopicTranslations_TopicId",
                table: "TopicTranslations",
                column: "TopicId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionStatuses_Code",
                table: "TransactionStatuses",
                column: "Code",
                unique: true,
                filter: "\"DeletedDate\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionStatuses_TransactionStatusPhaseId_Order",
                table: "TransactionStatuses",
                columns: new[] { "TransactionStatusPhaseId", "Order" },
                filter: "\"DeletedDate\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionStatusPhases_Code",
                table: "TransactionStatusPhases",
                column: "Code",
                unique: true,
                filter: "\"DeletedDate\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionStatusPhases_Order",
                table: "TransactionStatusPhases",
                column: "Order",
                filter: "\"DeletedDate\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionStatusPhaseTranslations_LanguageId",
                table: "TransactionStatusPhaseTranslations",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionStatusPhaseTranslations_TransactionStatusPhaseId~",
                table: "TransactionStatusPhaseTranslations",
                columns: new[] { "TransactionStatusPhaseId", "LanguageId" },
                unique: true,
                filter: "\"DeletedDate\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionStatusTransitions_FromStatusId_ToStatusId",
                table: "TransactionStatusTransitions",
                columns: new[] { "FromStatusId", "ToStatusId" },
                unique: true,
                filter: "\"DeletedDate\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionStatusTransitions_ToStatusId",
                table: "TransactionStatusTransitions",
                column: "ToStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionStatusTransitionTranslations_LanguageId",
                table: "TransactionStatusTransitionTranslations",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionStatusTransitionTranslations_TransactionStatusTr~",
                table: "TransactionStatusTransitionTranslations",
                columns: new[] { "TransactionStatusTransitionId", "LanguageId" },
                unique: true,
                filter: "\"DeletedDate\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionStatusTranslations_LanguageId",
                table: "TransactionStatusTranslations",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionStatusTranslations_TransactionStatusId_LanguageId",
                table: "TransactionStatusTranslations",
                columns: new[] { "TransactionStatusId", "LanguageId" },
                unique: true,
                filter: "\"DeletedDate\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserClaims_UserId",
                table: "UserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserLogins_UserId",
                table: "UserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "Users",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_Users_CountryId",
                table: "Users",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_StateId",
                table: "Users",
                column: "StateId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_TitleId",
                table: "Users",
                column: "TitleId");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "Users",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTemplates_Code",
                table: "WorkflowTemplates",
                column: "Code",
                unique: true,
                filter: "\"DeletedDate\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTemplates_InitialTransactionStatusId",
                table: "WorkflowTemplates",
                column: "InitialTransactionStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTemplates_IsDefault",
                table: "WorkflowTemplates",
                column: "IsDefault",
                unique: true,
                filter: "\"IsDefault\" = TRUE AND \"DeletedDate\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTemplateTransitions_TransactionStatusTransitionId",
                table: "WorkflowTemplateTransitions",
                column: "TransactionStatusTransitionId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTemplateTransitions_TransactionStatusTranslationId",
                table: "WorkflowTemplateTransitions",
                column: "TransactionStatusTranslationId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTemplateTransitions_WorkflowTemplateId_Order",
                table: "WorkflowTemplateTransitions",
                columns: new[] { "WorkflowTemplateId", "Order" },
                filter: "\"DeletedDate\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTemplateTransitions_WorkflowTemplateId_TransactionS~",
                table: "WorkflowTemplateTransitions",
                columns: new[] { "WorkflowTemplateId", "TransactionStatusTransitionId" },
                unique: true,
                filter: "\"DeletedDate\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTemplateTranslations_LanguageId",
                table: "WorkflowTemplateTranslations",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTemplateTranslations_WorkflowTemplateId_LanguageId",
                table: "WorkflowTemplateTranslations",
                columns: new[] { "WorkflowTemplateId", "LanguageId" },
                unique: true,
                filter: "\"DeletedDate\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTransitionConditions_TransactionStatusTransitionId_~",
                table: "WorkflowTransitionConditions",
                columns: new[] { "TransactionStatusTransitionId", "Order" },
                filter: "\"DeletedDate\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTransitionEffects_TransactionStatusTransitionId_Ord~",
                table: "WorkflowTransitionEffects",
                columns: new[] { "TransactionStatusTransitionId", "Order" },
                filter: "\"DeletedDate\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuthorSubmission");

            migrationBuilder.DropTable(
                name: "CityTranslations");

            migrationBuilder.DropTable(
                name: "CongressAnnouncementTranslations");

            migrationBuilder.DropTable(
                name: "CongressBoardMemberTranslations");

            migrationBuilder.DropTable(
                name: "CongressBoardTranslations");

            migrationBuilder.DropTable(
                name: "CongressDocuments");

            migrationBuilder.DropTable(
                name: "CongressEvaluationCriterions");

            migrationBuilder.DropTable(
                name: "CongressImportantDateTranslations");

            migrationBuilder.DropTable(
                name: "CongressPaymentPlanTranslations");

            migrationBuilder.DropTable(
                name: "CongressReviewers");

            migrationBuilder.DropTable(
                name: "CongressSectionTranslations");

            migrationBuilder.DropTable(
                name: "CongressSliderTranslations");

            migrationBuilder.DropTable(
                name: "CongressSubmissionTypes");

            migrationBuilder.DropTable(
                name: "CongressTopics");

            migrationBuilder.DropTable(
                name: "CongressTransactionStatusTransitions");

            migrationBuilder.DropTable(
                name: "CongressTranslations");

            migrationBuilder.DropTable(
                name: "CongressWorkflowSettings");

            migrationBuilder.DropTable(
                name: "CountryTranslations");

            migrationBuilder.DropTable(
                name: "DocumentTypeTranslations");

            migrationBuilder.DropTable(
                name: "EvaluationCriterionTranslations");

            migrationBuilder.DropTable(
                name: "EvaluationScoreOptions");

            migrationBuilder.DropTable(
                name: "EvaluationScores");

            migrationBuilder.DropTable(
                name: "EventRoomTranslations");

            migrationBuilder.DropTable(
                name: "MailOutboxMessages");

            migrationBuilder.DropTable(
                name: "OrganizationApiKeys");

            migrationBuilder.DropTable(
                name: "OrganizationUsers");

            migrationBuilder.DropTable(
                name: "PaymentDocuments");

            migrationBuilder.DropTable(
                name: "PaymentStatusTranslations");

            migrationBuilder.DropTable(
                name: "RegionTranslations");

            migrationBuilder.DropTable(
                name: "ResourceValues");

            migrationBuilder.DropTable(
                name: "ReviewerSubmission");

            migrationBuilder.DropTable(
                name: "RoleClaims");

            migrationBuilder.DropTable(
                name: "ShortLinks");

            migrationBuilder.DropTable(
                name: "StateTranslations");

            migrationBuilder.DropTable(
                name: "SubmissionAcceptanceLetters");

            migrationBuilder.DropTable(
                name: "SubmissionExhibitionDetails");

            migrationBuilder.DropTable(
                name: "SubmissionFiles");

            migrationBuilder.DropTable(
                name: "SubmissionHistories");

            migrationBuilder.DropTable(
                name: "SubmissionTypeTranslations");

            migrationBuilder.DropTable(
                name: "TitleTranslations");

            migrationBuilder.DropTable(
                name: "TopicTranslations");

            migrationBuilder.DropTable(
                name: "TransactionStatusPhaseTranslations");

            migrationBuilder.DropTable(
                name: "TransactionStatusTransitionTranslations");

            migrationBuilder.DropTable(
                name: "UserClaims");

            migrationBuilder.DropTable(
                name: "UserLogins");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "UserTokens");

            migrationBuilder.DropTable(
                name: "WorkflowTemplateTranslations");

            migrationBuilder.DropTable(
                name: "WorkflowTransitionConditions");

            migrationBuilder.DropTable(
                name: "WorkflowTransitionEffects");

            migrationBuilder.DropTable(
                name: "CongressAnnouncements");

            migrationBuilder.DropTable(
                name: "CongressBoardMembers");

            migrationBuilder.DropTable(
                name: "CongressImportantDates");

            migrationBuilder.DropTable(
                name: "CongressPaymentPlans");

            migrationBuilder.DropTable(
                name: "CongressSections");

            migrationBuilder.DropTable(
                name: "CongressSliders");

            migrationBuilder.DropTable(
                name: "WorkflowTemplateTransitions");

            migrationBuilder.DropTable(
                name: "DocumentTypes");

            migrationBuilder.DropTable(
                name: "EvaluationCriterions");

            migrationBuilder.DropTable(
                name: "SubmissionEvaluations");

            migrationBuilder.DropTable(
                name: "EventRooms");

            migrationBuilder.DropTable(
                name: "Regions");

            migrationBuilder.DropTable(
                name: "ResourceKeys");

            migrationBuilder.DropTable(
                name: "Authors");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "CongressBoards");

            migrationBuilder.DropTable(
                name: "TransactionStatusTransitions");

            migrationBuilder.DropTable(
                name: "TransactionStatusTranslations");

            migrationBuilder.DropTable(
                name: "WorkflowTemplates");

            migrationBuilder.DropTable(
                name: "Reviewers");

            migrationBuilder.DropTable(
                name: "Submissions");

            migrationBuilder.DropTable(
                name: "Congresses");

            migrationBuilder.DropTable(
                name: "Languages");

            migrationBuilder.DropTable(
                name: "PaymentStatuss");

            migrationBuilder.DropTable(
                name: "SubmissionTypes");

            migrationBuilder.DropTable(
                name: "Topics");

            migrationBuilder.DropTable(
                name: "TransactionStatuses");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Citys");

            migrationBuilder.DropTable(
                name: "Organizations");

            migrationBuilder.DropTable(
                name: "TransactionStatusPhases");

            migrationBuilder.DropTable(
                name: "Titles");

            migrationBuilder.DropTable(
                name: "States");

            migrationBuilder.DropTable(
                name: "Countrys");
        }
    }
}
