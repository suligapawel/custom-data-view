using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomDataView.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTabsPaginationAndActions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_tiles_display",
                schema: "custom_data_view",
                table: "tiles");

            migrationBuilder.DropCheckConstraint(
                name: "ck_tiles_items",
                schema: "custom_data_view",
                table: "tiles");

            migrationBuilder.AlterColumn<string>(
                name: "display",
                schema: "custom_data_view",
                table: "tiles",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10);

            migrationBuilder.AddColumn<string>(
                name: "pagination_page_number_parameter",
                schema: "custom_data_view",
                table: "tiles",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "pagination_page_size",
                schema: "custom_data_view",
                table: "tiles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pagination_page_size_parameter",
                schema: "custom_data_view",
                table: "tiles",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pagination_total_path",
                schema: "custom_data_view",
                table: "tiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "label",
                schema: "custom_data_view",
                table: "tile_children",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "source_node",
                schema: "custom_data_view",
                table: "tile_bindings",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "target_node",
                schema: "custom_data_view",
                table: "tile_bindings",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(110)",
                oldMaxLength: 110);

            migrationBuilder.CreateTable(
                name: "tile_actions",
                schema: "custom_data_view",
                columns: table => new
                {
                    tile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    type = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                    target_tile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scope = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tile_actions", x => new { x.tile_id, x.key });
                    table.UniqueConstraint("uq_tile_actions_tile_id_position", x => new { x.tile_id, x.position });
                    table.CheckConstraint("ck_tile_actions_position", "position >= 0");
                    table.CheckConstraint("ck_tile_actions_scope", "scope IN ('tile', 'item')");
                    table.CheckConstraint("ck_tile_actions_type", "type IN ('button', 'link')");
                    table.ForeignKey(
                        name: "fk_tile_actions_target_tile_id",
                        column: x => x.target_tile_id,
                        principalSchema: "custom_data_view",
                        principalTable: "tiles",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_tile_actions_tile_id",
                        column: x => x.tile_id,
                        principalSchema: "custom_data_view",
                        principalTable: "tiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tile_action_mappings",
                schema: "custom_data_view",
                columns: table => new
                {
                    tile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action_key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    target_kind = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    target_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    source_field = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tile_action_mappings", x => new { x.tile_id, x.action_key, x.target_kind, x.target_name });
                    table.CheckConstraint("ck_tile_action_mappings_position", "position >= 0");
                    table.CheckConstraint("ck_tile_action_mappings_target_kind", "target_kind IN ('parameter', 'input')");
                    table.ForeignKey(
                        name: "fk_tile_action_mappings_action",
                        columns: x => new { x.tile_id, x.action_key },
                        principalSchema: "custom_data_view",
                        principalTable: "tile_actions",
                        principalColumns: new[] { "tile_id", "key" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_tiles_display",
                schema: "custom_data_view",
                table: "tiles",
                sql: "display IN ('table', 'list', 'details', 'summary', 'form', 'tabs', 'pagination_table')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_tiles_items",
                schema: "custom_data_view",
                table: "tiles",
                sql: "(display IN ('table', 'list', 'pagination_table')) = (items_source_key IS NOT NULL AND items_path IS NOT NULL AND items_key_label IS NOT NULL) AND (display IN ('table', 'list', 'pagination_table') OR (items_source_key IS NULL AND items_path IS NULL AND items_key_label IS NULL))");

            migrationBuilder.AddCheckConstraint(
                name: "ck_tiles_pagination",
                schema: "custom_data_view",
                table: "tiles",
                sql: "(display = 'pagination_table') = (pagination_page_number_parameter IS NOT NULL AND pagination_page_size_parameter IS NOT NULL AND pagination_page_size IS NOT NULL) AND (display = 'pagination_table' OR (pagination_page_number_parameter IS NULL AND pagination_page_size_parameter IS NULL AND pagination_page_size IS NULL AND pagination_total_path IS NULL)) AND (pagination_page_size IS NULL OR pagination_page_size BETWEEN 1 AND 100)");

            migrationBuilder.CreateIndex(
                name: "ix_tile_actions_target_tile_id",
                schema: "custom_data_view",
                table: "tile_actions",
                column: "target_tile_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tile_action_mappings",
                schema: "custom_data_view");

            migrationBuilder.DropTable(
                name: "tile_actions",
                schema: "custom_data_view");

            migrationBuilder.DropCheckConstraint(
                name: "ck_tiles_display",
                schema: "custom_data_view",
                table: "tiles");

            migrationBuilder.DropCheckConstraint(
                name: "ck_tiles_items",
                schema: "custom_data_view",
                table: "tiles");

            migrationBuilder.DropCheckConstraint(
                name: "ck_tiles_pagination",
                schema: "custom_data_view",
                table: "tiles");

            migrationBuilder.DropColumn(
                name: "pagination_page_number_parameter",
                schema: "custom_data_view",
                table: "tiles");

            migrationBuilder.DropColumn(
                name: "pagination_page_size",
                schema: "custom_data_view",
                table: "tiles");

            migrationBuilder.DropColumn(
                name: "pagination_page_size_parameter",
                schema: "custom_data_view",
                table: "tiles");

            migrationBuilder.DropColumn(
                name: "pagination_total_path",
                schema: "custom_data_view",
                table: "tiles");

            migrationBuilder.DropColumn(
                name: "label",
                schema: "custom_data_view",
                table: "tile_children");

            migrationBuilder.AlterColumn<string>(
                name: "display",
                schema: "custom_data_view",
                table: "tiles",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "source_node",
                schema: "custom_data_view",
                table: "tile_bindings",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "target_node",
                schema: "custom_data_view",
                table: "tile_bindings",
                type: "character varying(110)",
                maxLength: 110,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddCheckConstraint(
                name: "ck_tiles_display",
                schema: "custom_data_view",
                table: "tiles",
                sql: "display IN ('table', 'list', 'details', 'summary', 'form')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_tiles_items",
                schema: "custom_data_view",
                table: "tiles",
                sql: "(display IN ('table', 'list')) = (items_source_key IS NOT NULL AND items_path IS NOT NULL AND items_key_label IS NOT NULL) AND (display IN ('table', 'list') OR (items_source_key IS NULL AND items_path IS NULL AND items_key_label IS NULL))");
        }
    }
}
