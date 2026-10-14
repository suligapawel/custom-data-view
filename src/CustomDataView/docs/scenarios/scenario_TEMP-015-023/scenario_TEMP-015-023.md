# #TEMP-015-023 — Scenariusz implementacji

## Cel

Użytkownik może zdefiniować kafelek: uporządkowaną listę pól, z których każde wskazuje połączenie, ścieżkę JSONPath do
wybranej wartości w odpowiedzi, etykietę i łańcuch transformacji. Kafelki można przeglądać, edytować i usuwać,
a podgląd (zapisanego lub niezapisanego kafelka) wywołuje każde użyte połączenie raz i zwraca JSON kafelka z etykietami
i przetransformowanymi wartościami albo błędem per pole.

## Kontekst biznesowy

Połączenia (TEMP-007 … TEMP-014) zwracają pełne odpowiedzi zewnętrznych API, a katalog transformacji (TEMP-001 …
TEMP-006) potrafi przekształcić pojedynczą wartość. Kafelek łączy te elementy: z każdej odpowiedzi bierze tylko
potrzebne wartości i nadaje im czytelną postać. Kafelek może korzystać z wielu połączeń (różne endpointy, a nawet różne
API), a z jednego połączenia może pobierać wiele pól — np. 2 połączenia × 10 pól = 20 pól, każde z własnym łańcuchem
transformacji.

Kafelek jest wspólny dla wszystkich klientów: nie przechowuje wartości parametrów połączeń (np. `customerId`) — przychodzą
one z frontu przy każdym wywołaniu, osobno dla każdego połączenia. Parametry transformacji (np. `decimals` w `round`)
opisują sposób prezentacji i są zapisane w kafelku.

Blokada usunięcia połączenia używanego przez kafelki (TEMP-020) chroni istniejące kafelki. Kryteria TEMP-018 i TEMP-019
dotyczące widoków (pozycja w siatce, usuwanie z siatki) przeniesiono do TEMP-024 w PRD — widoki jeszcze nie istnieją.
Parametry transformacji nie są walidowane przy zapisie (decyzja dewelopera); ich błędy ujawnia podgląd
(`transformation_failed`, `invalid_parameter`).

## Domena

Folder `CustomDataView.Core/Tiles/`:

- `Definitions/` — `Tile`, `TileDefinition`, `TileField`, `TileInput`, `TileFieldInput`, `TransformationStepInput`,
  `TransformationParameterValues`.
- `Paths/` — `JsonPath`, `JsonPathSegment`, `JsonPathParser`.
- `Validation/` — `TileDefinitionValidator` (reguły lokalne), `TileReferencesValidator` (połączenia i klucze
  transformacji).
- `Persistence/` — `ITileRepository`, `PostgresTileRepository`, `TileReference`.
- `Management/` — `ITileService`, `TileService`, `TileSummaryDto`, `TileDto`, `TileFieldDto`, `TransformationStepDto`.
- `Preview/` — `ITilePreviewService`, `TilePreviewService`, `TileParameterValues`, `TilePreview`,
  `TileFieldPreview`, `TileFieldError`, `TileFieldErrorCodes`.

Wspólne wyjątki w `CustomDataView.Core/Exceptions/`: istniejące `NotFoundException(id)`, `ValidationException(errors)`
oraz nowy `ConnectionInUseException(connectionId, tiles)`.

### Zadanie 1 — Definicja kafelka (TEMP-015)

```csharp
public sealed record TileField(
    Guid ConnectionId,
    string Path,
    string Label,
    IReadOnlyList<TransformationStep> Transformations);

public sealed record TileDefinition(string Name, IReadOnlyList<TileField> Fields);

public sealed record Tile(Guid Id, TileDefinition Definition);
```

- `Id` jest generowany przez serwer (`Guid.NewGuid()`) przy dodaniu i nie zmienia się przy edycji.
- `Fields` to lista uporządkowana; pozycja pola na liście jest jego tożsamością (etykiety nie muszą być unikalne).
  Pola z różnych połączeń mogą się przeplatać w dowolnej kolejności.
- `TileField.Transformations` używa istniejącego `TransformationStep(Key, Parameters)` z
  `Transformations/Execution/`. Pusta lista oznacza wartość bez zmian. Ta sama transformacja może wystąpić kilka razy.
