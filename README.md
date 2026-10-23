# CustomDataView

## Baza danych PostgreSQL w kontenerze

Uruchomienie kontenera (port `55432`, baza `custom_data_view_test`, użytkownik `postgres`, hasło `test`):

```bash
docker run -d --name custom-data-view-db \
  -p 55432:5432 \
  -e POSTGRES_DB=custom_data_view_test \
  -e POSTGRES_PASSWORD=test \
  postgres:17-alpine
```

Testy repozytoriów uruchamiają się, gdy zmienna `CUSTOM_DATA_VIEW_TEST_DATABASE` wskazuje bazę z sufiksem `_test`.
Każde uruchomienie usuwa schemat `custom_data_view` tej bazy i stosuje migracje od zera:

```bash
CUSTOM_DATA_VIEW_TEST_DATABASE="Host=localhost;Port=55432;Database=custom_data_view_test;Username=postgres;Password=test" \
  dotnet test CustomDataView.sln
```

## Migracje EF Core

Schemat tworzą migracje EF Core w `src/CustomDataView/CustomDataView.Infrastructure/Persistence/Migrations`
(historia w `custom_data_view.__ef_migrations_history`). Narzędzie `dotnet-ef` jest w manifeście repozytorium:

```bash
dotnet tool restore
dotnet ef migrations add <Nazwa> --project src/CustomDataView/CustomDataView.Infrastructure --output-dir Persistence/Migrations
dotnet ef database update --project src/CustomDataView/CustomDataView.Infrastructure --connection "<connection string>"
```

Jedna migracja `Initial` tworzy cały schemat od zera. Nie przenosi danych ze schematu utworzonego dawnymi
migracjami SQL (tam pola wskazywały połączenie, a widoki miały własne tabele).

Pełna lista kroków przed wdrożeniem jest w `docs/foundation/pre-deployment.md`.

## Model w skrócie

- **Połączenie** to szablon żądania REST z parametrami ścieżki, zapytania i treści.
- **Kafelek** to samodzielny komponent z typem wyświetlania `display`:

  | `display` | Dane | Wymaga |
  |---|---|---|
  | `table` | wiele elementów | `items` z `key` |
  | `list` | wiele elementów | `items` z `key`, pole z rolą `title` |
  | `details` | jeden rekord | — |
  | `summary` | jeden rekord | pole z rolą `primary` |
  | `form` | wartości od użytkownika | pola formularza (`type`, `required`, `default`) |

- **Źródła** kafelka: `connection` (pobiera dane połączeniem) albo `input` (gotowy JSON od rodzica lub z żądania).
  Pole wskazuje źródło (`source`); etykieta pola jest unikalna w kafelku (bez wielkości liter i białych znaków).
- **Dzieci** to osadzenia innych kafelków (`key`, `tileId`, `position`, `cssClass`, `scope`: `tile`/`item`,
  `load`: `eager`/`lazy`), najwyżej 3 poziomy. Każde dziecko ma własny `display`; pola rodzica wyświetlają się przed
  dziećmi. Kafelek złożony pełni rolę widoku — osobnych widoków nie ma.
- **Wiązania** (`bindings`) przekazują wartości pól wyłącznie jawnie: `{ "from", "to", "map": [ { "field",
  "parameter" | "input", "scope" } ] }`. Jedno wiązanie może mieć wiele mapowań. Węzły to źródła, dzieci i `parent`
  (pola samego kafelka); `to` może być ścieżką do wnuka (`shipping/carrier`). Zakresy: brak (wartość pola), `selected`
  (wybór użytkownika, wykonuje front), `all` (lista wartości ze wszystkich elementów), `item` (pole bieżącego elementu
  rodzica dla dziecka `scope: item`).
- **Pole ukryte** (`hidden: true`) przechodzi przez transformacje i może zasilać wiązania; podgląd zwraca je
  z oznaczeniem `"hidden": true`, a front go nie pokazuje.

## Przykład: panel pogodowy z dwoma dziećmi

Gotowe żądania są w `docs/foundation/rest/` (klient HTTP Ridera): `01-connections.http` tworzy połączenia,
`02-tiles.http` kafelki, a `03-preview.http` podglądy. Przykład korzysta z endpointów deweloperskich
`FakeWeatherEndpoints` (środowisko `Development`, `http://localhost:5080`):

