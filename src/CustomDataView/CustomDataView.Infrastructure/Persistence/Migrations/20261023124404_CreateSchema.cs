using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomDataView.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CreateSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "custom_data_view");

            migrationBuilder.CreateTable(
                name: "connections",
                schema: "custom_data_view",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    method = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    url = table.Column<string>(type: "text", nullable: false),
                    body = table.Column<string>(type: "json", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_connections", x => x.id);
                    table.CheckConstraint("ck_connections_body", "method = 'POST' OR body IS NULL");
                    table.CheckConstraint("ck_connections_method", "method IN ('GET', 'POST')");
                });

            migrationBuilder.CreateTable(
                name: "tiles",
                schema: "custom_data_view",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tiles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "views",
                schema: "custom_data_view",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_views", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "connection_body_parameters",
                schema: "custom_data_view",
                columns: table => new
                {
                    connection_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    required = table.Column<bool>(type: "boolean", nullable: false),
                    default_value = table.Column<string>(type: "json", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_connection_body_parameters", x => new { x.connection_id, x.name });
                    table.ForeignKey(
                        name: "fk_connection_body_parameters_connection_id",
                        column: x => x.connection_id,
                        principalSchema: "custom_data_view",
                        principalTable: "connections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "connection_path_parameters",
                schema: "custom_data_view",
                columns: table => new
                {
                    connection_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    required = table.Column<bool>(type: "boolean", nullable: false),
                    default_value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_connection_path_parameters", x => new { x.connection_id, x.name });
                    table.ForeignKey(
                        name: "fk_connection_path_parameters_connection_id",
                        column: x => x.connection_id,
                        principalSchema: "custom_data_view",
                        principalTable: "connections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "connection_query_parameters",
                schema: "custom_data_view",
                columns: table => new
                {
                    connection_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    required = table.Column<bool>(type: "boolean", nullable: false),
                    default_value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_connection_query_parameters", x => new { x.connection_id, x.name });
                    table.ForeignKey(
                        name: "fk_connection_query_parameters_connection_id",
                        column: x => x.connection_id,
                        principalSchema: "custom_data_view",
                        principalTable: "connections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tile_fields",
                schema: "custom_data_view",
                columns: table => new
                {
                    tile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    connection_id = table.Column<Guid>(type: "uuid", nullable: false),
                    path = table.Column<string>(type: "text", nullable: false),
                    label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tile_fields", x => new { x.tile_id, x.position });
                    table.CheckConstraint("ck_tile_fields_position", "position >= 0");
                    table.ForeignKey(
                        name: "fk_tile_fields_connection_id",
                        column: x => x.connection_id,
                        principalSchema: "custom_data_view",
                        principalTable: "connections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tile_fields_tile_id",
                        column: x => x.tile_id,
                        principalSchema: "custom_data_view",
                        principalTable: "tiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "view_tiles",
                schema: "custom_data_view",
                columns: table => new
                {
                    view_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    css_class = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
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

            migrationBuilder.CreateTable(
                name: "tile_field_transformations",
                schema: "custom_data_view",
                columns: table => new
                {
                    tile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    field_position = table.Column<int>(type: "integer", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tile_field_transformations", x => new { x.tile_id, x.field_position, x.position });
                    table.CheckConstraint("ck_tile_field_transformations_position", "position >= 0");
                    table.ForeignKey(
                        name: "fk_tile_field_transformations_field",
                        columns: x => new { x.tile_id, x.field_position },
                        principalSchema: "custom_data_view",
                        principalTable: "tile_fields",
                        principalColumns: new[] { "tile_id", "position" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tile_field_transformation_parameters",
                schema: "custom_data_view",
                columns: table => new
                {
                    tile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    field_position = table.Column<int>(type: "integer", nullable: false),
                    transformation_position = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    value = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tile_field_transformation_parameters", x => new { x.tile_id, x.field_position, x.transformation_position, x.name });
                    table.ForeignKey(
                        name: "fk_tile_field_transformation_parameters_transformation",
                        columns: x => new { x.tile_id, x.field_position, x.transformation_position },
                        principalSchema: "custom_data_view",
                        principalTable: "tile_field_transformations",
                        principalColumns: new[] { "tile_id", "field_position", "position" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_connections_created_at_id",
                schema: "custom_data_view",
                table: "connections",
                columns: new[] { "created_at", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_tile_fields_connection_id",
                schema: "custom_data_view",
                table: "tile_fields",
                column: "connection_id");

            migrationBuilder.CreateIndex(
                name: "ix_tiles_created_at_id",
                schema: "custom_data_view",
                table: "tiles",
                columns: new[] { "created_at", "id" });

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "connection_body_parameters",
                schema: "custom_data_view");

            migrationBuilder.DropTable(
                name: "connection_path_parameters",
                schema: "custom_data_view");

            migrationBuilder.DropTable(
                name: "connection_query_parameters",
                schema: "custom_data_view");

            migrationBuilder.DropTable(
                name: "tile_field_transformation_parameters",
                schema: "custom_data_view");

            migrationBuilder.DropTable(
                name: "view_tiles",
                schema: "custom_data_view");

            migrationBuilder.DropTable(
                name: "tile_field_transformations",
                schema: "custom_data_view");

            migrationBuilder.DropTable(
                name: "views",
                schema: "custom_data_view");

            migrationBuilder.DropTable(
                name: "tile_fields",
                schema: "custom_data_view");

            migrationBuilder.DropTable(
                name: "connections",
                schema: "custom_data_view");

            migrationBuilder.DropTable(
                name: "tiles",
                schema: "custom_data_view");
        }
    }
}
