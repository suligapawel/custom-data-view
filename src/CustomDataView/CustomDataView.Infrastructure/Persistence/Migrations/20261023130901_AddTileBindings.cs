using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomDataView.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTileBindings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tile_bindings",
                schema: "custom_data_view",
                columns: table => new
                {
                    tile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_node = table.Column<string>(type: "character varying(110)", maxLength: 110, nullable: false),
                    target_kind = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    target_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    source_node = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    source_field = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    source_scope = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    position = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tile_bindings", x => new { x.tile_id, x.target_node, x.target_kind, x.target_name });
                    table.CheckConstraint("ck_tile_bindings_nodes", "source_node <> target_node");
                    table.CheckConstraint("ck_tile_bindings_position", "position >= 0");
                    table.CheckConstraint("ck_tile_bindings_source_scope", "source_scope IN ('selected', 'all', 'item')");
                    table.CheckConstraint("ck_tile_bindings_target_kind", "target_kind IN ('parameter', 'input')");
                    table.ForeignKey(
                        name: "fk_tile_bindings_tile_id",
                        column: x => x.tile_id,
                        principalSchema: "custom_data_view",
                        principalTable: "tiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tile_bindings",
                schema: "custom_data_view");
        }
    }
}
