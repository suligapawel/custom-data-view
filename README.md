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

## Przykład: kafelek „Pogoda” na fałszywym API pogody

Przykład korzysta z endpointów deweloperskich `FakeWeatherEndpoints` (`/api/fake/weather/...`) i zakłada, że aplikacja
działa pod adresem `http://localhost:5080`. Kolejność kroków ma znaczenie — kafelek odwołuje się do identyfikatorów
połączeń.

### 1. Połączenie „Prognoza dla miasta”

`POST /api/custom-data-view/connections`

```json
{
  "name": "Prognoza dla miasta",
  "method": "GET",
  "url": "http://localhost:5080/api/fake/weather/{city}/forecast",
  "pathParameters": [
    { "name": "city", "required": true, "defaultValue": null }
  ],
  "queryParameters": [
    { "name": "days", "required": false, "defaultValue": "3" },
    { "name": "units", "required": false, "defaultValue": "metric" }
  ]
}
```

### 2. Połączenie „Wyszukiwanie prognoz”

`POST /api/custom-data-view/connections`

```json
{
  "name": "Wyszukiwanie prognoz",
  "method": "POST",
  "url": "http://localhost:5080/api/fake/weather/search",
  "body": { "cities": "{cities}", "days": "{days}", "units": "{units}" },
  "bodyParameters": [
    { "name": "cities", "required": false, "defaultValue": ["Gdańsk", "Kraków"] },
    { "name": "days", "required": false, "defaultValue": 2 },
    { "name": "units", "required": false, "defaultValue": "metric" }
  ]
}
```

Obie odpowiedzi zwracają `id` — wstaw je w miejsce `<ID_PROGNOZA>` i `<ID_WYSZUKIWANIE>` w kolejnych krokach.

### 3. Kafelek „Pogoda”

`POST /api/custom-data-view/tiles`

```json
{
  "name": "Pogoda",
  "fields": [
    {
      "connectionId": "<ID_PROGNOZA>",
      "path": "$.city",
      "label": "Miasto",
      "transformations": [
        { "key": "uppercase", "parameters": {} }
      ]
    },
    {
      "connectionId": "<ID_PROGNOZA>",
      "path": "$.forecast[0].date",
      "label": "Dzień",
      "transformations": [
        { "key": "date_format", "parameters": { "pattern": "dd.MM.yyyy" } }
      ]
    },
    {
      "connectionId": "<ID_PROGNOZA>",
      "path": "$.forecast[0].windSpeed",
      "label": "Wiatr",
      "transformations": [
        { "key": "round", "parameters": { "decimals": 0 } },
        { "key": "suffix", "parameters": { "text": " m/s" } }
      ]
    },
    {
      "connectionId": "<ID_PROGNOZA>",
      "path": "$.forecast[*].summary",
      "label": "Opisy",
      "transformations": [
        { "key": "lowercase", "parameters": {} },
        { "key": "truncate", "parameters": { "max_length": 5 } }
      ]
    },
    {
      "connectionId": "<ID_WYSZUKIWANIE>",
      "path": "$.results[*].city",
      "label": "Porównywane miasta",
      "transformations": [
        { "key": "prefix", "parameters": { "text": "📍 " } }
      ]
    },
    {
      "connectionId": "<ID_WYSZUKIWANIE>",
      "path": "$.results[*].forecast[0].temperature",
      "label": "Temperatury dziś",
      "transformations": [
        { "key": "round", "parameters": { "decimals": 1 } },
        { "key": "suffix", "parameters": { "text": " °C" } }
      ]
    },
    {
      "connectionId": "<ID_WYSZUKIWANIE>",
      "path": "$.results[0].forecast[0].humidity",
      "label": "Wilgotność"
    }
  ]
}
```

Odpowiedź zwraca `id` kafelka — wstaw je w miejsce `{id}` w podglądzie.

### 4. Podgląd zapisanego kafelka

`POST /api/custom-data-view/tiles/{id}/preview`

```json
{
  "parameters": {
    "city": "Warszawa",
    "days": 5,
    "units": "metric",
    "cities": ["Wrocław", "Poznań", "Gdańsk"]
  }
}
```

To samo metodą GET — parametry w adresie żądania, powtórzony parametr staje się listą:

```
GET /api/custom-data-view/tiles/{id}/preview?city=Warszawa&days=5&units=metric&cities=Wrocław&cities=Poznań&cities=Gdańsk
```

Przez curl (`-G` dokleja `--data-urlencode` do adresu i koduje polskie znaki):

```bash
curl -G "http://localhost:5080/api/custom-data-view/tiles/<ID_KAFELKA>/preview" \
  --data-urlencode "city=Warszawa" \
  --data-urlencode "days=5" \
  --data-urlencode "units=metric" \
  --data-urlencode "cities=Wrocław" \
  --data-urlencode "cities=Poznań" \
  --data-urlencode "cities=Gdańsk"
```

W jednej linii, z ręcznie zakodowanymi znakami:

```bash
curl "http://localhost:5080/api/custom-data-view/tiles/<ID_KAFELKA>/preview?city=Warszawa&days=5&units=metric&cities=Wroc%C5%82aw&cities=Pozna%C5%84&cities=Gda%C5%84sk"
```

### 5. Podgląd niezapisanego kafelka (opcjonalnie)

