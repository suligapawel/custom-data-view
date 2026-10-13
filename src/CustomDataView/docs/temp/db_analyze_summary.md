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
