> Wycofane (2026-10-23): widoki usunięto na rzecz kafelków złożonych — patrz TEMP-038. Dokument zostaje jako historia.

# #TEMP-024-028 — Scenariusz implementacji

## Cel

Użytkownik może zdefiniować widok: nazwę oraz listę kafelków, z których każdy ma pozycję w widoku i opcjonalną klasę
CSS opisującą sposób wyświetlenia. Widoki można przeglądać, edytować i usuwać, a usunięcie kafelka zdejmuje go ze
wszystkich widoków, które go zawierały.

## Kontekst biznesowy

Kafelki (TEMP-015 … TEMP-023) grupują wartości z odpowiedzi zewnętrznych API, ale użytkownik potrzebuje miejsca, w
którym ułoży je w docelowy układ prezentacji. Widok przechowuje wyłącznie układ: które kafelki, w jakiej kolejności
i z jaką klasą CSS. Renderowanie należy do frontendu, który nie jest częścią MVP — dlatego zamiast siatki z wierszami,
kolumnami, szerokością i wysokością układ opisuje kolejność (`position`) i klasa CSS (decyzja dewelopera; PRD 3.4
i `db-plan.md` decyzja 23).

Ten sam kafelek może występować w wielu widokach, ale w jednym widoku najwyżej raz. Usunięcie widoku nie zmienia
kafelków ani połączeń; usunięcie kafelka zdejmuje go z widoków, które same pozostają. Edycja kafelka nie zmienia jego
miejsca w widokach.

## Domena

Folder `CustomDataView.Core/Views/`:

- `Definitions/` — `View`, `ViewDefinition`, `ViewTile`, `ViewInput`, `ViewTileInput`.
- `Validation/` — `ViewDefinitionValidator` (reguły lokalne), `ViewReferencesValidator` (istnienie kafelków).
- `Persistence/` — `IViewRepository`, `PostgresViewRepository`, `ViewSummary`.
- `Management/` — `IViewService`, `ViewService`, `ViewSummaryDto`, `ViewDto`, `ViewTileDto`.

Zmiana w kafelkach: `ITileRepository.GetReferences(ids)` (Zadanie 3). Wspólne wyjątki bez zmian:
`NotFoundException(id)`, `ValidationException(errors)`.

### Zadanie 1 — Definicja widoku (TEMP-024)

```csharp
public sealed record ViewTile(Guid TileId, int Position, string CssClass);

public sealed record ViewDefinition(string Name, IReadOnlyList<ViewTile> Tiles);

public sealed record View(Guid Id, ViewDefinition Definition);
```

- `Id` jest generowany przez serwer (`Guid.NewGuid()`) przy dodaniu i nie zmienia się przy edycji.
- `Tiles` może być pusta (widok bez kafelków jest dozwolony).
- `Position` to nieujemna liczba całkowita podana przez użytkownika; pozycje są unikalne w widoku, luki są dozwolone
  (np. 0, 5, 10) i zapisywane bez zmian. Kolejność wpisów w żądaniu nie ma znaczenia — o kolejności decyduje
  `Position`.
- `CssClass` to dowolny tekst (jedna lub kilka klas); format nie jest sprawdzany. Spacje na brzegach są obcinane,
  a pusty wynik oznacza brak klasy (`null`).
- `ViewInput(Name, Tiles)` / `ViewTileInput(TileId, Position, CssClass)` to niezwalidowane wejście z żądania
  (`TileId` jako `Guid?`, `Position` jako `int?`); brak `tiles` = pusta lista. Po pomyślnej walidacji
  `ToDefinition()` tworzy `ViewDefinition` (z obciętą klasą CSS).

### Zadanie 2 — Walidacja lokalna (TEMP-025)

`ViewDefinitionValidator.Validate(input)` — statyczny, wywoływany z `request.Validate()`, zwraca słownik
`klucz → komunikaty`:

- `name`: wymagana, po obcięciu spacji niepusta, najwyżej 100 znaków; unikalność nie jest wymagana.
- `tiles[i].tileId`: wymagany. Gdy ten sam kafelek występuje ponownie, każde wystąpienie po pierwszym → błąd
  `Kafelek '<id>' występuje w widoku więcej niż raz.`
- `tiles[i].position`: wymagana; mniejsza niż 0 → błąd. Gdy ta sama pozycja występuje ponownie, każde wystąpienie po
  pierwszym → błąd `Pozycja <n> występuje w widoku więcej niż raz.`
- `tiles[i].cssClass`: po obcięciu spacji najwyżej 200 znaków.

