# Podsumowanie analizy bazy danych — CustomDataView (połączenia)

## Zakres

- Tylko definicje połączeń (PRD 3.2, historyjki TEMP-007 … TEMP-014). Kafelki i widoki zostaną dodane w kolejnych
  planach.
- Silnik: PostgreSQL. Dostęp: Dapper, bez Entity Framework.
- Schemat: `custom_data_view` (snake_case nazwy modułu).

## Tabele

### connections

| Kolumna    | Typ          | Ograniczenia                                     |
|------------|--------------|--------------------------------------------------|
| id         | uuid         | PK, NOT NULL (nadawany przez aplikację)          |
| name       | varchar(100) | NOT NULL                                         |
| method     | varchar(4)   | NOT NULL, CHECK (method IN ('GET', 'POST'))      |
| url        | text         | NOT NULL (szablon z `{name}` w ścieżce)          |
| body       | json         | NULL; CHECK (method = 'POST' OR body IS NULL)    |
| created_at | timestamptz  | NOT NULL, DEFAULT now()                          |
| updated_at | timestamptz  | NOT NULL, DEFAULT now()                          |

### connection_path_parameters

| Kolumna       | Typ         | Ograniczenia                                    |
|---------------|-------------|-------------------------------------------------|
| connection_id | uuid        | PK (1/2), FK → connections.id ON DELETE CASCADE |
| name          | varchar(50) | PK (2/2), NOT NULL                              |
| required      | boolean     | NOT NULL                                        |
| default_value | text        | NULL                                            |

### connection_query_parameters

| Kolumna       | Typ     | Ograniczenia                                    |
|---------------|---------|-------------------------------------------------|
| connection_id | uuid    | PK (1/2), FK → connections.id ON DELETE CASCADE |
| name          | text    | PK (2/2), NOT NULL                              |
| required      | boolean | NOT NULL                                        |
| default_value | text    | NULL                                            |

### connection_body_parameters

| Kolumna       | Typ         | Ograniczenia                                    |
|---------------|-------------|-------------------------------------------------|
| connection_id | uuid        | PK (1/2), FK → connections.id ON DELETE CASCADE |
| name          | varchar(50) | PK (2/2), NOT NULL                              |
| required      | boolean     | NOT NULL                                        |
| default_value | json        | NULL (dowolna wartość JSON)                     |

## Relacje

- Każda z tabel `connection_path_parameters`, `connection_query_parameters`, `connection_body_parameters` →
  `connections`: wiele-do-jednego, `ON DELETE CASCADE`.

## Indeksy

- PK `connections(id)` — odczyt, edycja, usunięcie, test po id.
- `ix_connections_created_at_id` na `connections(created_at, id)` — lista w kolejności dodania.
- PK `(connection_id, name)` w tabelach parametrów — wczytanie parametrów połączenia (`ORDER BY name`) i unikalność
  nazw; osobne indeksy nie są potrzebne.

## RLS

- Brak. W MVP wszystkie definicje są wspólne, bez użytkowników.

## Decyzje

1. Jedna baza zapisu i odczytu. Jedno repozytorium `IConnectionRepository` (dodanie, edycja, usunięcie, odczyt po id,
   lista) — bez osobnego interfejsu odczytu i bez osobnych tabel read-modelu.
2. Rodzaj połączenia w kolumnie `method` z CHECK; repozytorium mapuje na `GetConnectionDefinition` /
   `PostConnectionDefinition`. Szablon treści dozwolony tylko dla POST (CHECK).
3. Szablon treści w typie `json` (nie `jsonb`), aby zachować dokładny tekst i kolejność kluczy.
4. Każdy typ parametru w osobnej tabeli. PK `(connection_id, name)`, bez kolumny kolejności — kolejność deklaracji nie
   wpływa na wywołanie; odczyt sortuje po `name`. Edycja zastępuje wszystkie wiersze parametrów
   (DELETE + INSERT) w jednej transakcji z aktualizacją `connections`.
5. Wartości domyślne: `text` dla ścieżki i query, `json` dla treści.
6. Kolejność listy: `ORDER BY created_at, id`; `created_at` nie zmienia się przy edycji, `updated_at` ustawia
   repozytorium.
7. Unikalność nazw parametrów (ścieżka, query, treść) — w walidatorze i przez PK w bazie. Zgodność placeholderów
   z deklaracjami — tylko w aplikacji.
   Parametry treści tylko dla POST — tylko w aplikacji (reguła międzytabelowa).
8. W bazie tylko podstawowe ograniczenia: NOT NULL, CHECK metody i treści, `name varchar(100)`, nazwy parametrów
   ścieżki i treści `varchar(50)`. Format URL i snake_case — w aplikacji.