| Endpoint | Dane |
|---|---|
| `GET /api/fake/weather/{city}/forecast?days=&units=` | prognoza na kolejne dni |
| `POST /api/fake/weather/search` | prognozy kilku miast |
| `GET /api/fake/weather/{city}/station` | stacja pomiarowa miasta (`id`, `name`, `elevation`, `operator`) |
| `GET /api/fake/weather/stations/{stationId}/readings?hours=` | odczyty stacji z ostatnich godzin |

Drzewo kafelków:

```text
Panel pogodowy (details, korzeń)         pola: Miasto, Jednostki, [Kod miasta]
├── days: Prognoza dni (table)           elementy $.forecast[*], klucz „Dzień”
└── station: Stacja (details, etykieta)  pola: Nazwa, Wysokość, Operator, [ID stacji]
    └── readings: Odczyty stacji (table) elementy $.readings[*], klucz „Godzina”
```

`[…]` to pola ukryte, które zasilają wiązania. Korzeń ma własne źródło prognozy i przekazuje miasto obu dzieciom,
a etykieta „Stacja” przekazuje identyfikator stacji swojej tabeli:

```json
{
  "name": "Panel pogodowy",
  "display": "details",
  "sources": [ { "key": "forecast", "kind": "connection", "connectionId": "<ID_PROGNOZA>" } ],
  "fields": [
    { "source": "forecast", "path": "$.city", "label": "Miasto",
      "transformations": [ { "key": "uppercase" } ] },
    { "source": "forecast", "path": "$.units", "label": "Jednostki" },
    { "source": "forecast", "path": "$.city", "label": "Kod miasta", "hidden": true }
  ],
  "children": [
    { "key": "days", "tileId": "<ID_PROGNOZA_DNI>", "position": 0, "cssClass": "col-8" },
    { "key": "station", "tileId": "<ID_STACJA>", "position": 1, "cssClass": "col-4" }
  ],
  "bindings": [
    { "from": "parent", "to": "days", "map": [ { "field": "Kod miasta", "parameter": "city" } ] },
    { "from": "parent", "to": "station", "map": [ { "field": "Kod miasta", "parameter": "city" } ] }
  ]
}
```

```json
{
  "name": "Stacja",
  "display": "details",
  "sources": [ { "key": "station", "kind": "connection", "connectionId": "<ID_STACJA_POLACZENIE>" } ],
  "fields": [
    { "source": "station", "path": "$.name", "label": "Nazwa" },
    { "source": "station", "path": "$.elevation", "label": "Wysokość",
      "transformations": [ { "key": "suffix", "parameter": " m n.p.m." } ] },
    { "source": "station", "path": "$.operator", "label": "Operator" },
    { "source": "station", "path": "$.id", "label": "ID stacji", "hidden": true }
  ],
  "children": [ { "key": "readings", "tileId": "<ID_ODCZYTY>", "position": 0, "cssClass": "col-12" } ],
  "bindings": [ { "from": "parent", "to": "readings", "map": [ { "field": "ID stacji", "parameter": "station_id" } ] } ]
}
```

Klucze źródeł i dzieci mają wspólną przestrzeń nazw w kafelku, dlatego dziecko z prognozą ma klucz `days`, a nie
`forecast`. Szczegóły panelu (`GET /api/custom-data-view/tiles/{id}`) zwracają kontrakt — wejścia, których kafelek
nie wiąże sam, oraz wyjścia:

```json
"contract": {
  "parameters": [ { "name": "city", "required": true }, { "name": "days", "required": false }, { "name": "units", "required": false } ],
  "inputs": [],
  "outputs": { "fields": ["Miasto", "Jednostki", "Kod miasta"], "items": false }
}
```

### Podgląd

`POST /api/custom-data-view/tiles/{id}/preview` z `{ "parameters": { "city": "Warszawa" } }` (albo
`GET .../preview?city=Warszawa`) zwraca pola korzenia i osadzenia w kolejności pozycji (skrót):

