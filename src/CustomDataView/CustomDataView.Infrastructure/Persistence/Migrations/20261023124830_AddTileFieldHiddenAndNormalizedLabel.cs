using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomDataView.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTileFieldHiddenAndNormalizedLabel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "hidden",
                schema: "custom_data_view",
                table: "tile_fields",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "normalized_label",
                schema: "custom_data_view",
                table: "tile_fields",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                computedColumnSql: "lower(btrim(label))",
                stored: true);

            migrationBuilder.CreateIndex(
                name: "ux_tile_fields_tile_id_normalized_label",
                schema: "custom_data_view",
                table: "tile_fields",
                columns: new[] { "tile_id", "normalized_label" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_tile_fields_tile_id_normalized_label",
                schema: "custom_data_view",
                table: "tile_fields");

            migrationBuilder.DropColumn(
                name: "normalized_label",
                schema: "custom_data_view",
                table: "tile_fields");

            migrationBuilder.DropColumn(
                name: "hidden",
                schema: "custom_data_view",
                table: "tile_fields");
        }
    }
}
