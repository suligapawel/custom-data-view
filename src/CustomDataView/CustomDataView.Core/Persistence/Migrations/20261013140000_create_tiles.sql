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
