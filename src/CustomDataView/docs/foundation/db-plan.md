# Database Plan — CustomDataView

## Overview

Plan obejmuje definicje połączeń do zewnętrznych REST API (PRD 3.2, TEMP-007 … TEMP-014), definicje kafelków
(PRD 3.3, TEMP-015 … TEMP-023) oraz definicje widoków (PRD 3.4, TEMP-024 … TEMP-028). Tabela
`connections` przechowuje szablon żądania GET lub POST, a każdy typ parametru — deklaracje parametrów ścieżki, query
i treści — ma własną tabelę podrzędną powiązaną z połączeniem relacją wiele-do-jednego. Nagłówki nie są
przechowywane: są stałe i predefiniowane w kodzie.
Kafelek (`tiles`) ma uporządkowaną listę pól (`tile_fields`), a każde pole wskazuje połączenie, ścieżkę JSONPath,
etykietę oraz uporządkowany łańcuch transformacji (`tile_field_transformations`) z parametrami
(`tile_field_transformation_parameters`). Kafelki nie przechowują wartości parametrów połączeń — przychodzą one z frontu
przy każdym wywołaniu.
Wszystkie tabele leżą w schemacie `custom_data_view`; dostęp przez EF Core (provider Npgsql), bez kolumn JSON
w tabelach kafelków i widoków (szczegóły w sekcji „Mapowanie EF Core”).
Widok (`views`) ma uporządkowaną listę kafelków (`view_tiles`): każdy wpis wskazuje kafelek, jego pozycję w widoku
i opcjonalną klasę CSS, która opisuje sposób wyświetlenia. Kafelek występuje w widoku najwyżej raz, a jego usunięcie
kaskadowo zdejmuje go ze wszystkich widoków.

## Tables

### custom_data_view.connections

| Column     | Type         | Constraints                                   | Description                                                     |
|------------|--------------|-----------------------------------------------|-----------------------------------------------------------------|
| id         | uuid         | PK, NOT NULL                                  | Identyfikator połączenia nadawany przez aplikację (`Guid.NewGuid()`) |
| name       | varchar(100) | NOT NULL                                      | Nazwa połączenia; bez wymogu unikalności                        |
| method     | varchar(4)   | NOT NULL, CHECK (method IN ('GET', 'POST'))   | Rodzaj połączenia                                               |
| url        | text         | NOT NULL                                      | Szablon URL z parametrami ścieżki `{name}`                      |
| body       | json         | NULL, CHECK (method = 'POST' OR body IS NULL) | Szablon treści POST z miejscami `"{name}"`; NULL = brak treści  |
| created_at | timestamptz  | NOT NULL, DEFAULT now()                       | Moment dodania; wyznacza kolejność listy; nie zmienia się przy edycji |
| updated_at | timestamptz  | NOT NULL, DEFAULT now()                       | Moment ostatniej edycji; ustawiany przez repozytorium           |

### custom_data_view.connection_path_parameters

| Column        | Type        | Constraints                                         | Description                                  |
|---------------|-------------|-----------------------------------------------------|----------------------------------------------|
| connection_id | uuid        | PK, NOT NULL, FK → connections.id ON DELETE CASCADE | Połączenie                                   |
| name          | varchar(50) | PK, NOT NULL                                        | Nazwa placeholdera `{name}` w URL (snake_case) |
| required      | boolean     | NOT NULL                                            | Czy wartość jest wymagana przy wywołaniu     |
| default_value | text        | NULL                                                | Wartość domyślna; NULL = brak                |

PK: `(connection_id, name)`.

### custom_data_view.connection_query_parameters

| Column        | Type    | Constraints                                         | Description                                   |
|---------------|---------|-----------------------------------------------------|-----------------------------------------------|
| connection_id | uuid    | PK, NOT NULL, FK → connections.id ON DELETE CASCADE | Połączenie                                    |
| name          | text    | PK, NOT NULL                                        | Klucz query zewnętrznego API; unikalny         |
| required      | boolean | NOT NULL                                            | Czy wartość jest wymagana przy wywołaniu      |
| default_value | text    | NULL                                                | Wartość domyślna; NULL = brak                 |

