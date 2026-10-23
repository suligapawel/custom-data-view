using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomDataView.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
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
                    display = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    items_source_key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    items_path = table.Column<string>(type: "text", nullable: true),
                    items_key_label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tiles", x => x.id);
                    table.CheckConstraint("ck_tiles_display", "display IN ('table', 'list', 'details', 'summary', 'form')");
                    table.CheckConstraint("ck_tiles_items", "(display IN ('table', 'list')) = (items_source_key IS NOT NULL AND items_path IS NOT NULL AND items_key_label IS NOT NULL) AND (display IN ('table', 'list') OR (items_source_key IS NULL AND items_path IS NULL AND items_key_label IS NULL))");
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

            migrationBuilder.CreateTable(
                name: "tile_fields",
                schema: "custom_data_view",
                columns: table => new
                {
                    tile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    source_key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    path = table.Column<string>(type: "text", nullable: false),
                    label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    normalized_label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, computedColumnSql: "lower(btrim(label))", stored: true),
                    hidden = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    role = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tile_fields", x => new { x.tile_id, x.position });
                    table.CheckConstraint("ck_tile_fields_position", "position >= 0");
                    table.CheckConstraint("ck_tile_fields_role", "role IN ('title', 'subtitle', 'badge', 'primary', 'secondary')");
                    table.ForeignKey(
                        name: "fk_tile_fields_source",
                        columns: x => new { x.tile_id, x.source_key },
                        principalSchema: "custom_data_view",
                        principalTable: "tile_sources",
                        principalColumns: new[] { "tile_id", "key" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_tile_fields_tile_id",
                        column: x => x.tile_id,
                        principalSchema: "custom_data_view",
                        principalTable: "tiles",
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
                name: "ix_tile_children_child_tile_id",
                schema: "custom_data_view",
                table: "tile_children",
                column: "child_tile_id");

            migrationBuilder.CreateIndex(
                name: "ix_tile_fields_tile_id_source_key",
                schema: "custom_data_view",
                table: "tile_fields",
                columns: new[] { "tile_id", "source_key" });

            migrationBuilder.CreateIndex(
                name: "ux_tile_fields_tile_id_normalized_label",
                schema: "custom_data_view",
                table: "tile_fields",
                columns: new[] { "tile_id", "normalized_label" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_tile_form_fields_tile_id_normalized_label",
                schema: "custom_data_view",
                table: "tile_form_fields",
                columns: new[] { "tile_id", "normalized_label" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tile_sources_connection_id",
                schema: "custom_data_view",
                table: "tile_sources",
                column: "connection_id");

            migrationBuilder.CreateIndex(
                name: "ix_tiles_created_at_id",
                schema: "custom_data_view",
                table: "tiles",
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
                name: "tile_bindings",
                schema: "custom_data_view");

            migrationBuilder.DropTable(
                name: "tile_children",
                schema: "custom_data_view");

            migrationBuilder.DropTable(
                name: "tile_field_transformation_parameters",
                schema: "custom_data_view");

            migrationBuilder.DropTable(
                name: "tile_form_fields",
                schema: "custom_data_view");

            migrationBuilder.DropTable(
                name: "tile_field_transformations",
                schema: "custom_data_view");

            migrationBuilder.DropTable(
                name: "tile_fields",
                schema: "custom_data_view");

            migrationBuilder.DropTable(
                name: "tile_sources",
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
