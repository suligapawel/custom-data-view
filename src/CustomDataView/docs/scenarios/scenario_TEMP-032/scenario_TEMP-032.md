# #TEMP-032 — Scenariusz implementacji

## Cel

Kafelek ma wymagany typ wyświetlania (`table`, `list`, `details`, `summary`; `form` w TEMP-035), z którego front wie,
jak go pokazać. Kafelki z kolekcją (`table`, `list`) wskazują elementy (`items`: źródło, ścieżka z wildcardem,
etykieta pola-klucza), a pola są wtedy względne wobec elementu. Role pól (`title`, `subtitle`, `badge`, `primary`,
`secondary`) zależą od typu.

## Domena i aplikacja

- `TileDefinition(Name, Display, Sources, Items, Fields)`, `TileItems(Source, Path, Key)`, `TileField.Role`;
  słowniki `TileDisplays`, `TileFieldRoles`.
- `TileDefinitionValidator`: reguły `display`, ról i `items`.
- `TilePreview`: `display`, `fields` albo `itemKey` + `items` (`TileItemPreview(Key, Fields)`) albo `itemsError`.

## Baza danych

Migracja `AddTileDisplayAndItems`: `tiles.display` (istniejące → `details`), `items_source_key`, `items_path`,
`items_key_label` z CHECK `ck_tiles_items`, `tile_fields.role` z CHECK `ck_tile_fields_role`.

## Testy

API: `src/CustomDataView/docs/scenarios/scenario_TEMP-032/gherkin.md`; repozytorium: zapis i odczyt typu, elementów
i ról.