PK: `(connection_id, name)`.

### custom_data_view.connection_body_parameters

| Column        | Type        | Constraints                                         | Description                                        |
|---------------|-------------|-----------------------------------------------------|----------------------------------------------------|
| connection_id | uuid        | PK, NOT NULL, FK → connections.id ON DELETE CASCADE | Połączenie (tylko POST — pilnuje aplikacja)        |
| name          | varchar(50) | PK, NOT NULL                                        | Nazwa miejsca `"{name}"` w szablonie treści (snake_case) |
| required      | boolean     | NOT NULL                                            | Czy wartość jest wymagana przy wywołaniu           |
| default_value | json        | NULL                                                | Wartość domyślna (dowolna wartość JSON); NULL = brak |

PK: `(connection_id, name)`.

### custom_data_view.tiles

| Column     | Type         | Constraints             | Description                                                     |
|------------|--------------|-------------------------|-----------------------------------------------------------------|
| id         | uuid         | PK, NOT NULL            | Identyfikator kafelka nadawany przez aplikację (`Guid.NewGuid()`) |
| name       | varchar(100) | NOT NULL                | Nazwa kafelka; bez wymogu unikalności                           |
| created_at | timestamptz  | NOT NULL, DEFAULT now() | Moment dodania; wyznacza kolejność listy; nie zmienia się przy edycji |
| updated_at | timestamptz  | NOT NULL, DEFAULT now() | Moment ostatniej edycji; ustawiany przez repozytorium           |

### custom_data_view.tile_fields

| Column        | Type         | Constraints                                      | Description                                  |
|---------------|--------------|--------------------------------------------------|----------------------------------------------|
| tile_id       | uuid         | PK, NOT NULL, FK → tiles.id ON DELETE CASCADE    | Kafelek                                      |
| position      | integer      | PK, NOT NULL, CHECK (position >= 0)              | Pozycja pola na liście (od 0); tożsamość pola |
| connection_id | uuid         | NOT NULL, FK → connections.id ON DELETE RESTRICT | Połączenie zasilające pole                   |
| path          | text         | NOT NULL                                         | Ścieżka JSONPath (podzbiór składni z PRD 3.3) |
| label         | varchar(100) | NOT NULL                                         | Etykieta pola; bez wymogu unikalności        |

PK: `(tile_id, position)`.

### custom_data_view.tile_field_transformations

| Column         | Type        | Constraints                         | Description                              |
|----------------|-------------|-------------------------------------|------------------------------------------|
| tile_id        | uuid        | PK, NOT NULL, FK (z field_position) | Kafelek                                  |
| field_position | integer     | PK, NOT NULL, FK (z tile_id)        | Pozycja pola                             |
| position       | integer     | PK, NOT NULL, CHECK (position >= 0) | Pozycja transformacji w łańcuchu (od 0)  |
| key            | varchar(50) | NOT NULL                            | Klucz transformacji z katalogu w kodzie  |

PK: `(tile_id, field_position, position)`. FK: `(tile_id, field_position)` → `tile_fields (tile_id, position)`
`ON DELETE CASCADE`.

### custom_data_view.tile_field_transformation_parameters

| Column                  | Type        | Constraints  | Description                                         |
|-------------------------|-------------|--------------|-----------------------------------------------------|
| tile_id                 | uuid        | PK, NOT NULL | Kafelek                                             |
| field_position          | integer     | PK, NOT NULL | Pozycja pola                                        |
| transformation_position | integer     | PK, NOT NULL | Pozycja transformacji w łańcuchu                    |
| name                    | varchar(50) | PK, NOT NULL | Nazwa parametru transformacji (np. `decimals`)      |
| value                   | text        | NOT NULL     | Wartość jako tekst; typ odtwarzany z deskryptora transformacji |

