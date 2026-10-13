# #TEMP-007-014 — Scenariusz implementacji

## Cel

Użytkownik może dodawać, przeglądać, edytować i usuwać połączenia do zewnętrznych REST API. Połączenie jest szablonem
żądania GET albo POST z parametrami ścieżki, query i body, których wartości podaje się dopiero przy wykonaniu. Test
połączenia (zapisanego lub niezapisanego) z podanymi wartościami parametrów pokazuje status HTTP, czas odpowiedzi
i pełną treść JSON albo czytelny kod błędu.

## Kontekst biznesowy

Dziś każda zmiana źródła danych wymaga zmiany w kodzie. Wiele API ma adresy zależne od kontekstu
(`/contracts/{id}/sellers`), filtry w query i zapytania w body, dlatego połączenie opisuje endpoint raz, a konkretne
wartości trafiają do niego przy każdym wywołaniu (test połączenia, podgląd kafelka, w przyszłości moduły pobierające
dane). Globalny timeout i limit rozmiaru chronią aplikację przed wolnymi lub zbyt dużymi odpowiedziami. Blokada usuwania
połączeń używanych przez kafelki należy do TEMP-020 i nie jest częścią tego zadania.

## Domena

Folder `CustomDataView.Core/Connections/`:

- `Definitions/` — `Connection`, `ConnectionDefinition`, `GetConnectionDefinition`, `PostConnectionDefinition`,
  `ParameterDeclaration`, `ConnectionInput`, `ParameterDeclarationInput`, `ConnectionMethods`.
- `Validation/` — `ConnectionDefinitionValidator`, `ParameterValuesValidator`, `ParameterNames`.
- `Templates/` — `UrlTemplate`, `BodyTemplate` (podstawianie wartości i wartości domyślnych).
- `Calls/` — `IConnection`, `GetConnection`, `PostConnection`, `IConnectionFactory`, `ConnectionFactory`,
  `ConnectionRequestBuilder`, `ParameterValuesInput` (surowe wartości JSON z żądania), `ConnectionParameterValues`.
- `Http/` — `IExternalApiClient`, `ExternalApiClient`, `ExternalApiOptions`, `ExternalApiResult`,
  `ExternalApiErrorCodes`.
- `Persistence/` — `IConnectionRepository`, `InMemoryConnectionRepository`.
- `Management/` — `IConnectionService`, `ConnectionService`, `ConnectionSummaryDto`, `ConnectionDto`.
- `Testing/` — `IConnectionTestService`, `ConnectionTestService`.

Wspólne wyjątki w `CustomDataView.Core/Exceptions/`: `NotFoundException(id)` oraz `ValidationException(errors)`.

### Zadanie 1 — Definicja połączenia jako szablon (TEMP-007, TEMP-008)

```csharp
public sealed record ParameterDeclaration(string Name, bool Required, string DefaultValue);

public abstract record ConnectionDefinition(
    string Name,
    string Url,
    IReadOnlyList<ParameterDeclaration> PathParameters,
    IReadOnlyList<ParameterDeclaration> QueryParameters);

public sealed record GetConnectionDefinition(...) : ConnectionDefinition(...);

public sealed record PostConnectionDefinition(
    ...,
    string Body,
    IReadOnlyList<ParameterDeclaration> BodyParameters) : ConnectionDefinition(...);

public sealed record Connection(Guid Id, ConnectionDefinition Definition);
```

- Nie ma enuma metody: rodzaj połączenia wynika z typu definicji (`GetConnectionDefinition` /
  `PostConnectionDefinition`). Na granicy API rodzaj jest tekstem `"GET"` / `"POST"` w polu `method`.
- `Id` jest generowany przez serwer (`Guid.NewGuid()`) przy dodaniu i nie zmienia się przy edycji (także przy zmianie
  GET ↔ POST).
