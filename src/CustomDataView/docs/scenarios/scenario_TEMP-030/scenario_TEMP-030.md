# #TEMP-030 — Scenariusz implementacji

## Cel

Etykieta pola jest unikalna w kafelku, bo wiązania wskazują pola etykietą. Pole może być ukryte (`hidden`), aby
przenosić wartości techniczne (np. surowy JSON dla wejścia danych dziecka) bez pokazywania ich w widoku.

## Domena i aplikacja

- `TileField` dostaje `Hidden`; `TileFieldInput`, `TileFieldRequest`, `TileFieldDto` — `hidden` (bool, brak = false).
- `TileDefinitionValidator`: etykieta porównywana po `Trim()` i `ToLowerInvariant()` (`TileLabels.Normalize`);
  duplikat → `fields[i].label` „Etykieta pola musi być unikalna w kafelku.”.
- `TileFieldPreview` dostaje `Hidden`; konwerter zapisuje `"hidden": true` tylko dla pól ukrytych.
- Podgląd widoku filtruje pola ukryte.

## Baza danych

Migracja EF Core `AddTileFieldHiddenAndNormalizedLabel`: `hidden boolean NOT NULL DEFAULT false`,
`normalized_label` generowana `lower(btrim(label))` i indeks unikalny `ux_tile_fields_tile_id_normalized_label`.
Przed jej uruchomieniem deweloper sprawdza duplikaty zapytaniem z db-plan („Migracje danych”).

## Testy

API: `src/CustomDataView/docs/scenarios/scenario_TEMP-030/gherkin.md`; repozytorium: zapis i odczyt `hidden`
oraz odrzucenie duplikatu etykiety przez bazę.
