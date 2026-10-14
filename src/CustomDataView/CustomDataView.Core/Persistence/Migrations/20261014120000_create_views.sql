CREATE TABLE custom_data_view.views
(
    id         uuid         NOT NULL,
    name       varchar(100) NOT NULL,
    created_at timestamptz  NOT NULL DEFAULT now(),
    updated_at timestamptz  NOT NULL DEFAULT now(),
    CONSTRAINT pk_views PRIMARY KEY (id)
);

CREATE INDEX ix_views_created_at_id ON custom_data_view.views (created_at, id);

CREATE TABLE custom_data_view.view_tiles
(
    view_id   uuid         NOT NULL,
    tile_id   uuid         NOT NULL,
    position  integer      NOT NULL,
    css_class varchar(200) NULL,
    CONSTRAINT pk_view_tiles PRIMARY KEY (view_id, tile_id),
    CONSTRAINT uq_view_tiles_view_id_position UNIQUE (view_id, position),
    CONSTRAINT ck_view_tiles_position CHECK (position >= 0),
    CONSTRAINT fk_view_tiles_view_id FOREIGN KEY (view_id)
        REFERENCES custom_data_view.views (id) ON DELETE CASCADE,
    CONSTRAINT fk_view_tiles_tile_id FOREIGN KEY (tile_id)
        REFERENCES custom_data_view.tiles (id) ON DELETE CASCADE
);

CREATE INDEX ix_view_tiles_tile_id ON custom_data_view.view_tiles (tile_id);