- `Url` to szablon: parametr ścieżki zapisuje się jako `{name}`, np.
  `https://api.example.com/contracts/{id}/sellers?units=metric`. Stałe query można zostawić w samym URL.
- Definicja nie ma nagłówków: każde połączenie wysyła te same, predefiniowane w kodzie nagłówki — treść POST zawsze
  jako `Content-Type: application/json`.
- `PathParameters`, `QueryParameters`, `BodyParameters` to deklaracje: nazwa, czy wymagany, opcjonalna wartość
  domyślna. `DefaultValue` jest przechowywane jako surowy tekst JSON; dla ścieżki i query musi to być tekst JSON, dla
  body dowolna wartość JSON.
- `Body` (tylko POST) to szablon JSON zapisany jako surowy tekst (`JsonElement.GetRawText()`) albo `null`. Każda
  wartość tekstowa JSON równa dokładnie `"{name}"` (nazwa w formacie z Zadania 2) jest miejscem na parametr body.
  Klucze obiektów i fragmenty tekstu (`"Kontrakt {id}"`) nie są podmieniane.
- `ConnectionInput` to niezwalidowane wejście z żądania (`method` jako tekst, wartości domyślne jako `JsonElement?`).
  Po pomyślnej walidacji `ToDefinition()` tworzy `GetConnectionDefinition` albo `PostConnectionDefinition`.
  `method` jest przyjmowane bez rozróżniania wielkości liter.

### Zadanie 2 — Walidacja definicji (TEMP-009)

`ConnectionDefinitionValidator.Validate(input)` zwraca słownik `pole → komunikaty` (pusty = poprawna definicja). Ten
sam walidator obsługuje dodanie, edycję i test niezapisanej definicji. Klucze błędów: `name`, `url`, `method`, `body`,
`pathParameters[i].name|defaultValue|required`, `queryParameters[i].…`,
`bodyParameters[i].…`.

- `name`: wymagana, po obcięciu spacji niepusta, najwyżej 100 znaków; unikalność nie jest wymagana.
- `method`: `GET` albo `POST` (bez rozróżniania wielkości liter); inna wartość lub brak → błąd.
- `url`: wymagany; po podstawieniu za każdy placeholder wartości `x` musi być bezwzględnym URL `http`/`https`.
  Placeholdery `{name}` są dozwolone wyłącznie w ścieżce — klamry w hoście, porcie, query lub fragmencie oraz klamry
  niedomknięte albo z pustą/niepoprawną nazwą → błąd `url`.
- Nazwy parametrów ścieżki i body (`ParameterNames`): `^[a-z][a-z0-9_]{0,49}$` (snake_case, najwyżej 50 znaków).
  Nazwy parametrów query: niepuste, bez ograniczeń formatu (to klucze zewnętrznego API, np. `pageSize`, `api-version`).
- Nazwy parametrów ścieżki i body są unikalne w obrębie swojej listy (porównanie ordinalne) → powtórzenie to błąd.
  Nazwy parametrów query mogą się powtarzać (np. dwie deklaracje `tag` → `?tag=a&tag=b`). Ta sama nazwa w różnych
  listach jest dozwolona (np. `id` w ścieżce i w body).
- Każdy placeholder w URL ma deklarację w `pathParameters` i każda deklaracja ścieżki jest użyta w URL. To samo dla
  body i `bodyParameters`. Placeholder może wystąpić kilka razy.
- `required: true` z `defaultValue` → błąd. Parametr ścieżki musi być wymagany albo mieć niepustą wartość domyślną.
- `defaultValue` ścieżki i query musi być tekstem JSON; dla body dowolna wartość JSON.
- `body` i `bodyParameters` są dozwolone tylko dla POST. POST bez body jest poprawny (wtedy `bodyParameters` musi być
  puste). Wartość JSON `null` i brak pola `body` oznaczają brak body. Niepoprawny składniowo JSON w treści żądania nie
  dociera do walidatora — kończy się 400 przy deserializacji.
