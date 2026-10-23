CREATE TABLE custom_data_view.tiles
(
    id         uuid         NOT NULL,
    name       varchar(100) NOT NULL,
    created_at timestamptz  NOT NULL DEFAULT now(),
    updated_at timestamptz  NOT NULL DEFAULT now(),
    CONSTRAINT pk_tiles PRIMARY KEY (id)
);

CREATE INDEX ix_tiles_created_at_id ON custom_data_view.tiles (created_at, id);

CREATE TABLE custom_data_view.tile_fields
(
    tile_id       uuid         NOT NULL,
    position      integer      NOT NULL,
    connection_id uuid         NOT NULL,
    path          text         NOT NULL,
    label         varchar(100) NOT NULL,
    CONSTRAINT pk_tile_fields PRIMARY KEY (tile_id, position),
    CONSTRAINT ck_tile_fields_position CHECK (position >= 0),
    CONSTRAINT fk_tile_fields_tile_id FOREIGN KEY (tile_id)
        REFERENCES custom_data_view.tiles (id) ON DELETE CASCADE,
    CONSTRAINT fk_tile_fields_connection_id FOREIGN KEY (connection_id)
        REFERENCES custom_data_view.connections (id) ON DELETE RESTRICT
);

CREATE INDEX ix_tile_fields_connection_id ON custom_data_view.tile_fields (connection_id);

CREATE TABLE custom_data_view.tile_field_transformations
(
    tile_id        uuid        NOT NULL,
    field_position integer     NOT NULL,
    position       integer     NOT NULL,
    key            varchar(50) NOT NULL,
    CONSTRAINT pk_tile_field_transformations PRIMARY KEY (tile_id, field_position, position),
    CONSTRAINT ck_tile_field_transformations_position CHECK (position >= 0),
    CONSTRAINT fk_tile_field_transformations_field FOREIGN KEY (tile_id, field_position)
        REFERENCES custom_data_view.tile_fields (tile_id, position) ON DELETE CASCADE
);

CREATE TABLE custom_data_view.tile_field_transformation_parameters
(
    tile_id                 uuid        NOT NULL,
    field_position          integer     NOT NULL,
    transformation_position integer     NOT NULL,
    name                    varchar(50) NOT NULL,
    value                   text        NOT NULL,
    CONSTRAINT pk_tile_field_transformation_parameters
        PRIMARY KEY (tile_id, field_position, transformation_position, name),
    CONSTRAINT fk_tile_field_transformation_parameters_transformation
        FOREIGN KEY (tile_id, field_position, transformation_position)
        REFERENCES custom_data_view.tile_field_transformations (tile_id, field_position, position) ON DELETE CASCADE
);
