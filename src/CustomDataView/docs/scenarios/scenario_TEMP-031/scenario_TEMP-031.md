# #TEMP-031 — Scenariusz implementacji

## Cel

Brakujący albo niepoprawny parametr jednego połączenia nie blokuje podglądu: pola tego połączenia dostają błąd
`invalid_parameters`, a pozostałe pola zwracają dane. Zastępuje to odrzucenie podglądu kafelka (TEMP-015-023 #37, #51)
i błąd całego kafelka w podglądzie widoku (TEMP-029 #3).

## Domena i aplikacja

- `TilePreviewer.Call` waliduje każde połączenie osobno (`ParameterValuesValidator`) tuż przed wywołaniem; błąd →
  wynik połączenia z błędem pól `invalid_parameters`, bez wywołania.
- `TileFieldError` dostaje `Parameters` (`TileParameterError(Name, Message, Bound)`).
- `ViewTileError`, `ViewTileErrorCodes` i obsługa `ValidationException` w podglądzie widoku znikają.

## Testy

API: `src/CustomDataView/docs/scenarios/scenario_TEMP-031/gherkin.md`.