- Przy błędzie walidacji nic nie jest zapisywane ani wysyłane.

### Zadanie 3 — Wartości parametrów w czasie wykonania (TEMP-013)

```csharp
public sealed record ConnectionParameterValues(
    IReadOnlyDictionary<string, string> Path,
    IReadOnlyDictionary<string, IReadOnlyList<string>> Query,
    IReadOnlyDictionary<string, JsonNode> Body);
```

`ParameterValuesValidator.Validate(definition, values)` → słownik błędów z kluczami
`parameters.path.<name>`, `parameters.query.<name>`, `parameters.body.<name>`:

- Wartość dla niezadeklarowanego parametru → błąd.
- Brak wartości parametru wymaganego → błąd.
- Wartość ścieżki musi być niepustym tekstem JSON (`null`, liczba, obiekt → błąd).
- Wartość query to tekst albo niepusta tablica tekstów (`"a"` lub `["a", "b"]`); `null`, liczba, obiekt, pusta tablica
  lub element nietekstowy → błąd. Tekst jest traktowany jak tablica jednoelementowa.
- Wartość body może być dowolną wartością JSON, także `null`.

`ParameterValueResolver` ustala wartość efektywną: podana → domyślna → brak. Brak oznacza: parametr query pominięty
w URL, parametr body podmieniony na JSON `null` (parametr ścieżki zawsze ma wartość po walidacji).

### Zadanie 4 — Interfejs połączenia i dwie implementacje (TEMP-007, TEMP-008, TEMP-013)

```csharp
public interface IConnection
{
    Task<ExternalApiResult> Get(ConnectionParameterValues parameters, CancellationToken cancellationToken);
}

public sealed class GetConnection(GetConnectionDefinition definition, IExternalApiClient client) : IConnection;

public sealed class PostConnection(PostConnectionDefinition definition, IExternalApiClient client) : IConnection;
```

- Interfejs ma jedną metodę `Get`, która wykonuje zapytanie do zdefiniowanego wcześniej endpointu z przekazanymi
  wartościami parametrów. `GetConnection` wysyła żądanie metodą GET, `PostConnection` — metodą POST.
- Wspólne kroki obu implementacji (`UrlTemplate`):
  - placeholdery ścieżki są zastępowane wartościami zakodowanymi `Uri.EscapeDataString` (`/` → `%2F`),
  - parametry query są dopisywane (zakodowane) do istniejącego query URL w kolejności deklaracji; nic nie jest
    nadpisywane. Gdy dla nazwy podano wartości, w miejscu jej pierwszej deklaracji trafia po jednej parze na każdą
    wartość (`tag: ["a", "b"]` → `tag=a&tag=b`), a kolejne deklaracje tej nazwy są pomijane. Gdy nie podano wartości,
    każda deklaracja tej nazwy używa swojej wartości domyślnej albo jest pomijana; jeśli którakolwiek deklaracja nazwy
    jest wymagana, wartość jest wymagana.
- `PostConnection` dodatkowo (`BodyTemplate`): klonuje szablon body, każdą wartość `"{name}"` zastępuje wartością JSON
  parametru (typ zachowany: `"{limit}"` + `10` → `10`), wysyła `StringContent(..., UTF-8, "application/json")`.
  `Content-Type: application/json` jest stały i nie da się go zmienić. POST bez body wysyła żądanie bez treści.
- `Get` zakłada wartości zwalidowane przez `ParameterValuesValidator`; naruszenie (np. brak wymaganej ścieżki) →
  `InvalidOperationException`.
- `ConnectionFactory` (singleton) przyjmuje `IExternalApiClient`; `Create(definition)` zwraca `GetConnection` albo
  `PostConnection` według typu definicji (inny typ → `NotSupportedException`).
- Implementacje są wielokrotnego użytku dla podglądu kafelka (TEMP-021 … TEMP-023).

### Zadanie 5 — Klient HTTP z limitami (TEMP-014)

