using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomDataView.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTileChildren : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tile_children",
                schema: "custom_data_view",
                columns: table => new
                {
                    parent_tile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    child_tile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    css_class = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    scope = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    load = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tile_children", x => new { x.parent_tile_id, x.key });
                    table.UniqueConstraint("uq_tile_children_parent_tile_id_position", x => new { x.parent_tile_id, x.position });
                    table.CheckConstraint("ck_tile_children_load", "load IN ('eager', 'lazy')");
                    table.CheckConstraint("ck_tile_children_not_self", "parent_tile_id <> child_tile_id");
                    table.CheckConstraint("ck_tile_children_position", "position >= 0");
                    table.CheckConstraint("ck_tile_children_scope", "scope IN ('tile', 'item')");
                    table.ForeignKey(
                        name: "fk_tile_children_child_tile_id",
                        column: x => x.child_tile_id,
                        principalSchema: "custom_data_view",
                        principalTable: "tiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tile_children_parent_tile_id",
                        column: x => x.parent_tile_id,
                        principalSchema: "custom_data_view",
                        principalTable: "tiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_tile_children_child_tile_id",
                schema: "custom_data_view",
                table: "tile_children",
                column: "child_tile_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tile_children",
                schema: "custom_data_view");
        }
    }
}
