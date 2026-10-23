# #TEMP-037 — Scenariusz implementacji

## Cel

Dziecko `scope: item` ma instancję na każdy element rodzica; `parent` ze `scope: item` przekazuje pole bieżącego
elementu, a rodzeństwo `scope: item` wiąże się z instancjami tego samego elementu. Limity chronią przed lawiną wywołań.

## Domena i aplikacja

- `TilePreviewer`: krawędź `items.source` → dziecko `item`; instancje per element z adresem `[klucz]/dziecko`;
  limit 50 instancji `eager` na osadzenie.
- `PreviewContext`: deduplikacja wywołań (połączenie + wartości po zamianie na tekst), budżet 200 wywołań
  (rezerwowany przed startem osadzenia według liczby jego połączeń), limit 10 s (`TimeProvider`) i 8 równoległych
  wywołań instancji dzieci.
- Walidacja: `item` tylko z `parent` do dziecka `item`; z dziecka `item` tylko do rodzeństwa `item`.

## Testy

API: `src/CustomDataView/docs/scenarios/scenario_TEMP-037/gherkin.md` (czas przez testowy `TimeProvider`).
