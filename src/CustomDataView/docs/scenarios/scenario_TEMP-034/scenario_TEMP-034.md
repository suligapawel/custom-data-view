# #TEMP-034 — Scenariusz implementacji

## Cel

Kafelek może przyjąć gotowy JSON przez źródło `input` (wejście danych z kontraktu), a żądanie podglądu kafelka
przyjmuje `parameters` (wartości testowe albo doręczone dla wejść-parametrów) i `inputs` (wejścia danych). Dzięki temu
kafelek osadzany jako dziecko (TEMP-036) da się sprawdzić samodzielnie.

## Domena i aplikacja

- `TileSourceKinds.Input`: źródło bez połączenia; walidator dopuszcza oba rodzaje.
- `TilePreviewValues(Parameters, Inputs)` w zapytaniach podglądu; `TilePreviewRequest` i `TileDraftPreviewRequest`
  przyjmują `inputs`. GET podglądu kafelka zostaje z płaskimi `parameters` w adresie (decyzja dewelopera).
- `TilePreviewer`: węzeł `input` bierze wartość z `inputs`; brak → błąd `invalid_parameters` dla wejścia.

## Testy

API: `src/CustomDataView/docs/scenarios/scenario_TEMP-034/gherkin.md`.
