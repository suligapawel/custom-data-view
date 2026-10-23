# #TEMP-033 — Scenariusz implementacji

## Cel

Kafelek pobiera dane z nazwanych źródeł, a pola wskazują źródło. Wiązania źródło → źródło przekazują wartości pól
(po transformacjach) do parametrów innych połączeń; jedno wiązanie może przenosić wiele mapowań. Podgląd wykonuje
źródła falami według wspólnego mechanizmu grafu, który później obsługuje też dzieci i widok.

## Domena i aplikacja

- `TileSource(Key, Kind, ConnectionId)`, `TileField.Source`, `TileBinding(From, To, Map)`,
  `TileBindingMapping(Field, Parameter, Input, Scope)`.
- `TileDefinitionValidator` (kształt), `TileReferencesValidator` (połączenia, parametry, cykle).
- `Graph/BindingGraph`: węzły, krawędzie, wykrywanie cykli i podział na fale.
- `TilePreviewer`: wykonanie fal; mapa wartości węzła = wartości doręczone + wartości z wiązań (wiązanie wygrywa);
  `dependency_failed`, `invalid_binding`, `null` jako brak wartości, zamiana na tekst dla path i query.
- `TileContract` w `GET /tiles/{id}` (`contract`).

## Baza danych

Migracja `AddTileSources`: tabela `tile_sources`, migracja danych z `tile_fields.connection_id` (klucze `source_<n>`),
`tile_fields.source_key` z FK złożonym. Migracja `AddTileBindings`: tabela `tile_bindings`.

## Testy

API: `src/CustomDataView/docs/scenarios/scenario_TEMP-033/gherkin.md`; testy jednostkowe grafu; repozytorium.
