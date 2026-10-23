using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomDataView.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFieldCombinations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "source_key",
                schema: "custom_data_view",
                table: "tile_fields",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "path",
                schema: "custom_data_view",
                table: "tile_fields",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateTable(
                name: "tile_field_combinations",
                schema: "custom_data_view",
                columns: table => new
                {
                    tile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    field_label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    parameter = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tile_field_combinations", x => new { x.tile_id, x.field_label });
                    table.ForeignKey(
                        name: "fk_tile_field_combinations_field",
                        columns: x => new { x.tile_id, x.field_label },
                        principalSchema: "custom_data_view",
                        principalTable: "tile_fields",
                        principalColumns: new[] { "tile_id", "label" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tile_field_combination_fields",
                schema: "custom_data_view",
                columns: table => new
                {
                    tile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    field_label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    input_label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tile_field_combination_fields", x => new { x.tile_id, x.field_label, x.position });
                    table.CheckConstraint("ck_tile_field_combination_fields_position", "position >= 0");
                    table.ForeignKey(
                        name: "fk_tile_field_combination_fields_combination",
                        columns: x => new { x.tile_id, x.field_label },
                        principalSchema: "custom_data_view",
                        principalTable: "tile_field_combinations",
                        principalColumns: new[] { "tile_id", "field_label" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_tile_fields_source",
                schema: "custom_data_view",
                table: "tile_fields",
                sql: "(source_key IS NULL) = (path IS NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tile_field_combination_fields",
                schema: "custom_data_view");

            migrationBuilder.DropTable(
                name: "tile_field_combinations",
                schema: "custom_data_view");

            migrationBuilder.DropCheckConstraint(
                name: "ck_tile_fields_source",
                schema: "custom_data_view",
                table: "tile_fields");

            migrationBuilder.AlterColumn<string>(
                name: "source_key",
                schema: "custom_data_view",
                table: "tile_fields",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "path",
                schema: "custom_data_view",
                table: "tile_fields",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