PK: `(tile_id, field_position, transformation_position, name)`. FK: `(tile_id, field_position,
transformation_position)` → `tile_field_transformations (tile_id, field_position, position)` `ON DELETE CASCADE`.

### custom_data_view.views

| Column     | Type         | Constraints             | Description                                                     |
|------------|--------------|-------------------------|-----------------------------------------------------------------|
| id         | uuid         | PK, NOT NULL            | Identyfikator widoku nadawany przez aplikację (`Guid.NewGuid()`) |
| name       | varchar(100) | NOT NULL                | Nazwa widoku; bez wymogu unikalności                            |
| created_at | timestamptz  | NOT NULL, DEFAULT now() | Moment dodania; wyznacza kolejność listy; nie zmienia się przy edycji |
| updated_at | timestamptz  | NOT NULL, DEFAULT now() | Moment ostatniej edycji; ustawiany przez repozytorium           |

### custom_data_view.view_tiles

| Column    | Type         | Constraints                                   | Description                                         |
|-----------|--------------|-----------------------------------------------|-----------------------------------------------------|
| view_id   | uuid         | PK, NOT NULL, FK → views.id ON DELETE CASCADE | Widok                                               |
| tile_id   | uuid         | PK, NOT NULL, FK → tiles.id ON DELETE CASCADE | Kafelek umieszczony w widoku                        |
| position  | integer      | NOT NULL, CHECK (position >= 0)               | Pozycja kafelka w widoku (od 0); unikalna w widoku  |
| css_class | varchar(200) | NULL                                          | Klasa CSS (jedna lub kilka) opisująca wyświetlenie kafelka; NULL = brak |

PK: `(view_id, tile_id)`. UNIQUE: `(view_id, position)`.

## Relationships

| From table                  | To table    | Type        | Foreign key column                                        |
|-----------------------------|-------------|-------------|-----------------------------------------------------------|
| connection_path_parameters  | connections | many-to-one | connection_path_parameters.connection_id → connections.id  |
| connection_query_parameters | connections | many-to-one | connection_query_parameters.connection_id → connections.id |
| connection_body_parameters  | connections | many-to-one | connection_body_parameters.connection_id → connections.id  |
| tile_fields                 | tiles       | many-to-one | tile_fields.tile_id → tiles.id                             |
| tile_fields                 | connections | many-to-one | tile_fields.connection_id → connections.id                 |
| tile_field_transformations  | tile_fields | many-to-one | (tile_id, field_position) → tile_fields (tile_id, position) |
| tile_field_transformation_parameters | tile_field_transformations | many-to-one | (tile_id, field_position, transformation_position) → tile_field_transformations (tile_id, field_position, position) |
| view_tiles                  | views       | many-to-one | view_tiles.view_id → views.id                              |
| view_tiles                  | tiles       | many-to-one | view_tiles.tile_id → tiles.id                              |

Wszystkie klucze obce: `ON DELETE CASCADE`, z wyjątkiem `tile_fields.connection_id → connections.id`
(`ON DELETE RESTRICT`).

## Indexes

- `pk_connections` (`connections(id)`) — szczegóły, edycja, usunięcie i test połączenia po identyfikatorze
  (TEMP-010 … TEMP-013).
- `ix_connections_created_at_id` (`connections(created_at, id)`) — lista połączeń w kolejności dodania:
  `ORDER BY created_at, id` (TEMP-010).
- `pk_connection_path_parameters`, `pk_connection_query_parameters`, `pk_connection_body_parameters`
  (`(connection_id, name)`) — wczytanie parametrów połączenia (`WHERE connection_id = @id ORDER BY name`), kaskadowe
  usuwanie i unikalność nazw w obrębie połączenia. Wiodąca kolumna `connection_id` obsługuje FK, więc dodatkowe
  indeksy nie są potrzebne.