- `TileInput` / `TileFieldInput` / `TransformationStepInput` to niezwalidowane wejście z żądania (`connectionId` jako
  `Guid?`, parametry transformacji jako `JsonElement`). Po pomyślnej walidacji `ToDefinition(transformations)` tworzy
  `TileDefinition`.

### Zadanie 2 — Parametry transformacji zapisane w kafelku (TEMP-015, TEMP-016)

Parametry są przechowywane w bazie jako tekst (bez JSON), a ich typ jest odtwarzany z deskryptora transformacji
w katalogu. `TransformationParameterValues` stosuje tę samą normalizację przy zapisie, odczycie i podglądzie
niezapisanego kafelka, dlatego oba podglądy dają ten sam wynik:

- Do tekstu (`ToText`): string JSON → jego treść; liczba JSON → surowy tekst liczby (`2` → `"2"`, `1.5` → `"1.5"`).
  Inna wartość (`true`, `null`, obiekt, tablica) → błąd walidacji `fields[i].transformations[j].parameters.<nazwa>`
  (jedyna reguła parametrów sprawdzana przy zapisie — wynika z formatu przechowywania).
- Z tekstu (`FromText(transformation, name, text)`): deskryptor `integer` i tekst pasujący do `^-?[0-9]+$` → liczba
  JSON (`JsonValue.Create(int)`; przekroczenie zakresu `int` → string). W pozostałych przypadkach (deskryptor
  `string`, tekst nieliczbowy, nazwa spoza deskryptorów) → string JSON.
- Brakujące, nadmiarowe i spoza zakresu parametry **nie są** sprawdzane przy zapisie. W podglądzie
  `TransformationParameters` zgłasza brak lub zły typ jako `transformation_failed` (`invalid_parameter`), a nadmiarowe
  parametry są ignorowane (zachowanie z TEMP-001 … TEMP-006).

### Zadanie 3 — JSONPath bez zewnętrznego pakietu (TEMP-015, TEMP-016, TEMP-021, TEMP-023)

`JsonPathParser.TryParse(text, out JsonPath path)` (bez nowych pakietów NuGet) obsługuje podzbiór JSONPath:

| Składnia            | Znaczenie                                                              |
|---------------------|------------------------------------------------------------------------|
| `$`                 | korzeń odpowiedzi (musi rozpoczynać ścieżkę; sam `$` = cała odpowiedź) |
| `.name`             | właściwość obiektu; nazwa `[A-Za-z_][A-Za-z0-9_]*`                     |
| `['name']`, `["name"]` | właściwość o dowolnej nazwie (spacje, myślniki, kropki); escape `\'`, `\"`, `\\` |
| `[n]`, `[-n]`       | element tablicy; ujemny indeks liczony od końca (`[-1]` = ostatni)     |
| `[*]`, `.*`         | wildcard: wszystkie elementy tablicy albo wszystkie wartości obiektu   |

- Inna składnia — pusta ścieżka, brak `$` na początku, białe znaki, rekurencja `..`, filtry `?()`, wycinki `[a:b]`,
  unie `[0,1]`, niedomknięte nawiasy lub cudzysłowy — jest błędem składni.
- Nazwy właściwości są porównywane z rozróżnianiem wielkości liter.
- `JsonPath.HasWildcard` — czy ścieżka zawiera co najmniej jeden wildcard; decyduje o kształcie wyniku.
- `JsonPath.Evaluate(JsonNode root)` zwraca listę dopasowań w kolejności dokumentu:
  - właściwość na obiekcie → wartość, jeśli istnieje (także jawne `null`); na nie-obiekcie → brak dopasowania,
  - indeks na tablicy → element, jeśli w zakresie; poza zakresem lub na nie-tablicy → brak dopasowania,
  - wildcard na tablicy → elementy; na obiekcie → wartości właściwości; na wartości prostej → brak dopasowań,
  - kolejne wildcardy spłaszczają wynik (`$.orders[*].items[*].price` → jedna płaska lista cen).