`ExternalApiOptions`: `Timeout = 10 s`, `MaxResponseBytes = 1 048 576` (1 MB); domyślne wartości w kodzie, opcjonalnie
nadpisywane sekcją konfiguracji `CustomDataView:ExternalApi`.

`ExternalApiClient.Send(HttpRequestMessage request, CancellationToken cancellationToken)` → `ExternalApiResult`:

- Nazwany `HttpClient` z `IHttpClientFactory` (bez nowych pakietów NuGet), `SocketsHttpHandler { AllowAutoRedirect =
  false }`, `HttpClient.Timeout = Infinite`; globalny timeout realizuje połączony `CancellationTokenSource`
  z `CancelAfter(Timeout)`, obejmujący wysłanie i wczytanie całej treści.
- `HttpCompletionOption.ResponseHeadersRead`; czas mierzony `Stopwatch` od wysłania do wczytania całej treści,
  w pełnych milisekundach.
- Kolejność oceny wyniku:
  1. Timeout lub `HttpRequestException` (DNS, odmowa połączenia, TLS) → `source_unavailable`.
  2. Status spoza 2xx (także 3xx — przekierowania nie są wykonywane) → `unexpected_status` z otrzymanym statusem;
     treść nie jest czytana.
  3. `Content-Length` większy od limitu albo przekroczenie limitu podczas strumieniowego czytania →
     `response_too_large`; odpowiedź nie jest wczytywana w całości.
  4. Pusta treść lub niepoprawny JSON (`JsonNode.Parse`) → `invalid_json`. `Content-Type` odpowiedzi nie jest
     sprawdzany.
  5. W przeciwnym razie sukces: status, `ElapsedMs`, sparsowana pełna treść JSON (bez obcinania).
- Anulowanie żądania przez klienta naszego API (token wywołującego) nie jest zamieniane na kod błędu.
- Klient niczego nie loguje; nigdzie nie trafiają nagłówki, body żądań ani treść odpowiedzi.

### Zadanie 6 — Repozytorium w pamięci (TEMP-007, TEMP-010 … TEMP-012)

Moduł nie ma `db-plan.md`, więc zgodnie z `AGENTS.md` połączenia są przechowywane w statycznym `Dictionary` w pamięci,
bez wymyślania schematu. PostgreSQL dojdzie po przygotowaniu `db-plan.md`.

```csharp
public interface IConnectionRepository
{
    void Add(Connection connection);
    Connection Get(Guid id);
    IReadOnlyList<Connection> GetAll();
    void Update(Connection connection);
    bool Delete(Guid id);
}
```

- `InMemoryConnectionRepository` (singleton): statyczny `Dictionary<Guid, Connection>` oraz statyczna lista
  identyfikatorów w kolejności dodania; dostęp chroniony jednym `lock`.
- `Get` zwraca `null` dla nieistniejącego id. `GetAll` zwraca połączenia w kolejności dodania; edycja zachowuje pozycję.

### Zadanie 7 — Zarządzanie połączeniami (TEMP-007 … TEMP-012)

`ConnectionService` (singleton) przyjmuje `IConnectionRepository`:

- `Add(definition)` → `Guid`: nadaje id, zapisuje.
- `GetAll()` → `ConnectionSummaryDto(Id, Name, Method, Url)` w kolejności dodania; `Method` = `"GET"` / `"POST"`
  według typu definicji.
- `Get(id)` → `ConnectionDto(Id, Name, Method, Url, PathParameters, QueryParameters, Body, BodyParameters)`;
  `Body` i domyślne wartości body jako `JsonNode`; dla GET `Body = null`, `BodyParameters = []`. Brak →
  `NotFoundException(id)`.
- `Update(id, definition)`: brak → `NotFoundException(id)`; w przeciwnym razie zastępuje całą definicję (także jej
  rodzaj). Kafelki odwołują się do połączenia po id, więc po edycji korzystają z nowej definicji.