- `pk_tiles` (`tiles(id)`) — szczegóły, edycja, usunięcie i podgląd kafelka po identyfikatorze (TEMP-017 … TEMP-019,
  TEMP-021).
- `ix_tiles_created_at_id` (`tiles(created_at, id)`) — lista kafelków w kolejności dodania (TEMP-017).
- `pk_tile_fields` (`(tile_id, position)`), `pk_tile_field_transformations`,
  `pk_tile_field_transformation_parameters` — wczytanie definicji kafelka w kolejności pozycji (`WHERE tile_id = @id
  ORDER BY …position`) i kaskadowe usuwanie; wiodąca kolumna `tile_id` obsługuje FK.
- `ix_tile_fields_connection_id` (`tile_fields(connection_id)`) — wyszukanie kafelków korzystających z połączenia
  przy blokadzie jego usunięcia (TEMP-020) i obsługa FK `RESTRICT`.
- `pk_views` (`views(id)`) — szczegóły, edycja i usunięcie widoku po identyfikatorze (TEMP-026 … TEMP-028).
- `ix_views_created_at_id` (`views(created_at, id)`) — lista widoków w kolejności dodania (TEMP-026).
- `pk_view_tiles` (`(view_id, tile_id)`) — kafelek najwyżej raz w widoku (TEMP-025), kaskadowe usuwanie z `views`;
  wiodąca kolumna `view_id` obsługuje FK i `COUNT` kafelków na liście widoków (TEMP-026).
- `uq_view_tiles_view_id_position` (`(view_id, position)`) — unikalna pozycja w widoku i wczytanie kafelków widoku
  w kolejności (`WHERE view_id = @id ORDER BY position`).
- `ix_view_tiles_tile_id` (`view_tiles(tile_id)`) — kaskadowe zdjęcie usuwanego kafelka ze wszystkich widoków
  (TEMP-024) bez pełnego skanu `view_tiles`.

## Mapowanie EF Core

- `CustomDataViewDbContext` (`CustomDataView.Infrastructure/Persistence/`) ma domyślny schemat `custom_data_view`
  i ładuje konfiguracje z `Persistence/Configurations/` (`IEntityTypeConfiguration<T>`, jedna klasa na tabelę).
- Encje EF to modele wierszy w `Persistence/Rows/` (`ConnectionRow`, `TileFieldRow`, `ViewTileRow` itd.), oddzielone
  od modelu domeny. Repozytoria mapują wiersze na `Connection`, `Tile` i `View` oraz z powrotem.
- Konfiguracje odwzorowują ten plan 1:1: nazwy tabel i kolumn, typy (`json`, `text`, `varchar(n)`, `timestamptz`),
  klucze złożone, nazwy `pk_*`, `fk_*`, `ix_*`, `uq_*`, `OnDelete` (`Cascade`, a dla `tile_fields.connection_id`
  `Restrict`), CHECK przez `HasCheckConstraint` oraz `UNIQUE (view_id, position)` jako klucz alternatywny.
  Skrypt wygenerowany z modelu (`GenerateCreateScript`) jest zgodny z migracjami SQL.
- Identyfikatory i pozycje nadaje aplikacja (`ValueGeneratedNever`). `created_at` i `updated_at` przy dodaniu
  wypełnia `DEFAULT now()` bazy; edycja ustawia `updated_at` w `ExecuteUpdate` (`DateTime.UtcNow` tłumaczone na
  `now()`).
- Repozytoria są singletonami i tworzą krótkożyjący kontekst z `IDbContextFactory<CustomDataViewDbContext>` na każdą
  operację. Odczyty używają `AsNoTracking`, uporządkowanych `Include` i `AsSplitQuery`. Edycja to `ExecuteUpdate`
  wiersza głównego, `ExecuteDelete` wierszy podrzędnych i `SaveChanges` nowych wierszy w jednej transakcji
  (decyzje 5, 16 i 26). Usunięcie to `ExecuteDelete`, a kaskady i `RESTRICT` wykonuje baza.