- Kształt wartości pola:
  - ścieżka **bez** wildcardu → pojedyncza wartość; brak dopasowania → błąd `path_not_found`; jawne `null` w JSON
    → wartość `null` (nie jest błędem),
  - ścieżka **z** wildcardem → zawsze tablica (także jednoelementowa); brak dopasowań → pusta tablica `[]` (nie jest
    błędem).

### Zadanie 4 — Walidacja definicji (TEMP-016)

Walidacja ma dwa etapy; oba zwracają słownik `klucz → komunikaty` i oba muszą przejść, zanim cokolwiek zostanie zapisane
lub wywołane.

`TileDefinitionValidator.Validate(input)` — reguły lokalne (statyczne, wywoływane z `request.Validate()`):

- `name`: wymagana, po obcięciu spacji niepusta, najwyżej 100 znaków; unikalność nie jest wymagana.
- `fields`: co najmniej jedno pole.
- `fields[i].label`: wymagana, po obcięciu spacji niepusta, najwyżej 100 znaków; bez wymogu unikalności.
- `fields[i].path`: wymagana; składniowo poprawna według Zadania 3. Komunikat wskazuje pozycję pola.
- `fields[i].connectionId`: wymagany (brak → błąd).
- `fields[i].transformations[j].key`: wymagany (pusty → błąd).
- `fields[i].transformations[j].parameters.<nazwa>`: wartość tekstowa lub liczbowa (Zadanie 2).

`TileReferencesValidator.Validate(definition)` — reguły zależne od stanu (singleton, przyjmuje `IConnectionRepository`
i `IEnumerable<ITransformation>`):

- `fields[i].connectionId`: połączenie musi istnieć; komunikat zawiera identyfikator
  (`Połączenie '<id>' nie istnieje.`). Każde odwołane połączenie jest wczytywane raz.
- `fields[i].transformations[j].key`: klucz musi istnieć w katalogu; komunikat zawiera klucz
  (`Transformacja 'xyz' nie istnieje w katalogu.`).

Ta sama walidacja obsługuje dodanie, edycję i podgląd niezapisanego kafelka (w podglądzie klucze mają prefiks `tile.`).
Przy błędzie walidacji nic nie jest zapisywane ani wywoływane.

### Zadanie 5 — Repozytorium PostgreSQL (TEMP-015, TEMP-017 … TEMP-020)

```csharp
public sealed record TileReference(Guid Id, string Name);

public interface ITileRepository
{
    void Add(Tile tile);
    Tile Get(Guid id);
    IReadOnlyList<Tile> GetAll();
    void Update(Tile tile);
    void Delete(Guid id);
    IReadOnlyList<TileReference> GetUsingConnection(Guid connectionId);
}
```

- `PostgresTileRepository` (singleton) korzysta z tego samego `NpgsqlDataSource` co repozytorium połączeń.
- `Add` i `Update` zapisują kafelek, pola, transformacje i parametry w jednej transakcji. `Update` aktualizuje
  `name` i `updated_at`, usuwa wszystkie pola (transformacje i parametry znikają kaskadowo) i wstawia je ponownie.
  `Delete` usuwa kafelek; pola, transformacje i parametry znikają kaskadowo, połączenia pozostają.
- Pozycje pól i transformacji są zapisywane jako `position` od 0; odczyt sortuje po pozycji, więc kolejność jest
  zachowana. Parametry transformacji są zapisywane jako tekst (`TransformationParameterValues.ToText`) i odtwarzane
  z typem (`FromText`).
- `Get` zwraca `null` dla nieistniejącego id. `GetAll` zwraca kafelki po `created_at, id` (kolejność dodania; edycja
  zachowuje pozycję).
- `GetUsingConnection` zwraca różne kafelki, których co najmniej jedno pole korzysta z połączenia, po
  `created_at, id`.

### Zadanie 6 — Zarządzanie kafelkami (TEMP-015, TEMP-017 … TEMP-019)

`TileService` (singleton) przyjmuje `ITileRepository`, `TileReferencesValidator`:

- `Add(definition)` → `Guid`: `TileReferencesValidator` (błędy → `ValidationException`), nadaje id, zapisuje.
- `GetAll()` → `TileSummaryDto(Id, Name, FieldCount)` w kolejności dodania.
- `Get(id)` → `TileDto(Id, Name, Fields)`; `TileFieldDto(ConnectionId, Path, Label, Transformations)`;
  `TransformationStepDto(Key, Parameters)` z parametrami jako `JsonNode`. Brak → `NotFoundException(id)`.