- `Delete(id)`: brak → `NotFoundException(id)`.

### Zadanie 8 — Test połączenia (TEMP-013)

`ConnectionTestService` (singleton) przyjmuje `IConnectionRepository`, `IConnectionFactory`:

- `Test(id, values, ct)`: brak połączenia → `NotFoundException(id)`; `ParameterValuesValidator` → błędy →
  `ValidationException`; `factory.Create(definition).Get(values, ct)`.
- `Test(definition, values, ct)`: definicja zwalidowana przez endpoint; dalej jak wyżej.
- Oba warianty dają ten sam wynik dla tej samej definicji i wartości. Test nie tworzy ani nie zmienia połączenia,
  a wynik nie jest nigdzie zapisywany ani logowany.

## Kryteria akceptacji

- [ ] Połączenie GET z nazwą, szablonem URL `…/contracts/{id}/sellers` i deklaracjami parametrów ścieżki
  i query zapisuje się (201) i zwraca `id`.
- [ ] Połączenie POST z szablonem body `{"contractId": "{id}", "limit": "{limit}"}` i deklaracjami `bodyParameters`
  zapisuje się, a szczegóły zwracają ten sam szablon body.
- [ ] Połączenie POST bez body zapisuje się poprawnie.
- [ ] Zapisane połączenie jest widoczne na liście (id, nazwa, metoda, szablon URL) w kolejności dodania.
- [ ] Dwa połączenia o tej samej nazwie zapisują się poprawnie.
- [ ] `method` `get` zostaje zapisane jako połączenie GET.
- [ ] Brak nazwy, nazwa z samych spacji lub dłuższa niż 100 znaków → 400 (`name`).
- [ ] URL pusty, względny lub o schemacie innym niż http/https → 400 (`url`).
- [ ] Placeholder w hoście lub query (`https://{host}/x`, `?q={q}`) albo niedomknięta klamra → 400 (`url`).
- [ ] `method` inne niż GET/POST lub brak → 400 (`method`).
- [ ] Body lub `bodyParameters` podane dla GET → 400.
- [ ] Niepoprawny JSON w treści żądania → 400.
- [ ] Placeholder w URL bez deklaracji ścieżki albo deklaracja ścieżki nieużyta w URL → 400.
- [ ] Placeholder w body bez deklaracji albo deklaracja body nieużyta w body → 400.
- [ ] Nazwa parametru ścieżki/body niezgodna z snake_case (np. `Id`, `contract-id`) → 400.
- [ ] Pusta nazwa parametru query → 400; nazwa query `pageSize` jest poprawna.
- [ ] Powtórzona nazwa parametru ścieżki lub body → 400; powtórzona nazwa parametru query jest poprawna; ta sama
  nazwa w ścieżce i body jest poprawna.
- [ ] Parametr wymagany z wartością domyślną → 400; opcjonalny parametr ścieżki bez wartości domyślnej → 400.
- [ ] Przy błędzie walidacji nic nie zostaje zapisane (lista bez zmian).
- [ ] Lista jest pusta, gdy nie ma połączeń.
- [ ] Szczegóły zwracają nazwę, metodę, szablon URL, deklaracje parametrów i szablon body.
- [ ] Szczegóły, edycja, usunięcie i test nieistniejącego (lub niepoprawnego Guid) połączenia → 404.
- [ ] Edycja zastępuje całą definicję (także GET ↔ POST), zachowuje id i pozycję na liście i podlega tym samym regułom
  walidacji (400).
- [ ] Usunięte połączenie znika z listy, a jego szczegóły zwracają 404.
- [ ] Test połączenia GET z `path.id = "42"` wysyła GET na `…/contracts/42/sellers`.
- [ ] Wartość ścieżki jest kodowana (`"a/b"` → `a%2Fb`).
- [ ] Parametry query z wartości lub wartości domyślnej są dopisywane do istniejącego query URL; opcjonalny parametr
  query bez wartości jest pomijany.
