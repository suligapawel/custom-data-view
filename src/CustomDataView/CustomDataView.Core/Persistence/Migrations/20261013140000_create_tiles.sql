CREATE TABLE custom_data_view.tiles
(
    id         uuid         NOT NULL,
    name       varchar(100) NOT NULL,
    created_at timestamptz  NOT NULL DEFAULT now(),
    updated_at timestamptz  NOT NULL DEFAULT now(),
    CONSTRAINT pk_tiles PRIMARY KEY (id)
);

CREATE INDEX ix_tiles_created_at_id ON custom_data_view.tiles (created_at, id);