- Schemat nadal tworzą migracje SQL w `Persistence/Migrations/`. Migracje EF Core nie zostały jeszcze wygenerowane.

## Design decisions

1. **Jedna baza zapisu i odczytu.** Jedno repozytorium `IConnectionRepository` obsługuje dodanie, edycję, usunięcie,
   odczyt po id i listę połączeń (decyzja dewelopera) — bez osobnego interfejsu odczytu i bez osobnych tabel
   read-modelu.
2. **Rodzaj połączenia jako kolumna `method` z CHECK.** Jedna tabela upraszcza listę i przyszły FK z pól kafelków;
   repozytorium mapuje wiersz na `GetConnectionDefinition` albo `PostConnectionDefinition`. CHECK gwarantuje, że
   szablon treści istnieje tylko dla POST.
3. **Szablon treści w `json`, nie `jsonb`.** PRD wymaga zwracania body bez zmian; `json` zachowuje dokładny tekst
   i kolejność kluczy, a nadal waliduje składnię. Zawartość body nie jest filtrowana ani indeksowana.
4. **Każdy typ parametru w osobnej tabeli** (decyzja dewelopera). Różne typy wartości domyślnych (text vs json),
   i różne limity nazw w każdej sekcji.
5. **PK `(connection_id, name)` bez własnego id parametru i bez kolumny kolejności** (decyzja dewelopera). Parametry
   są identyfikowane nazwą, a kolejność deklaracji nie wpływa na wywołanie: ścieżka i treść podstawiają wartości po
   nazwie, a kolejność w query stringu jest nieistotna. Odczyt zwraca parametry posortowane po `name`. Edycja
   zastępuje całą definicję: `UPDATE connections` + `DELETE` i ponowny `INSERT` wierszy parametrów w jednej
   transakcji.
6. **Wartości domyślne:** `text` dla ścieżki i query (zawsze tekst), `json` dla treści (dowolna wartość JSON).
   Repozytorium mapuje je na obecny model w kodzie (surowy tekst JSON w `ParameterDeclaration.DefaultValue`).
7. **Kolejność listy po `created_at, id`.** `created_at` ustawiane przy dodaniu i niezmieniane przy edycji, więc
   edytowane połączenie zachowuje pozycję; `id` rozstrzyga remisy.
8. **Unikalność nazw parametrów w aplikacji i w bazie.** Nazwy ścieżki, query i treści są unikalne w obrębie
   połączenia; walidator zwraca czytelny błąd, a PK `(connection_id, name)` jest zabezpieczeniem w bazie. Wiele
   wartości query przekazuje się w czasie wywołania jako listę, więc powtarzanie deklaracji nie jest potrzebne.
   Zgodność placeholderów z deklaracjami i parametry treści tylko dla POST egzekwuje walidator.
9. **W bazie tylko podstawowe ograniczenia:** NOT NULL, PK, CHECK metody i treści, `name varchar(100)`, nazwy
   parametrów ścieżki i treści `varchar(50)`. Format URL i snake_case pozostają w aplikacji.
10. **Twarde usuwanie z kaskadą.** PRD nie wymaga historii ani przywracania. Blokadę usunięcia połączenia używanego
    przez pola kafelków (TEMP-020) sprawdza aplikacja, a FK `tile_fields.connection_id` z `ON DELETE RESTRICT`
    zabezpiecza ją w bazie (decyzja 19).
11. **Brak outboxa dla połączeń.** Operacje na połączeniach nie publikują zdarzeń integracyjnych w MVP.
12. **Brak RLS.** W MVP wszystkie definicje są wspólne i nie ma użytkowników.
13. **Brak `created_at` i `updated_at` w tabelach parametrów** (decyzja dewelopera). Parametry są zapisywane zawsze
    razem z połączeniem (edycja odtwarza wszystkie wiersze — decyzja 5), więc ich znaczniki czasu wynikają
    z `connections.created_at` i `connections.updated_at`.