### Zadanie 3 — Istnienie kafelków (TEMP-025, TEMP-026)

```csharp
public interface ITileRepository
{
    // … istniejące metody
    IReadOnlyList<TileReference> GetReferences(IReadOnlyCollection<Guid> ids);
}
```

- `GetReferences` zwraca `TileReference(Id, Name)` istniejących kafelków spośród `ids` (jedno zapytanie
  `WHERE id = ANY(@ids)`); nieistniejące są pomijane, pusta lista `ids` → pusta lista bez zapytania.
- `ViewReferencesValidator.Validate(definition)` (singleton, przyjmuje `ITileRepository`): `tiles[i].tileId`
  nieistniejącego kafelka → błąd `Kafelek '<id>' nie istnieje.` Sprawdzane po walidacji lokalnej, przed zapisem.

### Zadanie 4 — Repozytorium PostgreSQL (TEMP-024, TEMP-026 … TEMP-028)

```csharp
public sealed record ViewSummary(Guid Id, string Name, int TileCount);

public interface IViewRepository
{
    void Add(View view);
    View Get(Guid id);
    IReadOnlyList<ViewSummary> GetAll();
    void Update(View view);
    void Delete(Guid id);
}
```

- `PostgresViewRepository` (singleton) korzysta z tego samego `NpgsqlDataSource` co pozostałe repozytoria.
- `Add` i `Update` zapisują widok i wiersze `view_tiles` w jednej transakcji. `Update` aktualizuje `name`
  i `updated_at`, usuwa wszystkie wiersze `view_tiles` i wstawia je ponownie. `Delete` usuwa widok; `view_tiles`
  znikają kaskadowo, kafelki i połączenia pozostają.
- `Get` zwraca `null` dla nieistniejącego id; kafelki widoku są posortowane po `position`.
- `GetAll` zwraca widoki po `created_at, id` (kolejność dodania; edycja zachowuje pozycję na liście) z liczbą
  kafelków liczoną `COUNT` (`LEFT JOIN view_tiles`), więc widok bez kafelków ma `0`.
- Usunięcie kafelka zdejmuje go ze wszystkich widoków przez FK `view_tiles.tile_id → tiles.id ON DELETE CASCADE`;
  `TileService` i `PostgresTileRepository.Delete` nie zmieniają się. Edycja kafelka (`UPDATE tiles`, wymiana pól) nie
  dotyka `view_tiles`, więc kafelek zachowuje pozycję i klasę CSS w widokach.
- Wyścig (kafelek usunięty między walidacją a zapisem widoku) kończy się błędem bazy (naruszenie FK) bez utraty
  spójności; w MVP nie jest mapowany na 400.

### Zadanie 5 — Zarządzanie widokami (TEMP-024, TEMP-026 … TEMP-028)

`ViewService` (singleton) przyjmuje `IViewRepository`, `ITileRepository`, `ViewReferencesValidator`:

- `Add(definition)` → `Guid`: `ViewReferencesValidator` (błędy → `ValidationException`), nadaje id, zapisuje.
- `GetAll()` → `ViewSummaryDto(Id, Name, TileCount)` w kolejności dodania.
- `Get(id)` → `ViewDto(Id, Name, Tiles)`; `ViewTileDto(TileId, TileName, Position, CssClass)` posortowane po
  `Position`; nazwy z `ITileRepository.GetReferences` (jedno wywołanie). Brak widoku → `NotFoundException(id)`.
- `Update(id, definition)`: brak → `NotFoundException(id)` (przed walidacją referencji); następnie
  `ViewReferencesValidator`; zastępuje nazwę i całą listę kafelków.
- `Delete(id)`: brak → `NotFoundException(id)`; kafelki i połączenia nie są zmieniane.

## Kryteria akceptacji

Dodanie (TEMP-024):

- [ ] Widok z nazwą i dwoma kafelkami (pozycje i klasy CSS) zapisuje się (201) i zwraca `id`.
- [ ] Widok bez kafelków (pusta lista albo brak `tiles`) zapisuje się poprawnie.
- [ ] Kafelek bez klasy CSS zapisuje się z `cssClass: null`; klasa z samych spacji jest zapisywana jako `null`, a
  spacje na brzegach są obcinane.
- [ ] Pozycje z lukami (`0`, `5`) są zachowywane bez zmian, a szczegóły zwracają kafelki w kolejności pozycji
  niezależnie od kolejności w żądaniu.
