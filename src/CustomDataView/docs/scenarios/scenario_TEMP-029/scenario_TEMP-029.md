> Wycofane (2026-10-23): widoki usunięto na rzecz kafelków złożonych — patrz TEMP-038. Dokument zostaje jako historia.

# #TEMP-029 — Scenariusz implementacji

## Cel

Użytkownik może pobrać widok razem z danymi jego kafelków: podaje jedną listę parametrów, a każdy kafelek widoku
otrzymuje ją i zwraca swój podgląd. Dzięki temu widać cały skonfigurowany układ z danymi jednym żądaniem.

## Kontekst biznesowy

Widoki (TEMP-024 … TEMP-028) przechowują tylko układ, a podgląd kafelka (TEMP-021 … TEMP-023) pokazuje dane jednego
kafelka. Użytkownik chce sprawdzić cały widok naraz — z tymi samymi parametrami (np. `city`) przekazanymi niżej do
wszystkich kafelków. Zmiana granic PRD: „podgląd całego widoku z danymi” przestaje być wykluczony (decyzja dewelopera).

Błąd parametrów jednego kafelka (np. brak `city`, którego wymaga tylko jego połączenie) nie może blokować pozostałych
kafelków — dotyczy wyłącznie tego kafelka, a odpowiedź ma status 200 (decyzja dewelopera). Każdy kafelek wykonuje
własny podgląd, więc połączenie wspólne dla kilku kafelków może zostać wywołane kilka razy (decyzja dewelopera).

## Domena

Folder `CustomDataView.Core/Views/Preview/`:

- `IViewPreviewService`, `ViewPreviewService`, `ViewPreview`, `ViewTilePreview`, `ViewTileError`,
  `ViewTileErrorCodes`.

```csharp
public sealed record ViewTileError(string Code, IDictionary<string, string[]> Errors);

public sealed record ViewTilePreview(
    Guid TileId,
    string TileName,
    int Position,
    string CssClass,
    IReadOnlyList<TileFieldPreview> Fields,
    ViewTileError Error);

public sealed record ViewPreview(Guid Id, string Name, IReadOnlyList<ViewTilePreview> Tiles);
```

`ViewPreviewService` (singleton) przyjmuje `IViewRepository`, `ITileRepository`, `ITilePreviewService`:

1. `Preview(id, parameters, ct)`: brak widoku → `NotFoundException(id)`.
2. Kafelki widoku w kolejności `Position`; dla każdego `ITilePreviewService.Preview(tileId, parameters, ct)` — wszystkie
   równolegle (`Task.WhenAll`), każdy z tą samą płaską mapą parametrów.
3. Sukces kafelka → `Fields` z `TilePreview.Fields`, `Error = null`.
4. `ValidationException` z podglądu kafelka (błędy `parameters.<nazwa>`) → `Fields = null`,
   `Error = ViewTileError("invalid_parameters", errors)`; żadne połączenie tego kafelka nie jest wywoływane
   (zachowanie podglądu kafelka), pozostałe kafelki nie są dotknięte.
5. `NotFoundException` kafelka (usunięty w trakcie) → kafelek jest pomijany.
6. Nazwa kafelka z `TilePreview.Name`. Błędy pól (`source_unavailable`, `path_not_found`, `transformation_failed`)
   pochodzą bez zmian z podglądu kafelka.

Nic nie jest zapisywane ani logowane.

## Kryteria akceptacji

- [ ] Podgląd zwraca id i nazwę widoku oraz kafelki w kolejności pozycji z `tileId`, `tileName`, `position`, `cssClass`
  i `fields` (etykieta i przetransformowana wartość albo błąd pola).
- [ ] Parametry z płaskiej mapy trafiają do wszystkich kafelków (np. `customer_id` do kafelka „Klient”, `currency` do
  kafelka „Kurs walut”).
- [ ] Ten sam podgląd metodą GET (parametry w adresie, powtórzony parametr = lista) daje ten sam wynik co POST.
- [ ] Brak wymaganego parametru połączenia jednego kafelka → ten kafelek ma `error` z kodem `invalid_parameters`
  i kluczem `parameters.<nazwa>`, bez `fields`; jego połączenie nie jest wywoływane; pozostałe kafelki mają dane;
  status 200.
- [ ] Niedostępne źródło jednego kafelka daje błąd `source_unavailable` w jego polach; pozostałe kafelki mają dane.
- [ ] Połączenie wspólne dla dwóch kafelków jest wywoływane osobno dla każdego kafelka.
- [ ] Widok bez kafelków → 200 z pustą listą `tiles`.
- [ ] Podgląd nieistniejącego widoku → 404 (POST i GET).
- [ ] Podgląd nie zmienia widoku (szczegóły bez zmian).

## Endpoint

Plik `CustomDataView.Api/Endpoints/ViewEndpoints.cs`. Wartości parametrów jak w podglądzie kafelka
(`TilePreviewRequest.ValuesOf`).

| Metoda i ścieżka               | Wejście                   | Sukces          | Błędy |
|--------------------------------|---------------------------|-----------------|-------|
| `POST /{id:guid}/preview`      | `{ "parameters": { … } }` | 200 podgląd     | 404   |
| `GET /{id:guid}/preview`       | `?nazwa=wartość&…`        | 200 podgląd     | 404   |

Wynik (200):

```json
{
  "id": "…",
  "name": "Pulpit klienta",
  "tiles": [
    {
      "tileId": "…", "tileName": "Klient", "position": 0, "cssClass": "col-6",
      "fields": [ { "label": "Nazwa", "value": "acme" } ]
    },
    {
      "tileId": "…", "tileName": "Kurs walut", "position": 1, "cssClass": null,
      "error": { "code": "invalid_parameters", "errors": { "parameters.currency": [ "…" ] } }
    }
  ]
}
```

Kafelek ma `fields` albo `error`, nigdy oba (puste właściwości są pomijane).

## Walidacja wejścia

- `parameters`: opcjonalna płaska mapa `nazwa → wartość JSON`; reguły wartości jak w podglądzie kafelka, ale ich
  naruszenie daje błąd kafelka, nie 400.
- `id` w ścieżce: Guid (ograniczenie trasy; niepoprawny → 404).

## Handler i sekwencja wykonania

1. Endpoint: `POST` → `TilePreviewRequest.ToInput()`; `GET` → `TilePreviewRequest.ValuesOf(query)`.
2. `ViewPreviewService.Preview(id, parameters, ct)`: brak widoku → 404.
3. Równoległy podgląd każdego kafelka przez `ITilePreviewService`; `ValidationException` → błąd kafelka.
4. 200 z `ViewPreview`.

`AddCustomDataViewViews()` rejestruje dodatkowo `IViewPreviewService`; `MapViewEndpoints()` mapuje oba endpointy.

## Baza danych

Brak zmian.

## Repozytoria

Brak zmian.

## Frontend

Brak zmian.

## Testy

### Testy jednostkowe

Brak — logika składa istniejące serwisy; zachowanie pokrywają testy integracyjne.

### Testy integracyjne

> Testy integracyjne dla tego scenariusza są pisane na podstawie scenariuszy Gherkin w
> `src/CustomDataView/docs/scenarios/scenario_TEMP-029/gherkin.md` (API przez `CustomDataViewApplicationFactory`,
> `FakeExternalApi` liczący wywołania, repozytoria w pamięci).
