# #TEMP-038 — Scenariusz implementacji

## Cel

Widoki (TEMP-024 … TEMP-029) zostają usunięte decyzją dewelopera z 2026-10-23 — w miejsce etapu „wiązania widoku”
z przekazania. Kafelek złożony jest korzeniem drzewa osadzeń, więc osobna encja widoku nie jest potrzebna.

## Zmiany

- Usunięte: endpointy `/api/custom-data-view/views`, `Views` w warstwie aplikacji, `View`, `ViewDefinition`,
  `ViewTile`, `IViewRepository`, `PostgresViewRepository`, wiersze i konfiguracje EF, testy widoków.
- Migracja `RemoveViews`: `DROP TABLE view_tiles, views` (dane widoków zostają utracone; `Down` odtwarza puste tabele).
- `TileInUseException` i `FieldInUseException` zwracają tylko kafelki.

## Testy

API: `src/CustomDataView/docs/scenarios/scenario_TEMP-038/gherkin.md`.
