# Database Plan — CustomDataView

## Overview

Plan obejmuje na razie tylko definicje połączeń do zewnętrznych REST API (PRD 3.2, TEMP-007 … TEMP-014). Tabela
`connections` przechowuje szablon żądania GET lub POST, a każdy typ parametru — deklaracje parametrów ścieżki, query
i treści — ma własną tabelę podrzędną powiązaną z połączeniem relacją wiele-do-jednego. Nagłówki nie są
przechowywane: są stałe i predefiniowane w kodzie.
Wszystkie tabele leżą w schemacie `custom_data_view`; dostęp przez Dapper, bez Entity Framework. Kafelki i widoki
zostaną dodane w kolejnych wersjach planu.

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

## Relationships

| From table                  | To table    | Type        | Foreign key column                                        |
|-----------------------------|-------------|-------------|-----------------------------------------------------------|
| connection_path_parameters  | connections | many-to-one | connection_path_parameters.connection_id → connections.id  |
| connection_query_parameters | connections | many-to-one | connection_query_parameters.connection_id → connections.id |
| connection_body_parameters  | connections | many-to-one | connection_body_parameters.connection_id → connections.id  |

Wszystkie klucze obce: `ON DELETE CASCADE`.

## Indexes

- `pk_connections` (`connections(id)`) — szczegóły, edycja, usunięcie i test połączenia po identyfikatorze
  (TEMP-010 … TEMP-013).
- `ix_connections_created_at_id` (`connections(created_at, id)`) — lista połączeń w kolejności dodania:
  `ORDER BY created_at, id` (TEMP-010).
- `pk_connection_path_parameters`, `pk_connection_query_parameters`, `pk_connection_body_parameters`
  (`(connection_id, name)`) — wczytanie parametrów połączenia (`WHERE connection_id = @id ORDER BY name`), kaskadowe
  usuwanie i unikalność nazw w obrębie połączenia. Wiodąca kolumna `connection_id` obsługuje FK, więc dodatkowe
  indeksy nie są potrzebne.

## Design decisions

1. **Jedna baza zapisu i odczytu.** Repozytorium agregatu (`IConnectionRepository`: dodanie, edycja, usunięcie,
   odczyt po id) oraz osobny interfejs odczytu dla listy i szczegółów, zgodnie z `AGENTS.md`. Bez osobnych tabel
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
    przez pola kafelków (TEMP-020) zapewni przyszły FK z tabeli pól kafelka z `ON DELETE RESTRICT`.
11. **Brak outboxa dla połączeń.** Operacje na połączeniach nie publikują zdarzeń integracyjnych w MVP.
12. **Brak RLS.** W MVP wszystkie definicje są wspólne i nie ma użytkowników.
13. **Brak `created_at` i `updated_at` w tabelach parametrów** (decyzja dewelopera). Parametry są zapisywane zawsze
    razem z połączeniem (edycja odtwarza wszystkie wiersze — decyzja 5), więc ich znaczniki czasu wynikają
    z `connections.created_at` i `connections.updated_at`.
14. **Brak tabeli nagłówków** (decyzja dewelopera). Wszystkie połączenia wysyłają te same nagłówki, predefiniowane
    w kodzie (treść POST zawsze jako `Content-Type: application/json`), więc definicja połączenia ich nie zawiera.