- `Update(id, definition)`: brak → `NotFoundException(id)` (przed walidacją referencji); następnie
  `TileReferencesValidator`; zastępuje nazwę i całą listę pól wraz z kolejnością.
- `Delete(id)`: brak → `NotFoundException(id)`; połączenia nie są zmieniane.

### Zadanie 7 — Blokada usunięcia połączenia (TEMP-020)

- `ConnectionService` przyjmuje dodatkowo `ITileRepository`. `Delete(id)`: brak połączenia → `NotFoundException(id)`;
  `GetUsingConnection(id)` niepuste → `ConnectionInUseException(id, tiles)`; w przeciwnym razie usuwa połączenie.
- `ConnectionInUseException` → 409 z listą identyfikatorów i nazw kafelków; połączenie pozostaje niezmienione.
- Po usunięciu ostatniego kafelka korzystającego z połączenia (lub zmianie jego pól) usunięcie połączenia się udaje.
- FK `tile_fields.connection_id` z `ON DELETE RESTRICT` jest zabezpieczeniem w bazie przed wyścigiem; w MVP wyścig
  (kafelek dodany między sprawdzeniem a usunięciem) kończy się błędem bazy, nie utratą spójności.

### Zadanie 8 — Podgląd kafelka (TEMP-021 … TEMP-023)

```csharp
public sealed record TileFieldError(string Code, string Transformation = null, string Reason = null, int? Status = null);

public sealed record TileFieldPreview(string Label, JsonNode Value, TileFieldError Error);

public sealed record TilePreview(Guid? Id, string Name, IReadOnlyList<TileFieldPreview> Fields);
```

`TilePreviewService` (singleton) przyjmuje `ITileRepository`, `IConnectionRepository`, `IConnectionFactory`,
`ITransformationService`, `TileReferencesValidator`:

- `Preview(id, parameters, ct)`: brak kafelka → `NotFoundException(id)`; dalej jak niżej z `Id = id`.
- `Preview(definition, parameters, ct)`: definicja zwalidowana lokalnie przez endpoint; `TileReferencesValidator`
  (błędy z prefiksem `tile.` → `ValidationException`); dalej jak niżej z `Id = null`.

Sekwencja wspólna:

1. Wartości parametrów (płaska mapa `nazwa → wartość JSON` z frontu, bez identyfikatorów połączeń) są rozdzielane
   przez `TileParameterValues.For(definition, parameters)` na każde różne połączenie użyte w polach: parametr trafia do
   każdej sekcji (`path`, `query`, `body`) każdego połączenia, które deklaruje go pod tą nazwą; parametry
   niezadeklarowane przez żadne połączenie kafelka są pomijane; pominięte parametry dostają wartości domyślne
   z definicji. Dla `path` i `query` liczba JSON jest zamieniana na tekst w kulturze niezmiennej (także elementy
   listy w `query`); `body` dostaje wartość bez zmian.
2. Dla każdego różnego połączenia użytego w polach `ParameterValuesValidator.Validate(definition, values)`; błędy
   z kluczami `parameters.<nazwa>` (np. brak wymaganego, obiekt jako wartość parametru adresu) → `ValidationException`
   → 400; żadne połączenie nie zostaje wywołane.
3. Każde użyte połączenie jest wywoływane **dokładnie raz**, wszystkie równolegle (`Task.WhenAll`):
   `factory.Create(definition).Get(values.ToValues(), ct)`. Czas podglądu ≈ najwolniejsze źródło.
4. Dla każdego pola w kolejności definicji:
   - źródło zakończone błędem → `source_unavailable`; gdy kod klienta jest inny (`unexpected_status`, `invalid_json`,
     `response_too_large`), trafia do `reason`, a dla `unexpected_status` także `status`,
   - ścieżka bez wildcardu bez dopasowania → `path_not_found`,
   - wartość (lub każdy element tablicy przy wildcardzie) zamieniona `TransformationValue.FromJson` na model wartości
     łańcucha (`null`, `string`, `decimal`, `bool`, `JsonNode`) i przepuszczona przez
     `ITransformationService.Apply(value, field.Transformations)`,
   - błąd łańcucha (na pojedynczej wartości lub na dowolnym elemencie tablicy) → całe pole ma
     `transformation_failed` z kluczem transformacji (`transformation`) i powodem (`reason`), bez wartości,
   - sukces → `Value` jako `JsonNode` (`decimal`/`string`/`bool` → `JsonValue`, `null` → `null`, `JsonNode` → kopia);
     przy wildcardzie `JsonArray` przetransformowanych elementów.
