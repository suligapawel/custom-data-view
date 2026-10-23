# #TEMP-040 — Scenariusz implementacji

## Cel

Ścieżka JSONPath pola i elementów kafelka obsługuje segment filtra z porównaniem `==`/`!=` wartości pod ścieżką
względną `@` z literałem. Filtr, tak jak wildcard, zwraca płaską tablicę dopasowań. Wyrażenia logiczne (`&&`, `||`,
`!`), porównania `<`/`>`, test istnienia (`[?@.x]`), funkcje i ścieżki `$` wewnątrz filtra są poza MVP.

## Testy

Domena: parser i ewaluacja ścieżki (`JsonPathParser`, `JsonPath`).
API: `src/CustomDataView/docs/scenarios/scenario_TEMP-040/gherkin.md`.