9. Twarde usuwanie z kaskadą na parametry. Blokadę usunięcia połączenia używanego przez kafelki (TEMP-020) zapewni
   przyszły FK z tabeli pól kafelka (`ON DELETE RESTRICT`).
10. Brak zdarzeń integracyjnych dla połączeń — outbox nie jest potrzebny w tym zakresie.
11. Tabele parametrów nie mają `created_at` ani `updated_at` — parametry zapisuje się zawsze razem z połączeniem,
    więc znaczniki czasu wynikają z `connections`.
12. Brak tabeli nagłówków — nagłówki są stałe i predefiniowane w kodzie (treść POST zawsze jako
    `Content-Type: application/json`).

---

# Podsumowanie analizy bazy danych — CustomDataView (widoki)

## Zakres

- Definicje widoków (PRD 3.4, historyjki TEMP-024 … TEMP-028). Rozszerzenie istniejącego planu połączeń i kafelków.
- Silnik: PostgreSQL, Dapper, schemat `custom_data_view`.
- Zmiana wymagań: układ kafelka w widoku wyznacza kolejność (`position`) i opcjonalna klasa CSS (`css_class`),
  a nie wiersz, kolumna, szerokość i wysokość. PRD (3.4, TEMP-024, TEMP-025) zostaje zaktualizowane.

## Tabele

### views

| Kolumna    | Typ          | Ograniczenia                            |
|------------|--------------|-----------------------------------------|
| id         | uuid         | PK, NOT NULL (nadawany przez aplikację) |
| name       | varchar(100) | NOT NULL, bez unikalności               |
| created_at | timestamptz  | NOT NULL, DEFAULT now()                 |
| updated_at | timestamptz  | NOT NULL, DEFAULT now()                 |

### view_tiles

| Kolumna   | Typ          | Ograniczenia                                     |
|-----------|--------------|--------------------------------------------------|
| view_id   | uuid         | PK (1/2), FK → views.id ON DELETE CASCADE        |
| tile_id   | uuid         | PK (2/2), FK → tiles.id ON DELETE CASCADE        |
| position  | integer      | NOT NULL, CHECK (position >= 0), UNIQUE (view_id, position) |
| css_class | varchar(200) | NULL                                             |

## Relacje

- `view_tiles` → `views`: wiele-do-jednego, `ON DELETE CASCADE`.
- `view_tiles` → `tiles`: wiele-do-jednego, `ON DELETE CASCADE`.
- `views` ↔ `tiles`: wiele-do-wielu przez `view_tiles`.

## Indeksy

- PK `views(id)` — szczegóły, edycja, usunięcie po id.
- `ix_views_created_at_id` na `views(created_at, id)` — lista w kolejności dodania.
- PK `view_tiles(view_id, tile_id)` — kafelek najwyżej raz w widoku, wczytanie siatki, kaskada z `views`.
- `uq_view_tiles_view_id_position` na `view_tiles(view_id, position)` — unikalna pozycja, wczytanie siatki
  `ORDER BY position`.
- `ix_view_tiles_tile_id` na `view_tiles(tile_id)` — kaskadowe usunięcie kafelka z widoków bez pełnego skanu.

## RLS

- Brak. W MVP wszystkie definicje są wspólne.

## Decyzje

1. Jedna baza zapisu i odczytu; jedno repozytorium `IViewRepository` (dodanie, edycja, usunięcie, szczegóły, lista).
2. Siatka w osobnej tabeli `view_tiles`, bez kolumn JSON — spójnie z kafelkami.
3. Brak kolumn row/column/width/height (decyzja dewelopera). Układ = kolejność `position` + `css_class`.
4. `position` integer >= 0, unikalna w widoku; szczegóły zwracają kafelki `ORDER BY position`.
5. `css_class` varchar(200) NULL; walidacja tylko długości (bez formatu).
6. PK `(view_id, tile_id)` gwarantuje, że kafelek występuje w widoku najwyżej raz.
7. Usunięcie kafelka zdejmuje go ze wszystkich widoków przez FK `ON DELETE CASCADE`; widoki pozostają. Edycja kafelka
   (`UPDATE tiles`) nie dotyka `view_tiles`, więc kafelek zachowuje miejsce w widokach.
8. Usunięcie widoku kaskadowo usuwa `view_tiles`; kafelki i połączenia pozostają.
9. Edycja widoku: `UPDATE views` + `DELETE` / `INSERT` `view_tiles` w jednej transakcji.
10. Istnienie kafelka sprawdza walidator (czytelny błąd); FK jest zabezpieczeniem w bazie.
11. Lista `ORDER BY created_at, id`; liczba kafelków przez `COUNT` w zapytaniu, bez denormalizacji.
12. W bazie proste ograniczenia (NOT NULL, PK, FK, UNIQUE, CHECK `position >= 0`, długości); reszta w kodzie.
13. Brak outboxa i RLS.
