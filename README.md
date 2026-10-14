# CustomDataView

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
