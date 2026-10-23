CREATE SCHEMA IF NOT EXISTS custom_data_view;

CREATE TABLE custom_data_view.connections
(
    id         uuid         NOT NULL,
    name       varchar(100) NOT NULL,
    method     varchar(4)   NOT NULL,
    url        text         NOT NULL,
    body       json         NULL,
    created_at timestamptz  NOT NULL DEFAULT now(),
    updated_at timestamptz  NOT NULL DEFAULT now(),
    CONSTRAINT pk_connections PRIMARY KEY (id),
    CONSTRAINT ck_connections_method CHECK (method IN ('GET', 'POST')),
    CONSTRAINT ck_connections_body CHECK (method = 'POST' OR body IS NULL)
);

CREATE INDEX ix_connections_created_at_id ON custom_data_view.connections (created_at, id);

CREATE TABLE custom_data_view.connection_path_parameters
(
    connection_id uuid        NOT NULL,
    name          varchar(50) NOT NULL,
    required      boolean     NOT NULL,
    default_value text        NULL,
    CONSTRAINT pk_connection_path_parameters PRIMARY KEY (connection_id, name),
    CONSTRAINT fk_connection_path_parameters_connection_id FOREIGN KEY (connection_id)
        REFERENCES custom_data_view.connections (id) ON DELETE CASCADE
);

CREATE TABLE custom_data_view.connection_query_parameters
(
    connection_id uuid    NOT NULL,
    name          text    NOT NULL,
    required      boolean NOT NULL,
    default_value text    NULL,
    CONSTRAINT pk_connection_query_parameters PRIMARY KEY (connection_id, name),
    CONSTRAINT fk_connection_query_parameters_connection_id FOREIGN KEY (connection_id)
        REFERENCES custom_data_view.connections (id) ON DELETE CASCADE
);

CREATE TABLE custom_data_view.connection_body_parameters
(
    connection_id uuid        NOT NULL,
    name          varchar(50) NOT NULL,
    required      boolean     NOT NULL,
    default_value json        NULL,
    CONSTRAINT pk_connection_body_parameters PRIMARY KEY (connection_id, name),
    CONSTRAINT fk_connection_body_parameters_connection_id FOREIGN KEY (connection_id)
        REFERENCES custom_data_view.connections (id) ON DELETE CASCADE
);