5. Wynik zawsze 200, także gdy wszystkie pola mają błędy. Nic nie jest zapisywane ani logowane (treści odpowiedzi
   zewnętrznych API nie trafiają do logów).

`TransformationValue.FromJson(JsonNode)` jest dodawane w ramach tego zadania (opisane w TEMP-001 … TEMP-006, brak
w kodzie): `null` → `null`; string → `string`; liczba → `decimal`; `true`/`false` → `bool`; obiekt/tablica → ten sam
`JsonNode`.

## Kryteria akceptacji

Dodanie (TEMP-015):

- [ ] Kafelek z nazwą i dwoma polami z różnych połączeń zapisuje się (201) i zwraca `id`.
- [ ] Kilka pól może korzystać z tego samego połączenia.
- [ ] Pole z transformacjami `round(decimals: 2)` → `suffix(text: " zł")` zapisuje klucze, kolejność i wartości
  parametrów; szczegóły zwracają je bez zmian (`decimals` jako liczba, `text` jako tekst).
- [ ] Pole bez transformacji jest dozwolone.
- [ ] Kolejność pól jest zachowana w szczegółach i podglądzie.
- [ ] Dwa kafelki o tej samej nazwie i dwa pola o tej samej etykiecie zapisują się poprawnie.

Walidacja (TEMP-016):

- [ ] Brak nazwy, nazwa z samych spacji lub dłuższa niż 100 znaków → 400 (`name`).
- [ ] Pusta lista pól → 400 (`fields`).
- [ ] Brak etykiety lub etykieta dłuższa niż 100 znaków → 400 (`fields[i].label`).
- [ ] Pusta ścieżka albo ścieżka niepoprawna składniowo (`sellers[0]`, `$..name`, `$.items[?(@.x)]`, `$.a[`) → 400
  (`fields[i].path`).
- [ ] Brak `connectionId` albo odwołanie do nieistniejącego połączenia → 400 (`fields[i].connectionId`) z
  identyfikatorem w komunikacie.
- [ ] Klucz transformacji spoza katalogu → 400 (`fields[i].transformations[j].key`) z kluczem w komunikacie.
- [ ] Wartość parametru transformacji inna niż tekst lub liczba (np. obiekt) → 400
  (`fields[i].transformations[j].parameters.<nazwa>`).
- [ ] Brakujący lub spoza zakresu parametr transformacji (`round` bez `decimals`, `round(decimals: 20)`) zapisuje się,
  a podgląd zwraca dla pola `transformation_failed` (`round`, `invalid_parameter`).
- [ ] Przy błędzie walidacji nic nie zostaje zapisane (lista bez zmian).

Przeglądanie (TEMP-017):

- [ ] Lista zwraca id, nazwę i liczbę pól każdego kafelka w kolejności dodania; bez kafelków lista jest pusta.
- [ ] Szczegóły zwracają nazwę i uporządkowaną listę pól (połączenie, ścieżka, etykieta, transformacje z parametrami).
- [ ] Szczegóły nieistniejącego kafelka (lub niepoprawnego Guid) → 404.

Edycja (TEMP-018):

- [ ] Edycja zastępuje nazwę i listę pól wraz z kolejnością; id i pozycja na liście bez zmian.
- [ ] Edycja podlega tym samym regułom walidacji (400) i przy błędzie niczego nie zmienia.
- [ ] Edycja nieistniejącego kafelka → 404.

Usunięcie (TEMP-019):

- [ ] Usunięty kafelek znika z listy, a jego szczegóły zwracają 404.
- [ ] Połączenia używane przez pola kafelka pozostają niezmienione.
- [ ] Usunięcie nieistniejącego kafelka → 404.

Blokada usunięcia połączenia (TEMP-020):