- [ ] Powtórzone deklaracje query `tag` z wartościami domyślnymi `a` i `b` dają `?tag=a&tag=b`; wartość
  `"tag": ["x", "y", "z"]` daje `?tag=x&tag=y&tag=z`.
- [ ] Test połączenia POST wysyła POST z body, w którym `"{id}"` i `"{limit}"` zastąpiono wartościami z zachowaniem typu
  (`"limit": 10`), z `Content-Type: application/json`.
- [ ] Opcjonalny parametr body bez wartości i bez wartości domyślnej jest podmieniany na `null`.
- [ ] Brak wartości wymaganego parametru, wartość niezadeklarowanego parametru, pusta lub nietekstowa wartość ścieżki
  → 400 (`parameters.<sekcja>.<nazwa>`) i żadne żądanie nie jest wysyłane.
- [ ] Test zapisanego połączenia i test definicji przekazanej w żądaniu dają ten sam wynik dla tej samej definicji
  i wartości.
- [ ] Test niezapisanej, niepoprawnej definicji → 400 i żadne żądanie nie jest wysyłane.
- [ ] Odpowiedź 2xx z poprawnym JSON → 200 ze statusem, `elapsedMs` i pełną treścią JSON.
- [ ] Brak połączenia lub przekroczenie timeoutu 10 s → 502 `source_unavailable`.
- [ ] Status spoza 2xx, w tym przekierowanie 3xx (nie jest wykonywane) → 502 `unexpected_status` z otrzymanym statusem.
- [ ] Niepoprawny JSON lub pusta treść 2xx (np. 204) → 502 `invalid_json`.
- [ ] Odpowiedź większa niż 1 MB → 502 `response_too_large`, bez wczytywania jej w całości.
- [ ] Test nie tworzy ani nie zmienia połączenia, a jego wynik nie jest zapisywany.

## Endpoint

Prefiks: `/api/custom-data-view/connections`. Plik `CustomDataView.Api/Endpoints/ConnectionEndpoints.cs`
(`MapConnectionEndpoints`). Żądania z metodą `Validate()`: `ConnectionRequest` (mapowany na `ConnectionInput`
i sprawdzany przez `ConnectionDefinitionValidator`), `ConnectionTestRequest`, `ConnectionDraftTestRequest`.
Odpowiedzi bez opakowania w `ApiResponse` (jak katalog transformacji).

`ConnectionRequest` — GET:

```json
{
  "name": "Sprzedawcy kontraktu",
  "method": "GET",
  "url": "https://api.example.com/contracts/{id}/sellers?units=metric",
  "pathParameters": [ { "name": "id", "required": true } ],
  "queryParameters": [ { "name": "pageSize", "required": false, "defaultValue": "20" } ]
}
```

`ConnectionRequest` — POST:

```json
{
  "name": "Wyszukiwanie sprzedawców",
  "method": "POST",
  "url": "https://api.example.com/sellers/search",
  "pathParameters": [],
  "queryParameters": [],
  "body": { "contractId": "{id}", "limit": "{limit}" },
  "bodyParameters": [
    { "name": "id", "required": true },
    { "name": "limit", "required": false, "defaultValue": 10 }
  ]
}
```

Brak list = puste listy; brak `required` = `false`.

| Metoda i ścieżka              | Wejście                      | Sukces                                       | Błędy         |
|-------------------------------|------------------------------|----------------------------------------------|---------------|
| `POST /`                      | `ConnectionRequest`          | 201 `{ "id": "…" }`, `Location: …/{id}`      | 400           |
| `GET /`                       | —                            | 200 `[ { "id", "name", "method", "url" } ]`  | —             |
| `GET /{id:guid}`              | —                            | 200 pełna definicja z `id`                   | 404           |
| `PUT /{id:guid}`              | `ConnectionRequest`          | 204                                          | 400, 404      |
| `DELETE /{id:guid}`           | —                            | 204                                          | 404           |
| `POST /{id:guid}/test`        | `{ "parameters": … }`        | 200 wynik testu                              | 400, 404, 502 |
| `POST /test`                  | `{ "connection": …, "parameters": … }` | 200 wynik testu                    | 400, 502      |

