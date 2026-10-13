# Podsumowanie analizy bazy danych — CustomDataView (połączenia)

## Zakres

- Tylko definicje połączeń (PRD 3.2, historyjki TEMP-007 … TEMP-014). Kafelki i widoki zostaną dodane w kolejnych
  planach.
- Silnik: PostgreSQL. Dostęp: Dapper, bez Entity Framework.
- Schemat: `custom_data_view` (snake_case nazwy modułu). Pierwsza migracja tworzy schemat oraz
  `custom_data_view.__migration_history`.

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
| position      | int         | PK (2/2), NOT NULL, CHECK (position >= 0)       |
| name          | varchar(50) | NOT NULL                                        |
| required      | boolean     | NOT NULL                                        |
| default_value | text        | NULL                                            |
| created_at    | timestamptz | NOT NULL, DEFAULT now()                         |
| updated_at    | timestamptz | NOT NULL, DEFAULT now()                         |

### connection_query_parameters

| Kolumna       | Typ     | Ograniczenia                                    |
|---------------|---------|-------------------------------------------------|
| connection_id | uuid    | PK (1/2), FK → connections.id ON DELETE CASCADE |
| position      | int     | PK (2/2), NOT NULL, CHECK (position >= 0)       |
| name          | text    | NOT NULL (powtórzenia dozwolone)                |
| required      | boolean | NOT NULL                                        |
| default_value | text    | NULL                                            |
| created_at    | timestamptz | NOT NULL, DEFAULT now()                     |
| updated_at    | timestamptz | NOT NULL, DEFAULT now()                     |

### connection_body_parameters

| Kolumna       | Typ         | Ograniczenia                                    |
|---------------|-------------|-------------------------------------------------|
| connection_id | uuid        | PK (1/2), FK → connections.id ON DELETE CASCADE |
| position      | int         | PK (2/2), NOT NULL, CHECK (position >= 0)       |
| name          | varchar(50) | NOT NULL                                        |
| required      | boolean     | NOT NULL                                        |
| default_value | json        | NULL (dowolna wartość JSON)                     |
| created_at    | timestamptz | NOT NULL, DEFAULT now()                         |
| updated_at    | timestamptz | NOT NULL, DEFAULT now()                         |

## Relacje

- Każda z tabel `connection_path_parameters`, `connection_query_parameters`, `connection_body_parameters` →
  `connections`: wiele-do-jednego, `ON DELETE CASCADE`.

## Indeksy

- PK `connections(id)` — odczyt, edycja, usunięcie, test po id.
- `ix_connections_created_at_id` na `connections(created_at, id)` — lista w kolejności dodania.
- PK `(connection_id, position)` w tabelach parametrów — wczytanie parametrów połączenia w kolejności; osobne indeksy
  nie są potrzebne.

## RLS

- Brak. W MVP wszystkie definicje są wspólne, bez użytkowników.

## Decyzje

1. Jedna baza zapisu i odczytu. Repozytorium agregatu (dodanie, edycja, usunięcie, odczyt po id) oraz osobny interfejs
   odczytu dla listy i szczegółów — bez osobnych tabel read-modelu.
2. Rodzaj połączenia w kolumnie `method` z CHECK; repozytorium mapuje na `GetConnectionDefinition` /
   `PostConnectionDefinition`. Szablon treści dozwolony tylko dla POST (CHECK).
3. Szablon treści w typie `json` (nie `jsonb`), aby zachować dokładny tekst i kolejność kluczy.
4. Każdy typ parametru w osobnej tabeli. PK `(connection_id, position)`; edycja zastępuje wszystkie wiersze parametrów
   (DELETE + INSERT) w jednej transakcji z aktualizacją `connections`.
5. Wartości domyślne: `text` dla ścieżki i query, `json` dla treści.
6. Kolejność listy: `ORDER BY created_at, id`; `created_at` nie zmienia się przy edycji, `updated_at` ustawia
   repozytorium.
7. Unikalność nazw parametrów (ścieżka, treść) oraz zgodność placeholderów z deklaracjami — tylko w aplikacji.
   Parametry treści tylko dla POST — tylko w aplikacji (reguła międzytabelowa).
8. W bazie tylko podstawowe ograniczenia: NOT NULL, CHECK metody i treści, `name varchar(100)`, nazwy parametrów
   ścieżki i treści `varchar(50)`. Format URL i snake_case — w aplikacji.
9. Twarde usuwanie z kaskadą na parametry. Blokadę usunięcia połączenia używanego przez kafelki (TEMP-020) zapewni
   przyszły FK z tabeli pól kafelka (`ON DELETE RESTRICT`).
10. Brak zdarzeń integracyjnych dla połączeń — outbox nie jest potrzebny w tym zakresie.
11. Tabele parametrów mają `created_at` i `updated_at` (uzupełnione po bramce weryfikacyjnej). Wiersze są odtwarzane
    przy edycji, więc znaczniki wskazują ostatni zapis definicji połączenia.
12. Brak tabeli nagłówków — nagłówki są stałe i predefiniowane w kodzie (treść POST zawsze jako
    `Content-Type: application/json`).