- [ ] Usunięcie połączenia używanego przez pole co najmniej jednego kafelka → 409 z listą `{id, name}` kafelków.
- [ ] Przy konflikcie połączenie pozostaje niezmienione (szczegóły bez zmian).
- [ ] Po usunięciu ostatniego kafelka korzystającego z połączenia usunięcie połączenia się udaje (204).
- [ ] Połączenie nieużywane przez kafelki usuwa się jak dotychczas.

Podgląd zapisanego kafelka (TEMP-021):

- [ ] Podgląd zwraca id i nazwę kafelka oraz pola w kolejności definicji, każde z etykietą i wartością po łańcuchu
  transformacji (np. `21.456` z `round(1)` → `suffix(" °C")` → `"21.5 °C"`).
- [ ] Pole bez transformacji zwraca wartość z odpowiedzi bez zmian (liczba jako liczba, obiekt jako obiekt).
- [ ] Ścieżka z wildcardem (`$.sellers[*].name`) zwraca tablicę, a każdy element przechodzi przez łańcuch
  (`uppercase` → `["ACME", "GLOBEX"]`); brak dopasowań → `[]`.
- [ ] Ścieżka z dwoma wildcardami zwraca płaską tablicę.
- [ ] Ścieżki `$['unit-name']`, `$.items[-1]` i `$.*` wskazują odpowiednie wartości.
- [ ] Jawne `null` w odpowiedzi daje wartość `null`, nie błąd.
- [ ] Połączenie używane przez kilka pól jest wywoływane dokładnie raz; dwa różne połączenia — po jednym razie.
- [ ] Podgląd przyjmuje tylko płaską mapę `parameters`; wartość trafia do każdego połączenia kafelka, które deklaruje
  parametr o tej nazwie (np. `customerId` do A, `currency` do B i C).
- [ ] Brak wartości wymaganego parametru połączenia → 400 (`parameters.<nazwa>`) i żadne połączenie nie jest wywoływane.
- [ ] Parametr niezadeklarowany przez żadne połączenie kafelka jest pomijany.
- [ ] Liczba jako wartość parametru adresu lub zapytania jest zamieniana na tekst; niepoprawna wartość (np. obiekt)
  → 400 (`parameters.<nazwa>`).
- [ ] Podgląd nie zmienia kafelka, a jego wynik nie jest zapisywany.
- [ ] Podgląd nieistniejącego kafelka → 404.

Podgląd niezapisanego kafelka (TEMP-022):

- [ ] Podgląd definicji z żądania zwraca wynik w tym samym formacie (`id: null`) i z tymi samymi wartościami co
  podgląd tej samej definicji po zapisaniu.
- [ ] Niepoprawna definicja → 400 (klucze z prefiksem `tile.`) i żadne połączenie nie jest wywoływane.
- [ ] Podgląd nie tworzy kafelka (lista bez zmian).

Częściowy wynik (TEMP-023):

- [ ] Gdy jedno ze źródeł jest niedostępne, jego pola mają `source_unavailable`, a pola z pozostałych źródeł mają
  wartości.
- [ ] Odpowiedź źródła ze statusem 503 daje dla jego pól `source_unavailable` z `reason: unexpected_status`
  i `status: 503`.
- [ ] Ścieżka bez wildcardu bez wartości → `path_not_found`.
- [ ] Błąd łańcucha (np. `round` na `"abc"`) → `transformation_failed` z kluczem `round` i powodem `invalid_value`;
  przy wildcardzie błąd jednego elementu daje błąd całego pola.
- [ ] Podgląd z błędami pól (także wszystkich) zwraca HTTP 200.

## Endpoint

Prefiks: `/api/custom-data-view/tiles`. Plik `CustomDataView.Api/Endpoints/TileEndpoints.cs` (`MapTileEndpoints`).
Żądania z metodą `Validate()`: `TileRequest` (mapowany na `TileInput` i sprawdzany przez `TileDefinitionValidator`),
`TilePreviewRequest`, `TileDraftPreviewRequest`. Odpowiedzi bez opakowania w `ApiResponse` (jak połączenia).

`TileRequest`:

```json
{
  "name": "Klient",
  "fields": [
    {
      "connectionId": "3f2a…",
      "path": "$.balance",
      "label": "Saldo",
      "transformations": [
        { "key": "round", "parameters": { "decimals": 2 } },
        { "key": "suffix", "parameters": { "text": " zł" } }
      ]
    },
    { "connectionId": "9b1c…", "path": "$.orders[*].number", "label": "Zamówienia" }
  ]
}
```

Brak `transformations` = pusta lista; brak `parameters` = brak parametrów.

| Metoda i ścieżka           | Wejście                    | Sukces                                          | Błędy    |
|----------------------------|----------------------------|-------------------------------------------------|----------|
| `POST /`                   | `TileRequest`              | 201 `{ "id": "…" }`, `Location: …/{id}`         | 400      |
| `GET /`                    | —                          | 200 `[ { "id", "name", "fieldCount" } ]`        | —        |
| `GET /{id:guid}`           | —                          | 200 pełna definicja z `id`                      | 404      |
| `PUT /{id:guid}`           | `TileRequest`              | 204                                             | 400, 404 |
| `DELETE /{id:guid}`        | —                          | 204                                             | 404      |
| `POST /{id:guid}/preview`  | `{ "parameters": { … } }`  | 200 podgląd                                     | 400, 404 |
| `POST /preview`            | `{ "tile": …, "parameters": { … } }` | 200 podgląd                           | 400      |

Zmiana w połączeniach: `DELETE /api/custom-data-view/connections/{id:guid}` zwraca dodatkowo 409:

```json
{ "code": "connection_in_use", "tiles": [ { "id": "…", "name": "Klient" } ] }
```

Wartości parametrów w podglądzie — płaska mapa bez identyfikatorów połączeń (pole `parameters` opcjonalne):

```json
{
  "parameters": { "customerId": 42, "currency": "EUR", "tag": [ "a", "b" ], "limit": 5 }
}
```

Wynik podglądu (200):

```json
{
  "id": "…",
  "name": "Klient",
  "fields": [
    { "label": "Saldo", "value": "1234.57 zł" },
    { "label": "Zamówienia", "value": [ "Z/1", "Z/2" ] },
    { "label": "Kurs", "error": { "code": "source_unavailable", "reason": "unexpected_status", "status": 503 } },
    { "label": "Miasto", "error": { "code": "path_not_found" } },
    { "label": "Data", "error": { "code": "transformation_failed", "transformation": "date_format", "reason": "invalid_value" } }
  ]
}
```

Pole ma `value` (także `null`) albo `error`, nigdy oba. Puste właściwości błędu są pomijane.

Błąd walidacji (400) — `Results.ValidationProblem(errors)`; klucze jak w Zadaniu 4 i Zadaniu 8 (dla `POST /preview`
definicja z prefiksem `tile.`). `NotFoundException` → 404, `ValidationException` → 400, `ConnectionInUseException` →
409 w `CustomDataViewExceptionHandler`.

## Walidacja wejścia

- `name`: wymagana, niepusta po obcięciu spacji, 1-100 znaków, bez unikalności.
- `fields`: wymagana, co najmniej jedno pole, bez limitu liczby pól.
- `fields[i].connectionId`: wymagany Guid istniejącego połączenia.
- `fields[i].path`: wymagana, składnia z Zadania 3.
- `fields[i].label`: wymagana, niepusta po obcięciu spacji, 1-100 znaków, bez unikalności.
- `fields[i].transformations[j].key`: wymagany, z katalogu transformacji.
- `fields[i].transformations[j].parameters.<nazwa>`: tekst lub liczba JSON; nazwy i zakres nie są sprawdzane.
- `parameters.<nazwa>` (podgląd): reguły `ParameterValuesValidator` z TEMP-013 dla każdej sekcji każdego użytego
  połączenia, które deklaruje parametr (liczby w `path`/`query` zamieniane na tekst); nazwy niezadeklarowane
  pomijane.
- `id` w ścieżce: Guid (ograniczenie trasy; niepoprawny → 404).

## Handler i sekwencja wykonania

Dodanie / edycja:

1. Endpoint deserializuje `TileRequest` (niepoprawny JSON → 400).
2. `request.Validate()` → `TileDefinitionValidator`; błędy → `ValidationProblem` 400, nic nie jest zapisywane.
3. `TileInput.ToDefinition(transformations)` → `TileDefinition` (parametry znormalizowane — Zadanie 2).
4. `TileService.Add` / `Update`: (edycja: brak kafelka → 404) → `TileReferencesValidator` (błędy → 400) →
   `ITileRepository.Add` / `Update` w jednej transakcji.
5. 201 z `id` / 204.

Odczyt i usunięcie: `TileService.GetAll` / `Get` / `Delete` → 200 / 204, brak → 404.

Usunięcie połączenia: `ConnectionService.Delete` → brak → 404; `ITileRepository.GetUsingConnection` niepuste → 409;
w przeciwnym razie usunięcie → 204.

Podgląd:

1. Zapisany: `TilePreviewService.Preview(id, connections)` (brak → 404). Niezapisany: `request.Validate()` (klucze
   z prefiksem `tile.`) → `ToDefinition` → `TilePreviewService.Preview(definition, connections)` →
   `TileReferencesValidator`.
2. Walidacja listy wartości i `ParameterValuesValidator` dla każdego użytego połączenia; błędy → 400 bez wywołań.
3. Równoległe wywołanie każdego użytego połączenia raz (`IConnectionFactory` / `IConnection.Get`).
4. Dla każdego pola: błąd źródła → `JsonPath.Evaluate` → `ITransformationService.Apply` (per element przy
   wildcardzie) → `TileFieldPreview`.
5. 200 z `TilePreview`.

`CustomDataView.Bootstrapper` wywołuje `AddCustomDataViewTiles()` (rejestracja repozytorium, walidatora referencji
i serwisów) oraz `MapTileEndpoints()`.

## Baza danych

Schemat według `docs/foundation/db-plan.md` (tabele `tiles`, `tile_fields`, `tile_field_transformations`,
`tile_field_transformation_parameters`; decyzje 15-21). Migracja
`CustomDataView.Core/Persistence/Migrations/20261013140000_create_tiles.sql` tworzy tabele kafelków, klucze obce
(w tym `tile_fields.connection_id → connections.id ON DELETE RESTRICT`) i indeksy `ix_tiles_created_at_id`,
`ix_tile_fields_connection_id`; aplikacja jej nie wykonuje — uruchamia się ją ręcznie.

## Repozytoria

- Nowe `ITileRepository` / `PostgresTileRepository` (Zadanie 5), w tym `GetUsingConnection(connectionId)`.
- `IConnectionRepository` bez zmian. `ConnectionService` dostaje zależność `ITileRepository` (Zadanie 7).
- W testach API: `TestTileRepository` (w pamięci, bez bibliotek mockujących) współdzielący listę połączeń
  z `TestConnectionRepository` tylko przez identyfikatory.

## Frontend

Brak zmian.

## Testy

### Testy jednostkowe

- **`JsonPathParser` / `JsonPath`:** poprawne i niepoprawne składnie (tabela z Zadania 3); `HasWildcard`; dopasowania
  właściwości, `['name']` ze znakami specjalnymi, indeksu dodatniego/ujemnego/poza zakresem, wildcardu na tablicy,
  obiekcie i wartości prostej, płaskiego wyniku dwóch wildcardów, jawnego `null`.
- **`TransformationParameterValues`:** `ToText` dla tekstu, liczby i wartości odrzucanych; `FromText` dla `integer`
  (cyfry → liczba, tekst → string), `string` i nazwy spoza deskryptorów.
- **`TransformationValue.FromJson`:** każdy rodzaj wartości JSON.
- **`TileDefinitionValidator`:** każda reguła lokalna z kluczem błędu.

### Testy integracyjne

> Testy integracyjne dla tego scenariusza są pisane na podstawie scenariuszy Gherkin w
> `src/CustomDataView/docs/scenarios/scenario_TEMP-015-023/gherkin.md` (API przez `CustomDataViewApplicationFactory`,
> `FakeExternalApi` liczący wywołania, repozytoria w pamięci). `PostgresTileRepository` ma testy w
> `CustomDataView.Core.Persistence.Tests` (zapis i odczyt kolejności, parametrów z typem, edycja, kaskadowe usunięcie,
> `GetUsingConnection`, FK `RESTRICT`).
