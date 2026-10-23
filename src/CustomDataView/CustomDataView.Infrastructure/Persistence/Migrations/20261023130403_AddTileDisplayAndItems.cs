using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomDataView.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTileDisplayAndItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "display",
                schema: "custom_data_view",
                table: "tiles",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "details");

            migrationBuilder.Sql("ALTER TABLE custom_data_view.tiles ALTER COLUMN display DROP DEFAULT;");

            migrationBuilder.AddColumn<string>(
                name: "items_key_label",
                schema: "custom_data_view",
                table: "tiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "items_path",
                schema: "custom_data_view",
                table: "tiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "items_source_key",
                schema: "custom_data_view",
                table: "tiles",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "role",
                schema: "custom_data_view",
                table: "tile_fields",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

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

            migrationBuilder.AddCheckConstraint(
                name: "ck_tile_fields_role",
                schema: "custom_data_view",
                table: "tile_fields",
                sql: "role IN ('title', 'subtitle', 'badge', 'primary', 'secondary')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_tiles_display",
                schema: "custom_data_view",
                table: "tiles");

            migrationBuilder.DropCheckConstraint(
                name: "ck_tiles_items",
                schema: "custom_data_view",
                table: "tiles");

            migrationBuilder.DropCheckConstraint(
                name: "ck_tile_fields_role",
                schema: "custom_data_view",
                table: "tile_fields");

            migrationBuilder.DropColumn(
                name: "display",
                schema: "custom_data_view",
                table: "tiles");

            migrationBuilder.DropColumn(
                name: "items_key_label",
                schema: "custom_data_view",
                table: "tiles");

            migrationBuilder.DropColumn(
                name: "items_path",
                schema: "custom_data_view",
                table: "tiles");

            migrationBuilder.DropColumn(
                name: "items_source_key",
                schema: "custom_data_view",
                table: "tiles");

            migrationBuilder.DropColumn(
                name: "role",
                schema: "custom_data_view",
                table: "tile_fields");
        }
    }
}
