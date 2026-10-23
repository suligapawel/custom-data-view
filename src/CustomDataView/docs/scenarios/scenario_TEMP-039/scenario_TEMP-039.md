# #TEMP-039 — Scenariusz implementacji

## Cel

Wiązanie z zakresem `selected` (domyślnym dla pola węzła z elementami) wykonuje front po wyborze elementu. Serwer
zwraca cel w stanie `awaiting_input` z `pending` (adres węzła źródłowego, pole, zakres, parametr albo wejście)
i kompletem pozostałych `inputs`, więc front woła podgląd kafelka osadzonego bez znajomości definicji rodzica.
Symulacja wyboru na serwerze (`selection`) jest poza MVP (decyzja dewelopera).

## Testy

API: `src/CustomDataView/docs/scenarios/scenario_TEMP-039/gherkin.md`.
