# #TEMP-036 — Scenariusz implementacji

## Cel

Kafelek może osadzać inne samodzielne kafelki (`children`, `scope: tile`) z własnym typem wyświetlania i wiązać je
z rodzicem: `parent` → dziecko, dziecko → dziecko, dziecko → źródło (np. formularz filtrujący połączenie) i cele
wskazane ścieżką do potomków. Podgląd wykonuje dzieci rekurencyjnie i zwraca osadzenia ze stanem.

## Domena i aplikacja

- `TileChild(Key, TileId, Position, CssClass, Scope, Load)`, `TileDefinition.Children`.
- `TileReferencesValidator`: istnienie dzieci, cykle zawierania, limit 3 poziomów (także dla przodków), wiązania na
  podstawie kontraktów dzieci (`TileContracts`).
- `TilePreviewer`: wspólny graf źródeł i dzieci (fale algorytmem Kahna); stany osadzeń `data`, `awaiting_input`,
  `awaiting_load`, `error` z `address`, `inputs` i `pending`; `descendants` w żądaniu.
- Blokady: `TileInUseException` (409 `tile_in_use`) i `FieldInUseException` (409 `field_in_use`).

## Baza danych

Migracja `AddTileChildren`: tabela `tile_children` (FK do rodzica `CASCADE`, do dziecka `RESTRICT`).

## Testy

API: `src/CustomDataView/docs/scenarios/scenario_TEMP-036/gherkin.md`; repozytorium: zapis dzieci, rodzice
i użycia pól.
