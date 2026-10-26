using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Ticketing.Dal.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "reservations",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_public_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValueSql: "'HELD'"),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<string>(type: "text", nullable: false, defaultValueSql: "CURRENT_USER"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reservations", x => x.id);
                    table.CheckConstraint("reservations_status_check", "status IN ('HELD', 'CONFIRMED', 'EXPIRED', 'CANCELLED')");
                    table.CheckConstraint("reservations_version_check", "version > 0");
                });

            migrationBuilder.CreateTable(
                name: "ticket_types",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    event_public_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_title_snapshot = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "char(3)", nullable: false, defaultValue: "UAH"),
                    total_quantity = table.Column<int>(type: "integer", nullable: false),
                    reserved_quantity = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    sold_quantity = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<string>(type: "text", nullable: false, defaultValueSql: "CURRENT_USER"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ticket_types", x => x.id);
                    table.CheckConstraint("ticket_types_inventory_check", "reserved_quantity + sold_quantity <= total_quantity");
                    table.CheckConstraint("ticket_types_price_check", "price >= 0");
                    table.CheckConstraint("ticket_types_reserved_quantity_check", "reserved_quantity >= 0");
                    table.CheckConstraint("ticket_types_sold_quantity_check", "sold_quantity >= 0");
                    table.CheckConstraint("ticket_types_total_quantity_check", "total_quantity >= 0");
                    table.CheckConstraint("ticket_types_version_check", "version > 0");
                });

            migrationBuilder.CreateTable(
                name: "reservation_details",
                columns: table => new
                {
                    reservation_id = table.Column<long>(type: "bigint", nullable: false),
                    contact_name_snapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    contact_email_snapshot = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<string>(type: "text", nullable: false, defaultValueSql: "CURRENT_USER"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reservation_details", x => x.reservation_id);
                    table.CheckConstraint("reservation_details_version_check", "version > 0");
                    table.ForeignKey(
                        name: "FK_reservation_details_reservations_reservation_id",
                        column: x => x.reservation_id,
                        principalTable: "reservations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "reservation_items",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    event_public_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_title_snapshot = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    ticket_type_name_snapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    reservation_id = table.Column<long>(type: "bigint", nullable: false),
                    ticket_type_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<string>(type: "text", nullable: false, defaultValueSql: "CURRENT_USER"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reservation_items", x => x.id);
                    table.UniqueConstraint("reservation_items_id_ticket_type_key", x => new { x.id, x.ticket_type_id });
                    table.CheckConstraint("reservation_items_quantity_check", "quantity > 0");
                    table.CheckConstraint("reservation_items_unit_price_check", "unit_price >= 0");
                    table.CheckConstraint("reservation_items_version_check", "version > 0");
                    table.ForeignKey(
                        name: "FK_reservation_items_reservations_reservation_id",
                        column: x => x.reservation_id,
                        principalTable: "reservations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_reservation_items_ticket_types_ticket_type_id",
                        column: x => x.ticket_type_id,
                        principalTable: "ticket_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tickets",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    reservation_item_id = table.Column<long>(type: "bigint", nullable: false),
                    ticket_type_id = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValueSql: "'VALID'"),
                    issued_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    used_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<string>(type: "text", nullable: false, defaultValueSql: "CURRENT_USER"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tickets", x => x.id);
                    table.CheckConstraint("tickets_used_at_check", "(status = 'USED' AND used_at IS NOT NULL) OR (status <> 'USED' AND used_at IS NULL)");
                    table.CheckConstraint("tickets_version_check", "version > 0");
                    table.ForeignKey(
                        name: "FK_tickets_reservation_items_reservation_item_id_ticket_type_id",
                        columns: x => new { x.reservation_item_id, x.ticket_type_id },
                        principalTable: "reservation_items",
                        principalColumns: new[] { "id", "ticket_type_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_reservation_details_is_deleted",
                table: "reservation_details",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "IX_reservation_items_is_deleted",
                table: "reservation_items",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "IX_reservation_items_ticket_type_id",
                table: "reservation_items",
                column: "ticket_type_id");

            migrationBuilder.CreateIndex(
                name: "reservation_items_reservation_ticket_type_key",
                table: "reservation_items",
                columns: new[] { "reservation_id", "ticket_type_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_reservations_is_deleted",
                table: "reservations",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "IX_reservations_public_id",
                table: "reservations",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "reservations_user_status_created_at_idx",
                table: "reservations",
                columns: new[] { "user_public_id", "status", "created_at" },
                descending: new[] { false, false, true },
                filter: "is_deleted = FALSE")
                .Annotation("Npgsql:IndexInclude", new[] { "public_id", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "IX_ticket_types_is_deleted",
                table: "ticket_types",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ticket_types_event_active_idx",
                table: "ticket_types",
                column: "event_public_id",
                filter: "is_deleted = FALSE")
                .Annotation("Npgsql:IndexInclude", new[] { "name", "price", "currency", "total_quantity", "reserved_quantity", "sold_quantity" });

            migrationBuilder.CreateIndex(
                name: "ticket_types_event_name_key",
                table: "ticket_types",
                columns: new[] { "event_public_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tickets_is_deleted",
                table: "tickets",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "IX_tickets_public_id",
                table: "tickets",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tickets_reservation_item_id_ticket_type_id",
                table: "tickets",
                columns: new[] { "reservation_item_id", "ticket_type_id" });

            migrationBuilder.CreateIndex(
                name: "tickets_reservation_item_status_idx",
                table: "tickets",
                columns: new[] { "reservation_item_id", "status" },
                filter: "is_deleted = FALSE")
                .Annotation("Npgsql:IndexInclude", new[] { "public_id", "issued_at", "used_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "reservation_details");

            migrationBuilder.DropTable(
                name: "tickets");

            migrationBuilder.DropTable(
                name: "reservation_items");

            migrationBuilder.DropTable(
                name: "reservations");

            migrationBuilder.DropTable(
                name: "ticket_types");
        }
    }
}