```json
{
  "name": "Panel pogodowy", "display": "details",
  "fields": [ { "label": "Miasto", "value": "WARSZAWA" }, { "label": "Jednostki", "value": "metric" },
              { "label": "Kod miasta", "value": "Warszawa", "hidden": true } ],
  "children": [
    { "key": "days", "address": "days", "cssClass": "col-8", "state": "data",
      "tile": { "name": "Prognoza dni", "display": "table", "itemKey": "Dzień",
                "items": [ { "key": "23.10", "fields": [ { "label": "Dzień", "value": "23.10" }, { "label": "Temperatura", "value": "0 °C" } ] } ] } },
    { "key": "station", "address": "station", "cssClass": "col-4", "state": "data",
      "tile": { "name": "Stacja", "display": "details",
                "fields": [ { "label": "Nazwa", "value": "Stacja Warszawa-Centrum" }, { "label": "ID stacji", "value": "WAR-03", "hidden": true } ],
                "children": [ { "key": "readings", "address": "station/readings", "state": "data",
                                "tile": { "name": "Odczyty stacji", "display": "table", "itemKey": "Godzina", "items": [ ] } } ] } }
  ]
}
```

Prognoza korzenia i tabeli `days` ma te same wartości parametrów, więc połączenie jest wywoływane raz na żądanie.
Podgląd samej etykiety może doręczyć wartości wnukowi: `{ "parameters": { "city": "Gdańsk" }, "descendants":
{ "readings": { "parameters": { "hours": 12 } } } }`.

Treść żądania podglądu (wszystkie sekcje opcjonalne): `forms` (wartości formularzy według adresu osadzenia
formularza; podgląd samego formularza używa adresu `""`), `parameters` (wartości wejść-parametrów kontraktu), `inputs`
(wejścia danych) i `descendants` (wartości dla potomków według ścieżki). Wiązanie w definicji wygrywa z wartością
z żądania. `GET .../preview` przyjmuje tylko płaskie `parameters` w adresie.

### Wybór elementu i dzieci w elementach

- Wiązanie z pola kafelka z elementami bez zakresu oznacza `selected`: cel dostaje stan `awaiting_input` z `pending`
  (`from` — adres węzła, `field`, `scope`, `parameter`/`input`) i kompletem pozostałych `inputs`. Po wyborze front
  uzupełnia wartość i woła podgląd kafelka celu.
- Dziecko `scope: item` ma instancję w każdym elemencie rodzica, z adresem zawierającym klucz elementu
  (`days[23.10]/details`), a `parent` z zakresem `item` przekazuje pole bieżącego elementu:

  ```json
  "children": [ { "key": "details", "tileId": "<ID_SZCZEGÓŁY_DNIA>", "position": 0, "scope": "item", "load": "lazy" } ],
  "bindings": [ { "from": "parent", "to": "details", "map": [ { "field": "Dzień", "parameter": "date", "scope": "item" } ] } ]
  ```

### Stany osadzeń i błędy

| Stan | Znaczenie |
|---|---|
| `data` | kafelek wykonany; `tile` zawiera zagnieżdżony podgląd (pola mogą mieć błędy) |
| `awaiting_input` | czeka na wybór elementu albo wymagane pole formularza (`inputs`, `pending`) |
| `awaiting_load` | `lazy` albo przekroczony limit (`inputs`) |
| `error` | `dependency_failed` (z przyczyną: węzeł, pole, kod) albo `invalid_binding` |

Błędy pól: `source_unavailable`, `path_not_found`, `transformation_failed`, `invalid_parameters` (brak albo zły typ
parametru połączenia, ze szczegółami i informacją, czy wartość pochodzi z wiązania), `dependency_failed`,
`invalid_binding`, `awaiting_input`, a w formularzu `invalid_value`. Podgląd zawsze zwraca 200.

Limity podglądu: 50 instancji na osadzenie `eager` ze `scope: item`, 200 wywołań zewnętrznych na żądanie (po
deduplikacji — to samo połączenie z tymi samymi wartościami jest wywoływane raz), 10 s na całe żądanie i 8 równoległych
wywołań instancji dzieci. Osadzenia ponad limit dostają `awaiting_load`.

### Blokady

| Operacja | Wynik |
|---|---|
| Usunięcie połączenia używanego przez źródło kafelka | 409 `connection_in_use` z listą kafelków |
| Usunięcie kafelka osadzonego w innym kafelku | 409 `tile_in_use` z listą rodziców |
| Edycja usuwająca albo zmieniająca etykietę pola użytego w wiązaniu rodzica | 409 `field_in_use` z polami i rodzicami |