Niepoprawny Guid w ścieżce nie pasuje do ograniczenia `:guid` i daje 404.

Wartości parametrów (`parameters`, całe pole i każda sekcja opcjonalne):

```json
{ "parameters": { "path": { "id": "42" }, "query": { "pageSize": "50", "tag": [ "a", "b" ] }, "body": { "limit": 5 } } }
```

Wynik testu (200):

```json
{ "status": 200, "elapsedMs": 143, "body": { "sellers": [ { "name": "ACME" } ] } }
```

Błąd testu (502):

```json
{ "code": "unexpected_status", "status": 503 }
```

`status` występuje tylko dla `unexpected_status`; pozostałe kody: `source_unavailable`, `invalid_json`,
`response_too_large`.

Błąd walidacji (400) — `Results.ValidationProblem(errors)`; dla `POST /test` klucze definicji mają prefiks
`connection.` (np. `connection.url`), a wartości parametrów — `parameters.<sekcja>.<nazwa>`:

```json
{ "title": "One or more validation errors occurred.", "status": 400,
  "errors": { "pathParameters[0].name": [ "…" ], "parameters.path.id": [ "…" ] } }
```

`NotFoundException` → 404, `ValidationException` → `ValidationProblem` 400; mapowanie w API (np. `IExceptionHandler`
rejestrowany w bootstrapperze).

## Walidacja wejścia

- `name`: wymagana, niepusta po obcięciu spacji, 1-100 znaków, bez unikalności.
- `method`: wymagana, `GET` lub `POST` (bez rozróżniania wielkości liter).
- `url`: wymagany, bezwzględny `http`/`https` po podstawieniu placeholderów; `{name}` tylko w ścieżce.
- `pathParameters`: nazwy snake_case ≤ 50, unikalne, zgodne 1:1 z placeholderami URL; wymagany albo z niepustą
  wartością domyślną (tekst).
- `queryParameters`: nazwy niepuste, mogą się powtarzać; `defaultValue` tekst; `required` wyklucza `defaultValue`.
- `body`: opcjonalna wartość JSON, tylko dla POST.
- `bodyParameters`: tylko dla POST; nazwy snake_case ≤ 50, unikalne, zgodne 1:1 z placeholderami body;
  `defaultValue` dowolny JSON; `required` wyklucza `defaultValue`.
- `parameters` (test): tylko zadeklarowane nazwy; wymagane obecne; ścieżka jako niepusty tekst; query jako tekst lub
  niepusta tablica tekstów; body dowolny JSON.
- `id` w ścieżce: Guid (ograniczenie trasy).

## Handler i sekwencja wykonania

Dodanie / edycja:

1. Endpoint deserializuje `ConnectionRequest` (niepoprawny JSON → 400).
2. `request.Validate()` → `ConnectionDefinitionValidator`; błędy → `ValidationProblem` 400, nic nie jest zapisywane.
3. `ConnectionInput.ToDefinition()` → `GetConnectionDefinition` / `PostConnectionDefinition`.
4. `ConnectionService.Add` / `Update` (brak → `NotFoundException` → 404).
5. 201 z `id` / 204.

Odczyt i usunięcie: `ConnectionService.GetAll` / `Get` / `Delete` → 200 / 204, brak → 404.

Test:

1. Zapisane: `ConnectionTestService.Test(id, values)` (brak → 404). Niezapisane: walidacja definicji (400 z prefiksem
   `connection.`), `ToDefinition()`, `ConnectionTestService.Test(definition, values)`.