- [ ] Ten sam kafelek może występować w dwóch różnych widokach.
- [ ] Dwa widoki o tej samej nazwie zapisują się poprawnie.
- [ ] Edycja kafelka zachowuje jego pozycję i klasę CSS w widokach, które go zawierają.
- [ ] Usunięcie kafelka zdejmuje go ze wszystkich widoków, które go zawierały; widoki pozostają, a ich liczba kafelków
  maleje.

Walidacja (TEMP-025):

- [ ] Brak nazwy, nazwa z samych spacji lub dłuższa niż 100 znaków → 400 (`name`).
- [ ] Brak `tileId` → 400 (`tiles[i].tileId`).
- [ ] Odwołanie do nieistniejącego kafelka → 400 (`tiles[i].tileId`) z identyfikatorem w komunikacie.
- [ ] Ten sam kafelek użyty w widoku więcej niż raz → 400 (`tiles[i].tileId` dla każdego powtórzenia).
- [ ] Brak pozycji lub pozycja mniejsza niż 0 → 400 (`tiles[i].position`).
- [ ] Pozycja powtórzona w widoku → 400 (`tiles[i].position` dla każdego powtórzenia).
- [ ] Klasa CSS dłuższa niż 200 znaków (po obcięciu spacji) → 400 (`tiles[i].cssClass`); dokładnie 200 znaków jest
  dozwolone.
- [ ] Przy błędzie walidacji nic nie zostaje zapisane (lista bez zmian).

Przeglądanie (TEMP-026):

- [ ] Lista zwraca id, nazwę i liczbę kafelków każdego widoku w kolejności dodania; bez widoków lista jest pusta;
  widok bez kafelków ma `tileCount: 0`.
- [ ] Szczegóły zwracają nazwę i kafelki w kolejności pozycji z identyfikatorem, nazwą, pozycją i klasą CSS każdego
  kafelka; nazwa kafelka odzwierciedla jego aktualną nazwę.
- [ ] Szczegóły nieistniejącego widoku (lub niepoprawnego Guid) → 404.

Edycja (TEMP-027):

- [ ] Edycja zastępuje nazwę i całą listę kafelków (dodane, usunięte, zmienione pozycje i klasy); id i pozycja na
  liście widoków bez zmian.
- [ ] Edycja podlega tym samym regułom walidacji (400) i przy błędzie niczego nie zmienia.
- [ ] Edycja nieistniejącego widoku → 404.

Usunięcie (TEMP-028):

- [ ] Usunięty widok znika z listy, a jego szczegóły zwracają 404.
- [ ] Kafelki i połączenia użyte w widoku pozostają niezmienione (szczegóły kafelka bez zmian, kafelek nadal w innych
  widokach).
- [ ] Usunięcie nieistniejącego widoku → 404.

## Endpoint

Prefiks: `/api/custom-data-view/views`. Plik `CustomDataView.Api/Endpoints/ViewEndpoints.cs` (`MapViewEndpoints`).
Żądanie `ViewRequest` z metodą `Validate()` (mapowany na `ViewInput` i sprawdzany przez `ViewDefinitionValidator`).
Odpowiedzi bez opakowania w `ApiResponse` (jak połączenia i kafelki).

`ViewRequest`:

```json
{
  "name": "Pulpit klienta",
  "tiles": [
    { "tileId": "3f2a…", "position": 0, "cssClass": "col-6 highlight" },
    { "tileId": "9b1c…", "position": 5 }
  ]
}
```

| Metoda i ścieżka    | Wejście       | Sukces                                          | Błędy    |
|---------------------|---------------|-------------------------------------------------|----------|
| `POST /`            | `ViewRequest` | 201 `{ "id": "…" }`, `Location: …/{id}`         | 400      |
| `GET /`             | —             | 200 `[ { "id", "name", "tileCount" } ]`         | —        |
| `GET /{id:guid}`    | —             | 200 szczegóły                                   | 404      |
| `PUT /{id:guid}`    | `ViewRequest` | 204                                             | 400, 404 |
| `DELETE /{id:guid}` | —             | 204                                             | 404      |

Szczegóły (200):

```json
{
  "id": "…",
  "name": "Pulpit klienta",
  "tiles": [
    { "tileId": "3f2a…", "tileName": "Klient", "position": 0, "cssClass": "col-6 highlight" },
    { "tileId": "9b1c…", "tileName": "Zamówienia", "position": 5, "cssClass": null }
  ]
}
```

Błąd walidacji (400) — `Results.ValidationProblem(errors)` z kluczami z Zadań 2 i 3. `NotFoundException` → 404,
`ValidationException` → 400 w istniejącym `CustomDataViewExceptionHandler`.

