using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomDataView.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTileFormFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tile_form_fields",
                schema: "custom_data_view",
                columns: table => new
                {
                    tile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    normalized_label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, computedColumnSql: "lower(btrim(label))", stored: true),
                    type = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    required = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    default_value = table.Column<string>(type: "json", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tile_form_fields", x => new { x.tile_id, x.position });
                    table.CheckConstraint("ck_tile_form_fields_position", "position >= 0");
                    table.CheckConstraint("ck_tile_form_fields_type", "type IN ('text', 'number', 'boolean', 'date')");
                    table.ForeignKey(
                        name: "fk_tile_form_fields_tile_id",
                        column: x => x.tile_id,
                        principalSchema: "custom_data_view",
                        principalTable: "tiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ux_tile_form_fields_tile_id_normalized_label",
                schema: "custom_data_view",
                table: "tile_form_fields",
                columns: new[] { "tile_id", "normalized_label" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tile_form_fields",
                schema: "custom_data_view");
        }
    }
}