14. **Brak tabeli nagłówków** (decyzja dewelopera). Wszystkie połączenia wysyłają te same nagłówki, predefiniowane
    w kodzie (treść POST zawsze jako `Content-Type: application/json`), więc definicja połączenia ich nie zawiera.
15. **Kafelek w czterech tabelach, bez kolumn JSON** (decyzja dewelopera). Kafelek, pola, transformacje i parametry
    transformacji są osobnymi tabelami; klucz transformacji jest zapisany jako tekst (`key`).
16. **Pola i transformacje identyfikowane pozycją.** PK złożone z `position` zachowuje kolejność pól i łańcucha
    transformacji bez osobnych identyfikatorów. Etykiety pól nie muszą być unikalne. Edycja zastępuje całą definicję:
    `UPDATE tiles` + `DELETE` pól (transformacje i parametry kaskadowo) i ponowny `INSERT` w jednej transakcji.
17. **Klucz transformacji bez FK.** Katalog transformacji żyje w kodzie; istnienie klucza sprawdza walidator przy
    zapisie.
18. **Parametry transformacji jako `text`.** Wartość (tekst lub liczba z żądania) jest zapisywana jako tekst, a typ
    (`integer` / `string`) odtwarzany przy odczycie z deskryptora transformacji w katalogu. Parametry nie są walidowane
    przy zapisie (decyzja dewelopera) — brak lub zły zakres ujawnia podgląd kafelka.
19. **`ON DELETE RESTRICT` z pól do połączeń.** Usunięcie kafelka nie zmienia połączeń; usunięcie połączenia używanego
    przez pole jest blokowane. Aplikacja zwraca 409 z listą kafelków (`WHERE connection_id = @id`), a FK chroni przed
    wyścigiem.
20. **Kolejność listy kafelków po `created_at, id`** — jak w połączeniach (decyzja 7).
21. **Wartości parametrów połączeń nie są przechowywane.** Przychodzą z frontu przy każdym podglądzie lub pobraniu
    danych, więc jeden kafelek obsługuje wielu klientów. Wyniki podglądu nie są zapisywane.
22. **Siatka widoku w tabeli `view_tiles`, bez kolumn JSON** (decyzja dewelopera) — spójnie z kafelkami (decyzja 15).
    Jedno repozytorium `IViewRepository` obsługuje zapis i odczyt widoków (jak decyzja 1).
23. **Układ jako kolejność i klasa CSS zamiast wiersza, kolumny, szerokości i wysokości** (decyzja dewelopera).
    `position` zachowuje kolejność kafelków; `css_class` (`varchar(200)`, opcjonalna) przekazuje frontendowi sposób
    wyświetlenia. Aplikacja sprawdza tylko długość klasy, bez formatu. Reguły 12 kolumn i nachodzenia kafelków nie
    występują.
24. **PK `(view_id, tile_id)`** gwarantuje, że kafelek występuje w widoku najwyżej raz; `UNIQUE (view_id, position)`
    pilnuje jednoznacznej kolejności. Walidator zwraca czytelne błędy (duplikat, nieistniejący kafelek), a ograniczenia
    bazy są zabezpieczeniem.
25. **`ON DELETE CASCADE` z `view_tiles` do `tiles` i `views`.** Usunięcie kafelka zdejmuje go ze wszystkich widoków,
    które pozostają; usunięcie widoku nie zmienia kafelków ani połączeń. Edycja kafelka (`UPDATE tiles`) nie dotyka
    `view_tiles`, więc kafelek zachowuje swoje miejsce w widokach.
26. **Edycja widoku zastępuje całą definicję:** `UPDATE views` + `DELETE` i ponowny `INSERT` wierszy `view_tiles`
    w jednej transakcji.
27. **Lista widoków po `created_at, id`** (jak decyzja 7) z liczbą kafelków liczoną `COUNT` w zapytaniu — bez
    denormalizacji, więc licznik jest aktualny także po kaskadowym usunięciu kafelka.