## Walidacja wejścia

- `name`: wymagana, niepusta po obcięciu spacji, 1-100 znaków, bez unikalności.
- `tiles`: opcjonalna; brak lub `null` = pusta lista; bez limitu liczby kafelków.
- `tiles[i].tileId`: wymagany Guid istniejącego kafelka, unikalny w widoku.
- `tiles[i].position`: wymagana liczba całkowita >= 0, unikalna w widoku; luki dozwolone.
- `tiles[i].cssClass`: opcjonalna; po obcięciu spacji najwyżej 200 znaków; pusta → `null`; format nie jest
  sprawdzany.
- `id` w ścieżce: Guid (ograniczenie trasy; niepoprawny → 404).

## Handler i sekwencja wykonania

Dodanie / edycja:

1. Endpoint deserializuje `ViewRequest` (niepoprawny JSON → 400).
2. `request.Validate()` → `ViewDefinitionValidator`; błędy → `ValidationProblem` 400, nic nie jest zapisywane.
3. `ViewInput.ToDefinition()` → `ViewDefinition` (klasa CSS obcięta, pusta → `null`).
4. `ViewService.Add` / `Update`: (edycja: brak widoku → 404) → `ViewReferencesValidator`
   (`ITileRepository.GetReferences`; błędy → 400) → `IViewRepository.Add` / `Update` w jednej transakcji.
5. 201 z `id` / 204.

Odczyt: `ViewService.GetAll` → `IViewRepository.GetAll` → 200. `ViewService.Get` → `IViewRepository.Get` (brak → 404)
→ `ITileRepository.GetReferences(ids)` → `ViewDto` posortowane po pozycji → 200.

Usunięcie: `ViewService.Delete` → brak → 404; `IViewRepository.Delete` → 204.

`CustomDataView.Bootstrapper` wywołuje `AddCustomDataViewViews()` (rejestracja repozytorium, walidatora referencji
i serwisu) oraz `MapViewEndpoints()`.

## Baza danych

Schemat według `docs/foundation/db-plan.md` (tabele `views`, `view_tiles`; decyzje 22-27). Migracja
`CustomDataView.Core/Persistence/Migrations/20261014120000_create_views.sql` tworzy tabele widoków, klucze obce
(`view_tiles.view_id → views.id` i `view_tiles.tile_id → tiles.id`, oba `ON DELETE CASCADE`), `CHECK (position >= 0)`,
`UNIQUE (view_id, position)` oraz indeksy `ix_views_created_at_id`, `ix_view_tiles_tile_id`; aplikacja jej nie
wykonuje — uruchamia się ją ręcznie.

## Repozytoria

- Nowe `IViewRepository` / `PostgresViewRepository` (Zadanie 4).
- `ITileRepository.GetReferences(ids)` — nowa metoda w `PostgresTileRepository` (Zadanie 3). Pozostałe metody bez
  zmian.
- W testach API: `TestViewRepository` (w pamięci, bez bibliotek mockujących). `TestTileRepository.Delete` zdejmuje
  usuwany kafelek z `TestViewRepository`, naśladując FK `ON DELETE CASCADE`; `TestTileRepository` implementuje
  `GetReferences`.

## Frontend

Brak zmian.

## Testy

### Testy jednostkowe

- **`ViewDefinitionValidator`:** każda reguła lokalna z kluczem błędu (nazwa, brak `tileId`, duplikat kafelka, brak
  i ujemna pozycja, duplikat pozycji, klasa CSS 200/201 znaków po obcięciu), błąd tylko dla powtórzeń po pierwszym
  wystąpieniu, poprawny pusty widok.
- **`ViewInput.ToDefinition`:** obcięcie klasy CSS, pusta i z samych spacji → `null`, brak `tiles` → pusta lista.

### Testy integracyjne

> Testy integracyjne dla tego scenariusza są pisane na podstawie scenariuszy Gherkin w
> `src/CustomDataView/docs/scenarios/scenario_TEMP-024-028/gherkin.md` (API przez `CustomDataViewApplicationFactory`,
> repozytoria w pamięci). `PostgresViewRepository` i `PostgresTileRepository.GetReferences` mają testy
> w `CustomDataView.Core.Persistence.Tests` (zapis i odczyt kolejności po pozycji i klasy CSS, lista z liczbą
> kafelków, edycja, usunięcie widoku z zachowaniem kafelków, kaskadowe zdjęcie usuniętego kafelka z widoków, edycja
> kafelka bez zmiany `view_tiles`).
