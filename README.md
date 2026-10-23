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

Przed migracją `AddTileFieldHiddenAndNormalizedLabel` na istniejącej bazie sprawdź, czy etykiety pól w kafelkach się
nie powtarzają (zapytanie w `src/CustomDataView/docs/foundation/db-plan.md`, sekcja „Migracje danych”). Baza
utworzona wcześniej migracjami SQL ma już schemat z migracji `CreateSchema` — wpisz ją do historii, zanim
zastosujesz kolejne. Migracja `RemoveViews` usuwa tabele widoków razem z danymi.

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

## Przykład: pulpit pogodowy na fałszywym API pogody

Przykład korzysta z endpointów deweloperskich `FakeWeatherEndpoints` (`/api/fake/weather/...`) i zakłada, że aplikacja
działa pod adresem `http://localhost:5080`. Kolejność kroków ma znaczenie — kafelki odwołują się do identyfikatorów
połączeń i innych kafelków.

### 1. Połączenia

`POST /api/custom-data-view/connections` — „Prognoza dla miasta” (zwraca `id` → `<ID_PROGNOZA>`):

```json
{
  "name": "Prognoza dla miasta",
  "method": "GET",
  "url": "http://localhost:5080/api/fake/weather/{city}/forecast",
  "pathParameters": [ { "name": "city", "required": true } ],
  "queryParameters": [
    { "name": "days", "required": false, "defaultValue": "3" },
    { "name": "units", "required": false, "defaultValue": "metric" }
  ]
}
```

„Wyszukiwanie prognoz” (`<ID_WYSZUKIWANIE>`):

```json
{
  "name": "Wyszukiwanie prognoz",
  "method": "POST",
  "url": "http://localhost:5080/api/fake/weather/search",
  "body": { "cities": "{cities}", "days": "{days}" },
  "bodyParameters": [
    { "name": "cities", "required": true },
    { "name": "days", "required": false, "defaultValue": 1 }
  ]
}
```

### 2. Formularz „Filtry” (`form`)

`POST /api/custom-data-view/tiles` → `<ID_FILTRY>`:

```json
{
  "name": "Filtry",
  "display": "form",
  "fields": [
    { "label": "Miasto", "type": "text", "required": true },
    { "label": "Dni", "type": "number", "default": 3 }
  ]
}
```

### 3. Tabela „Dni prognozy” (`table` z wejściem danych)

Kafelek dostaje surowy JSON dni od rodzica przez wejście `days` → `<ID_DNI>`:

```json
{
  "name": "Dni prognozy",
  "display": "table",
  "sources": [ { "key": "days", "kind": "input" } ],
  "items": { "source": "days", "path": "$[*]", "key": "Dzień" },
  "fields": [
    { "source": "days", "path": "$.date", "label": "Dzień",
      "transformations": [ { "key": "date_format", "parameters": { "pattern": "dd.MM" } } ] },
    { "source": "days", "path": "$.temperature", "label": "Temperatura",
      "transformations": [ { "key": "suffix", "parameters": { "text": " °C" } } ] },
    { "source": "days", "path": "$.summary", "label": "Opis" }
  ]
}
```

### 4. Lista „Porównanie” (`list`) i podsumowanie „Dziś” (`summary`)

`<ID_PORÓWNANIE>` — lista miast z wyszukiwania (wymaga parametru `cities`):

```json
{
  "name": "Porównanie",
  "display": "list",
  "sources": [ { "key": "search", "kind": "connection", "connectionId": "<ID_WYSZUKIWANIE>" } ],
  "items": { "source": "search", "path": "$.results[*]", "key": "Miasto" },
  "fields": [
    { "source": "search", "path": "$.city", "label": "Miasto", "role": "title" },
    { "source": "search", "path": "$.forecast[0].summary", "label": "Opis", "role": "subtitle" },
    { "source": "search", "path": "$.forecast[0].temperature", "label": "Temperatura", "role": "badge" }
  ]
}
```

`<ID_DZIŚ>` — wyróżnione wartości bieżącego dnia (wymaga parametru `city`):

```json
{
  "name": "Dziś",
  "display": "summary",
  "sources": [ { "key": "forecast", "kind": "connection", "connectionId": "<ID_PROGNOZA>" } ],
  "fields": [
    { "source": "forecast", "path": "$.forecast[0].temperature", "label": "Temperatura", "role": "primary" },
    { "source": "forecast", "path": "$.forecast[0].windSpeed", "label": "Wiatr", "role": "secondary",
      "transformations": [ { "key": "round", "parameters": { "decimals": 0 } }, { "key": "suffix", "parameters": { "text": " m/s" } } ] }
  ]
}
```

### 5. Kafelek złożony „Pulpit pogodowy”

