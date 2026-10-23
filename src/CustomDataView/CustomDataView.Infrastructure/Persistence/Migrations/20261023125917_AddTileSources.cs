using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomDataView.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTileSources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tile_sources",
                schema: "custom_data_view",
                columns: table => new
                {
                    tile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    kind = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    connection_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tile_sources", x => new { x.tile_id, x.key });
                    table.UniqueConstraint("uq_tile_sources_tile_id_position", x => new { x.tile_id, x.position });
                    table.CheckConstraint("ck_tile_sources_connection", "(kind = 'connection' AND connection_id IS NOT NULL) OR (kind <> 'connection' AND connection_id IS NULL)");
                    table.CheckConstraint("ck_tile_sources_kind", "kind IN ('connection', 'input')");
                    table.CheckConstraint("ck_tile_sources_position", "position >= 0");
                    table.ForeignKey(
                        name: "fk_tile_sources_connection_id",
                        column: x => x.connection_id,
                        principalSchema: "custom_data_view",
                        principalTable: "connections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tile_sources_tile_id",
                        column: x => x.tile_id,
                        principalSchema: "custom_data_view",
                        principalTable: "tiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO custom_data_view.tile_sources (tile_id, key, position, kind, connection_id)
                SELECT
                    tile_id,
                    'source_' || row_number() OVER (PARTITION BY tile_id ORDER BY first_position),
                    row_number() OVER (PARTITION BY tile_id ORDER BY first_position) - 1,
                    'connection',
                    connection_id
                FROM (
                    SELECT tile_id, connection_id, min(position) AS first_position
                    FROM custom_data_view.tile_fields
                    GROUP BY tile_id, connection_id
                ) used;
                """);

            migrationBuilder.AddColumn<string>(
                name: "source_key",
                schema: "custom_data_view",
                table: "tile_fields",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE custom_data_view.tile_fields field
                SET source_key = source.key
                FROM custom_data_view.tile_sources source
                WHERE source.tile_id = field.tile_id AND source.connection_id = field.connection_id;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "source_key",
                schema: "custom_data_view",
                table: "tile_fields",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.DropForeignKey(
                name: "fk_tile_fields_connection_id",
                schema: "custom_data_view",
                table: "tile_fields");

            migrationBuilder.DropIndex(
                name: "ix_tile_fields_connection_id",
                schema: "custom_data_view",
                table: "tile_fields");

            migrationBuilder.DropColumn(
                name: "connection_id",
                schema: "custom_data_view",
                table: "tile_fields");

            migrationBuilder.CreateIndex(
                name: "ix_tile_fields_tile_id_source_key",
                schema: "custom_data_view",
                table: "tile_fields",
                columns: new[] { "tile_id", "source_key" });

            migrationBuilder.CreateIndex(
                name: "ix_tile_sources_connection_id",
                schema: "custom_data_view",
                table: "tile_sources",
                column: "connection_id");

            migrationBuilder.AddForeignKey(
                name: "fk_tile_fields_source",
                schema: "custom_data_view",
                table: "tile_fields",
                columns: new[] { "tile_id", "source_key" },
                principalSchema: "custom_data_view",
                principalTable: "tile_sources",
                principalColumns: new[] { "tile_id", "key" },
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "connection_id",
                schema: "custom_data_view",
                table: "tile_fields",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE custom_data_view.tile_fields field
                SET connection_id = source.connection_id
                FROM custom_data_view.tile_sources source
                WHERE source.tile_id = field.tile_id AND source.key = field.source_key;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "connection_id",
                schema: "custom_data_view",
                table: "tile_fields",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.DropForeignKey(
                name: "fk_tile_fields_source",
                schema: "custom_data_view",
                table: "tile_fields");

            migrationBuilder.DropIndex(
                name: "ix_tile_fields_tile_id_source_key",
                schema: "custom_data_view",
                table: "tile_fields");

            migrationBuilder.DropColumn(
                name: "source_key",
                schema: "custom_data_view",
                table: "tile_fields");

            migrationBuilder.DropTable(
                name: "tile_sources",
                schema: "custom_data_view");

            migrationBuilder.CreateIndex(
                name: "ix_tile_fields_connection_id",
                schema: "custom_data_view",
                table: "tile_fields",
                column: "connection_id");

            migrationBuilder.AddForeignKey(
                name: "fk_tile_fields_connection_id",
                schema: "custom_data_view",
                table: "tile_fields",
                column: "connection_id",
                principalSchema: "custom_data_view",
                principalTable: "connections",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