`POST /api/custom-data-view/tiles/preview` — w `tile` wstaw definicję z kroku 3:

```json
{
  "tile": {
    "name": "Pogoda",
    "fields": [
      {
        "connectionId": "<ID_PROGNOZA>",
        "path": "$.city",
        "label": "Miasto",
        "transformations": [
          { "key": "uppercase", "parameters": {} }
        ]
      }
    ]
  },
  "parameters": { "city": "Warszawa", "days": 5, "cities": ["Wrocław", "Poznań"] }
}
```

### 6. Widok „Pulpit pogodowy”

`POST /api/custom-data-view/views` — w `tileId` wstaw identyfikator kafelka z kroku 3. Kafelek może wystąpić w widoku
najwyżej raz, a pozycje muszą być unikalne i nieujemne (luki są dozwolone). Klasa CSS jest opcjonalna (do 200 znaków)
i opisuje frontendowi sposób wyświetlenia kafelka:

```json
{
  "name": "Pulpit pogodowy",
  "tiles": [
    { "tileId": "<ID_KAFELKA>", "position": 0, "cssClass": "col-6 highlight" }
  ]
}
```

Odpowiedź zwraca `id` widoku. Pozostałe operacje:

| Operacja                   | Żądanie                                       | Wynik                                           |
|----------------------------|-----------------------------------------------|-------------------------------------------------|
| Lista widoków              | `GET /api/custom-data-view/views`             | `[ { "id", "name", "tileCount" } ]`             |
| Szczegóły widoku           | `GET /api/custom-data-view/views/{id}`        | nazwa i kafelki w kolejności pozycji            |
| Edycja (zastępuje całość)  | `PUT /api/custom-data-view/views/{id}`        | 204; treść jak przy dodaniu                     |
| Usunięcie                  | `DELETE /api/custom-data-view/views/{id}`     | 204; kafelki i połączenia pozostają             |

Szczegóły widoku:

```json
{
  "id": "<ID_WIDOKU>",
  "name": "Pulpit pogodowy",
  "tiles": [
    { "tileId": "<ID_KAFELKA>", "tileName": "Pogoda", "position": 0, "cssClass": "col-6 highlight" }
  ]
}
```

Przez curl:

```bash
curl -X POST "http://localhost:5080/api/custom-data-view/views" \
  -H "Content-Type: application/json" \
  -d '{"name": "Pulpit pogodowy", "tiles": [{"tileId": "<ID_KAFELKA>", "position": 0, "cssClass": "col-6 highlight"}]}'

curl "http://localhost:5080/api/custom-data-view/views/<ID_WIDOKU>"
```

Usunięcie kafelka zdejmuje go ze wszystkich widoków, które go zawierały; widoki pozostają.

### 7. Podgląd widoku z danymi

`POST /api/custom-data-view/views/{id}/preview` — parametry jak w podglądzie kafelka (jedna płaska mapa); trafiają do
wszystkich kafelków widoku:

```json
{
  "parameters": { "city": "Warszawa", "days": 5, "cities": ["Wrocław", "Poznań"] }
}
```

To samo metodą GET:

```bash
curl -G "http://localhost:5080/api/custom-data-view/views/<ID_WIDOKU>/preview" \
  --data-urlencode "city=Warszawa" \
  --data-urlencode "days=5" \
  --data-urlencode "cities=Wrocław" \
  --data-urlencode "cities=Poznań"
```

Wynik zawiera układ widoku i podgląd każdego kafelka w kolejności pozycji:

```json
{
  "id": "<ID_WIDOKU>",
  "name": "Pulpit pogodowy",
  "tiles": [
    {
      "tileId": "<ID_KAFELKA>", "tileName": "Pogoda", "position": 0, "cssClass": "col-6 highlight",
      "fields": [ { "label": "Miasto", "value": "WARSZAWA" } ]
    }
  ]
}
```

Brak wymaganego parametru daje błąd tylko w kafelku, którego połączenia go wymagają (pozostałe kafelki zwracają
dane, status 200):

```json
{ "tileId": "…", "tileName": "Pogoda", "position": 0, "cssClass": null,
  "error": { "code": "invalid_parameters", "errors": { "parameters.city": ["…"] } } }
```

Każdy kafelek pobiera dane osobno, więc połączenie wspólne dla kilku kafelków jest wywoływane kilka razy.

### Uwagi

- Podgląd przyjmuje wyłącznie płaską mapę parametrów, bez identyfikatorów połączeń. Wartość trafia do każdego
  połączenia kafelka, które deklaruje parametr o tej nazwie; parametry niezadeklarowane są pomijane.
- Wymagany jest tylko `city` — pozostałe parametry mają wartości domyślne w połączeniach.
- `days` i `units` trafiają do obu połączeń: w „Prognozie” jako parametry zapytania (liczba zamieniana na tekst),
  w „Wyszukiwaniu” do treści żądania.
- W GET wszystkie wartości są tekstem. Pojedyncze `cities=Gdańsk` da tekst zamiast listy i pola z „Wyszukiwania”
  dostaną błąd źródła — wtedy użyj POST z `"cities": ["Gdańsk"]`.
- `date_format` przyjmuje daty ISO (`yyyy-MM-dd` lub `yyyy-MM-ddTHH:mm:sszzz`), dlatego pole „Dzień” korzysta
  z `forecast[0].date`, a nie z `generatedAt`.