Rodzic ma własne źródło i pola oraz cztery dzieci. Formularz zasila połączenie rodzica (dziecko → źródło, dwa
mapowania w jednym wiązaniu), ukryte pole z surowym JSON dni trafia do wejścia tabeli (`parent` → dziecko), a miasto
z formularza trafia do dziecka „Dziś”. „Porównanie” ładuje się na żądanie frontu (`lazy`):

```json
{
  "name": "Pulpit pogodowy",
  "display": "details",
  "sources": [ { "key": "forecast", "kind": "connection", "connectionId": "<ID_PROGNOZA>" } ],
  "fields": [
    { "source": "forecast", "path": "$.city", "label": "Miasto",
      "transformations": [ { "key": "uppercase", "parameters": {} } ] },
    { "source": "forecast", "path": "$.forecast", "label": "Dni JSON", "hidden": true }
  ],
  "children": [
    { "key": "filters", "tileId": "<ID_FILTRY>", "position": 0, "cssClass": "col-12" },
    { "key": "today", "tileId": "<ID_DZIŚ>", "position": 1, "cssClass": "col-4" },
    { "key": "days", "tileId": "<ID_DNI>", "position": 2, "cssClass": "col-8" },
    { "key": "compare", "tileId": "<ID_PORÓWNANIE>", "position": 3, "load": "lazy" }
  ],
  "bindings": [
    { "from": "filters", "to": "forecast",
      "map": [ { "field": "Miasto", "parameter": "city" }, { "field": "Dni", "parameter": "days" } ] },
    { "from": "parent", "to": "days", "map": [ { "field": "Dni JSON", "input": "days" } ] },
    { "from": "filters", "to": "today", "map": [ { "field": "Miasto", "parameter": "city" } ] }
  ]
}
```

Szczegóły kafelka (`GET /api/custom-data-view/tiles/{id}`) zwracają definicję i kontrakt — wejścia, których nie
wiąże sam kafelek (tu opcjonalne `units` połączenia rodzica), oraz wyjścia:

```json
"contract": {
  "parameters": [ { "name": "units", "required": false } ],
  "inputs": [],
  "outputs": { "fields": ["Miasto", "Dni JSON"], "items": false }
}
```

### 6. Podgląd

`POST /api/custom-data-view/tiles/{id}/preview` — wartości formularzy według adresu osadzenia formularza:

```json
{ "forms": { "filters": { "Miasto": "Warszawa", "Dni": 5 } } }
```

Odpowiedź (skrócona) zawiera pola rodzica i osadzenia w kolejności pozycji z adresem i stanem:

```json
{
  "id": "<ID_PULPIT>", "name": "Pulpit pogodowy", "display": "details",
  "fields": [ { "label": "Miasto", "value": "WARSZAWA" }, { "label": "Dni JSON", "value": [ ], "hidden": true } ],
  "children": [
    { "key": "filters", "address": "filters", "state": "data",
      "tile": { "display": "form", "fields": [ { "label": "Miasto", "type": "text", "required": true, "default": null, "value": "Warszawa" } ] } },
    { "key": "today", "address": "today", "state": "data", "tile": { "display": "summary", "fields": [ ] } },
    { "key": "days", "address": "days", "state": "data",
      "tile": { "display": "table", "itemKey": "Dzień", "items": [ { "key": "23.10", "fields": [ ] } ] } },
    { "key": "compare", "address": "compare", "state": "awaiting_load", "inputs": {} }
  ]
}
```

Dziecko `lazy` front doładowuje podglądem kafelka dziecka z jego `inputs` (tu bez wiązań, więc z własnymi
wartościami):

```bash
curl -X POST "http://localhost:5080/api/custom-data-view/tiles/<ID_PORÓWNANIE>/preview" \
  -H "Content-Type: application/json" \
  -d '{"parameters": {"cities": ["Warszawa"]}}'
```

Treść żądania podglądu (wszystkie sekcje opcjonalne): `forms` (wartości formularzy według adresu; podgląd samego
formularza używa adresu `""`), `parameters` (wartości wejść-parametrów kontraktu), `inputs` (wejścia danych)
i `descendants` (wartości dla potomków według ścieżki, np. `{ "days": { "inputs": { "days": [ ] } } }`). Wiązanie
w definicji wygrywa z wartością z żądania. `GET .../preview?city=Warszawa` przyjmuje tylko płaskie `parameters`.

### Wybór elementu i dzieci w elementach

- Wiązanie z pola kafelka z elementami bez zakresu oznacza `selected`: cel dostaje stan `awaiting_input` z `pending`
  (`from` — adres węzła, `field`, `scope`, `parameter`/`input`) i kompletem pozostałych `inputs`. Po wyborze front
  uzupełnia wartość i woła podgląd kafelka celu.
- Dziecko `scope: item` ma instancję w każdym elemencie rodzica, z adresem zawierającym klucz elementu
  (`[23.10]/details`), a `parent` z zakresem `item` przekazuje pole bieżącego elementu:

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