2. `ParameterValuesValidator` sprawdza wartości względem deklaracji; błędy → 400, żądanie nie jest wysyłane.
3. `ConnectionFactory.Create(definition)` → `GetConnection` / `PostConnection`; `Get(values)` podmienia parametry,
   buduje żądanie i wysyła je przez `ExternalApiClient` z limitami.
4. Sukces → 200 `{ status, elapsedMs, body }`; błąd → 502 `{ code, status? }`.

`CustomDataView.Bootstrapper` wywołuje `AddCustomDataViewConnections()` (rejestracja repozytorium, serwisów, fabryki,
opcji i nazwanego `HttpClient`) oraz `MapConnectionEndpoints()`.

## Baza danych

Brak zmian — moduł nie ma `db-plan.md`; połączenia są w statycznym `Dictionary` w pamięci (Zadanie 6). Konfiguracja nie
przetrwa restartu do czasu wdrożenia PostgreSQL według przyszłego `db-plan.md`.

## Repozytoria

- `IConnectionRepository` (Zadanie 6): `Add`, `Get`, `GetAll`, `Update`, `Delete`.
- `InMemoryConnectionRepository` — statyczny `Dictionary<Guid, Connection>` + kolejność dodania, chroniony `lock`.

## Frontend

Brak zmian.

## Testy

### Testy jednostkowe

Testy w NUnit bez mocków, na prawdziwych obiektach:

- **`ConnectionDefinitionValidator`:** każda reguła z sekcji walidacji, w tym placeholdery w hoście/query,
  niedomknięte klamry, zgodność placeholderów z deklaracjami (ścieżka i body), snake_case, unikalność ścieżki i body
  (powtórzenia query dozwolone), `required` z `defaultValue`, body dla GET, duplikaty nazw połączeń.
- **`ParameterValuesValidator` i `ParameterValueResolver`:** niezadeklarowane, brakujące wymagane, wartości domyślne,
  pominięcie opcjonalnego query, `null` dla opcjonalnego body, typy wartości ścieżki/query, wiele wartości query.
- **`UrlTemplate` (query):** powtórzone deklaracje z wartościami domyślnymi, tablica wartości zastępująca wszystkie
  deklaracje nazwy.
- **`UrlTemplate` i `BodyTemplate`:** kodowanie wartości ścieżki, wielokrotne wystąpienie placeholdera, dopisanie query
  do istniejącego, zachowanie typu wartości w body, brak podmiany kluczy i fragmentów tekstu.
- **`GetConnection` / `PostConnection`:** na prawdziwym `ExternalApiClient` z własnym `HttpMessageHandler` (testowa
  implementacja przechwytująca żądanie, nie biblioteka mocków): metoda HTTP, URL, body i `Content-Type`.
- **`ExternalApiClient`:** sukces 2xx z JSON, `unexpected_status` (404, 500, 302 bez podążania), `invalid_json`
  (niepoprawny i pusty 204), `response_too_large` (po `Content-Length` i przy strumieniu bez `Content-Length`),
  `source_unavailable` (`HttpRequestException` i przekroczenie skróconego timeoutu).
- **`ConnectionService` i `ConnectionTestService`:** na własnym niestatycznym repozytorium w pamięci w projekcie
  testowym: dodanie, lista w kolejności dodania, edycja zachowująca id i pozycję (także GET ↔ POST), usunięcie,
  `NotFoundException`, `ValidationException` dla wartości parametrów, ten sam wynik testu zapisanego i niezapisanego
  połączenia, test nie zmienia repozytorium.

### Testy integracyjne

> Testy integracyjne dla tego scenariusza powinny być pisane na podstawie scenariuszy Gherkin zdefiniowanych
> w `src/CustomDataView/docs/scenarios/scenario_TEMP-007-014/gherkin.md`.

Host testowy (`Microsoft.AspNetCore.Mvc.Testing`) wymaga nowego pakietu NuGet, więc przed dopisaniem testów
integracyjnych potrzebna jest zgoda.
