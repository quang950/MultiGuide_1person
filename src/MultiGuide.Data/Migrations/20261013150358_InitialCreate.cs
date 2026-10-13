using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MultiGuide.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "language",
                columns: table => new
                {
                    code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_language", x => x.code);
                });

            migrationBuilder.CreateTable(
                name: "tour_area",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    center_lat = table.Column<double>(type: "double precision", nullable: false),
                    center_lng = table.Column<double>(type: "double precision", nullable: false),
                    radius_m = table.Column<double>(type: "double precision", nullable: false),
                    price_usd = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    pass_ttl_hours = table.Column<int>(type: "integer", nullable: false, defaultValue: 24),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tour_area", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "payment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    area_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "USD"),
                    provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "mock"),
                    provider_ref = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    idempotency_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment", x => x.id);
                    table.ForeignKey(
                        name: "FK_payment_tour_area_area_id",
                        column: x => x.area_id,
                        principalTable: "tour_area",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "poi",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    area_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lat = table.Column<double>(type: "double precision", nullable: false),
                    lng = table.Column<double>(type: "double precision", nullable: false),
                    trigger_radius_m = table.Column<double>(type: "double precision", nullable: false, defaultValue: 30.0),
                    priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    image_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_poi", x => x.id);
                    table.ForeignKey(
                        name: "FK_poi_tour_area_area_id",
                        column: x => x.area_id,
                        principalTable: "tour_area",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "access_pass",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    area_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    issued_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_access_pass", x => x.id);
                    table.ForeignKey(
                        name: "FK_access_pass_payment_payment_id",
                        column: x => x.payment_id,
                        principalTable: "payment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_access_pass_tour_area_area_id",
                        column: x => x.area_id,
                        principalTable: "tour_area",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "poi_translation",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    poi_id = table.Column<Guid>(type: "uuid", nullable: false),
                    language_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    audio_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    audio_duration_sec = table.Column<int>(type: "integer", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_poi_translation", x => x.id);
                    table.ForeignKey(
                        name: "FK_poi_translation_language_language_code",
                        column: x => x.language_code,
                        principalTable: "language",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_poi_translation_poi_poi_id",
                        column: x => x.poi_id,
                        principalTable: "poi",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "play_log",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    pass_id = table.Column<Guid>(type: "uuid", nullable: false),
                    poi_id = table.Column<Guid>(type: "uuid", nullable: false),
                    language_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    listened_sec = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    completed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_play_log", x => x.id);
                    table.ForeignKey(
                        name: "FK_play_log_access_pass_pass_id",
                        column: x => x.pass_id,
                        principalTable: "access_pass",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_play_log_language_language_code",
                        column: x => x.language_code,
                        principalTable: "language",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_play_log_poi_poi_id",
                        column: x => x.poi_id,
                        principalTable: "poi",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_access_pass_area_id",
                table: "access_pass",
                column: "area_id");

            migrationBuilder.CreateIndex(
                name: "uq_access_pass_payment_id",
                table: "access_pass",
                column: "payment_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_access_pass_token_hash",
                table: "access_pass",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payment_area_id",
                table: "payment",
                column: "area_id");

            migrationBuilder.CreateIndex(
                name: "uq_payment_idempotency_key",
                table: "payment",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_playlog_poi_started",
                table: "play_log",
                columns: new[] { "poi_id", "started_at" });

            migrationBuilder.CreateIndex(
                name: "IX_play_log_language_code",
                table: "play_log",
                column: "language_code");

            migrationBuilder.CreateIndex(
                name: "IX_play_log_pass_id",
                table: "play_log",
                column: "pass_id");

            migrationBuilder.CreateIndex(
                name: "idx_poi_area_active",
                table: "poi",
                columns: new[] { "area_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "idx_poi_location",
                table: "poi",
                columns: new[] { "lat", "lng" });

            migrationBuilder.CreateIndex(
                name: "IX_poi_translation_language_code",
                table: "poi_translation",
                column: "language_code");

            migrationBuilder.CreateIndex(
                name: "uq_poi_translation_poi_lang",
                table: "poi_translation",
                columns: new[] { "poi_id", "language_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_tour_area_code",
                table: "tour_area",
                column: "code",
                unique: true);

            // Enable Row Level Security (RLS) without policy for all tables (PRD 6.1 & CLAUDE.md)
            migrationBuilder.Sql("ALTER TABLE language ENABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE tour_area ENABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE poi ENABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE poi_translation ENABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE payment ENABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE access_pass ENABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE play_log ENABLE ROW LEVEL SECURITY;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "play_log");

            migrationBuilder.DropTable(
                name: "poi_translation");

            migrationBuilder.DropTable(
                name: "access_pass");

            migrationBuilder.DropTable(
                name: "language");

            migrationBuilder.DropTable(
                name: "poi");

            migrationBuilder.DropTable(
                name: "payment");

            migrationBuilder.DropTable(
                name: "tour_area");
        }
    }
}
