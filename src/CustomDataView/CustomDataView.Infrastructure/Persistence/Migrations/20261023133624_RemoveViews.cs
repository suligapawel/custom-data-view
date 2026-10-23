using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomDataView.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveViews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "view_tiles",
                schema: "custom_data_view");

            migrationBuilder.DropTable(
                name: "views",
                schema: "custom_data_view");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "views",
                schema: "custom_data_view",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_views", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "view_tiles",
                schema: "custom_data_view",
                columns: table => new
                {
                    view_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    css_class = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    position = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_view_tiles", x => new { x.view_id, x.tile_id });
                    table.UniqueConstraint("uq_view_tiles_view_id_position", x => new { x.view_id, x.position });
                    table.CheckConstraint("ck_view_tiles_position", "position >= 0");
                    table.ForeignKey(
                        name: "fk_view_tiles_tile_id",
                        column: x => x.tile_id,
                        principalSchema: "custom_data_view",
                        principalTable: "tiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_view_tiles_view_id",
                        column: x => x.view_id,
                        principalSchema: "custom_data_view",
                        principalTable: "views",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_view_tiles_tile_id",
                schema: "custom_data_view",
                table: "view_tiles",
                column: "tile_id");

            migrationBuilder.CreateIndex(
                name: "ix_views_created_at_id",
                schema: "custom_data_view",
                table: "views",
                columns: new[] { "created_at", "id" });
        }
    }
}
