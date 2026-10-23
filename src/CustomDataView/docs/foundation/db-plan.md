# Database Plan — CustomDataView

## Overview

Plan obejmuje definicje połączeń do zewnętrznych REST API (PRD 3.2, TEMP-007 … TEMP-014), definicje kafelków
(PRD 3.3, TEMP-015 … TEMP-023) oraz kafelki złożone i wiązania (TEMP-030 … TEMP-039), zakładki, tabelę ze stronicowaniem i akcje (TEMP-041 …
TEMP-043). Widoki (dawniej TEMP-024 …
TEMP-029) zostały usunięte; ich rolę pełni kafelek złożony (TEMP-038). Tabela `connections` przechowuje szablon żądania GET lub POST, a każdy typ parametru —
deklaracje parametrów ścieżki, query i treści — ma własną tabelę podrzędną powiązaną z połączeniem relacją
wiele-do-jednego. Nagłówki nie są przechowywane: są stałe i predefiniowane w kodzie.
Kafelek (`tiles`) ma typ wyświetlania i opcjonalne elementy (`items_*`), listę źródeł danych (`tile_sources`:
połączenie albo wejście danych), uporządkowaną listę pól (`tile_fields`) — każde pole wskazuje źródło, ścieżkę JSONPath,
unikalną etykietę, rolę, flagę ukrycia oraz uporządkowany łańcuch transformacji (`tile_field_transformations`,
każda z najwyżej jednym parametrem tekstowym; transformacja wskazuje pole etykietą). Pole złożone zamiast źródła
i ścieżki ma kombinator (`tile_field_combinations`) z listą etykiet pól wejściowych (`tile_field_combination_fields`).
Kafelek `form` ma zamiast nich pola formularza
(`tile_form_fields`). Kafelek może osadzać inne kafelki (`tile_children`) i definiować wiązania wewnętrzne
(`tile_bindings`, jeden wiersz na mapowanie) oraz akcje prowadzące do innych kafelków (`tile_actions`
z mapowaniami w `tile_action_mappings`). Kafelki nie przechowują wartości parametrów połączeń — przychodzą one
z wiązań albo z żądania podglądu.
Wszystkie tabele leżą w schemacie `custom_data_view`; dostęp przez EF Core (provider Npgsql), bez kolumn JSON
w tabelach kafelków poza wartością domyślną pola formularza (szczegóły w sekcji „Mapowanie EF Core”).

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

| Column           | Type         | Constraints             | Description                                                     |
|------------------|--------------|-------------------------|-----------------------------------------------------------------|
| id               | uuid         | PK, NOT NULL            | Identyfikator kafelka nadawany przez aplikację (`Guid.NewGuid()`) |
| name             | varchar(100) | NOT NULL                | Nazwa kafelka; bez wymogu unikalności                           |
| display          | varchar(20)  | NOT NULL, CHECK (display IN ('table', 'list', 'details', 'summary', 'form', 'tabs', 'pagination_table')) | Typ wyświetlania |
| items_source_key | varchar(50)  | NULL                    | Klucz źródła z tablicą elementów (`table`, `list`)              |
| items_path       | text         | NULL                    | Ścieżka JSONPath do elementów                                   |
| items_key_label  | varchar(100) | NULL                    | Etykieta pola identyfikującego element                          |
| pagination_page_number_parameter | varchar(50) | NULL | Parametr połączenia z numerem strony (`pagination_table`)    |
| pagination_page_size_parameter   | varchar(50) | NULL | Parametr połączenia z rozmiarem strony (`pagination_table`)  |
| pagination_page_size             | integer     | NULL | Domyślny rozmiar strony (1–100)                              |
| pagination_total_path            | text        | NULL | JSONPath łącznej liczby elementów; NULL = brak               |
| created_at       | timestamptz  | NOT NULL, DEFAULT now() | Moment dodania; wyznacza kolejność listy; nie zmienia się przy edycji |
| updated_at       | timestamptz  | NOT NULL, DEFAULT now() | Moment ostatniej edycji; ustawiany przez repozytorium           |

CHECK `ck_tiles_items`: wszystkie trzy kolumny `items_*` są ustawione wtedy i tylko wtedy, gdy
`display IN ('table', 'list', 'pagination_table')`. CHECK `ck_tiles_pagination`: trzy kolumny
`pagination_page_number_parameter`, `pagination_page_size_parameter` i `pagination_page_size` są ustawione wtedy i
tylko wtedy, gdy `display = 'pagination_table'`, `pagination_total_path` tylko wtedy, a `pagination_page_size` wynosi
1–100.

### custom_data_view.tile_sources

| Column        | Type        | Constraints                                       | Description                                  |
|---------------|-------------|---------------------------------------------------|----------------------------------------------|
| tile_id       | uuid        | PK, NOT NULL, FK → tiles.id ON DELETE CASCADE     | Kafelek                                      |
| key           | varchar(50) | PK, NOT NULL                                      | Klucz źródła, unikalny w kafelku; węzeł wiązań |
| position      | integer     | NOT NULL, CHECK (position >= 0)                   | Kolejność źródła w definicji (od 0)          |
| kind          | varchar(10) | NOT NULL, CHECK (kind IN ('connection', 'input')) | Rodzaj źródła                                |
| connection_id | uuid        | NULL, FK → connections.id ON DELETE RESTRICT      | Połączenie źródła `connection`               |

PK: `(tile_id, key)`. UNIQUE `uq_tile_sources_tile_id_position` na `(tile_id, position)`. CHECK `ck_tile_sources_connection`: `connection_id` ustawione wtedy i tylko wtedy, gdy
`kind = 'connection'`.

### custom_data_view.tile_fields

| Column           | Type         | Constraints                                      | Description                                  |
|------------------|--------------|--------------------------------------------------|----------------------------------------------|
| tile_id          | uuid         | PK, NOT NULL, FK → tiles.id ON DELETE CASCADE    | Kafelek                                      |
| position         | integer      | PK, NOT NULL, CHECK (position >= 0)              | Pozycja pola na liście (od 0); tożsamość pola |
| source_key       | varchar(50)  | NULL, FK (z tile_id) → tile_sources              | Źródło zasilające pole; NULL = pole złożone  |
| path             | text         | NULL                                             | Ścieżka JSONPath (podzbiór składni z PRD 3.3); NULL = pole złożone |
| label            | varchar(100) | NOT NULL                                         | Etykieta pola; unikalna w kafelku            |
| normalized_label | varchar(100) | NOT NULL, GENERATED ALWAYS AS (lower(btrim(label))) STORED | Etykieta do porównań bez rozróżniania wielkości liter |
| role             | varchar(10)  | NULL, CHECK (role IN ('title', 'subtitle', 'badge', 'primary', 'secondary')) | Rola pola; NULL = brak |
| hidden           | boolean      | NOT NULL, DEFAULT false                          | Pole techniczne; front go nie pokazuje       |

PK: `(tile_id, position)`. FK: `(tile_id, source_key)` → `tile_sources (tile_id, key)` `ON DELETE CASCADE`.
UNIQUE INDEX `ux_tile_fields_tile_id_normalized_label` na `(tile_id, normalized_label)`. UNIQUE
`uq_tile_fields_tile_id_label` na `(tile_id, label)` — cel FK z `tile_field_transformations`
i `tile_field_combinations`. CHECK `ck_tile_fields_source`: `(source_key IS NULL) = (path IS NULL)`. To, że pole ma
albo źródło i ścieżkę, albo wiersz w `tile_field_combinations`, sprawdza aplikacja.

### custom_data_view.tile_field_transformations

| Column      | Type         | Constraints                         | Description                                          |
|-------------|--------------|-------------------------------------|------------------------------------------------------|
| tile_id     | uuid         | PK, NOT NULL, FK (z field_label)    | Kafelek                                              |
| field_label | varchar(100) | PK, NOT NULL, FK (z tile_id)        | Etykieta pola, któremu służy transformacja           |
| position    | integer      | PK, NOT NULL, CHECK (position >= 0) | Kolejność wykonania w łańcuchu pola (od 0)           |
| key         | varchar(50)  | NOT NULL                            | Klucz transformacji z katalogu w kodzie              |
| parameter   | text         | NULL                                | Parametr transformacji jako tekst; NULL = brak       |

PK: `(tile_id, field_label, position)`. FK: `(tile_id, field_label)` → `tile_fields (tile_id, label)`
`ON DELETE CASCADE`.

### custom_data_view.tile_field_combinations

| Column      | Type         | Constraints                      | Description                                     |
|-------------|--------------|----------------------------------|-------------------------------------------------|
| tile_id     | uuid         | PK, NOT NULL, FK (z field_label) | Kafelek                                         |
| field_label | varchar(100) | PK, NOT NULL, FK (z tile_id)     | Etykieta pola złożonego                         |
| key         | varchar(50)  | NOT NULL                         | Klucz kombinatora z katalogu w kodzie           |
| parameter   | text         | NULL                             | Parametr kombinatora jako tekst; NULL = brak    |

PK: `(tile_id, field_label)`. FK `fk_tile_field_combinations_field`: `(tile_id, field_label)` →
`tile_fields (tile_id, label)` `ON DELETE CASCADE`.

### custom_data_view.tile_field_combination_fields

| Column      | Type         | Constraints                         | Description                                  |
|-------------|--------------|-------------------------------------|----------------------------------------------|
| tile_id     | uuid         | PK, NOT NULL, FK (z field_label)    | Kafelek                                      |
| field_label | varchar(100) | PK, NOT NULL, FK (z tile_id)        | Etykieta pola złożonego                      |
| position    | integer      | PK, NOT NULL, CHECK (position >= 0) | Kolejność pola wejściowego (od 0)            |
| input_label | varchar(100) | NOT NULL                            | Etykieta pola wejściowego tego kafelka       |

PK: `(tile_id, field_label, position)`. FK `fk_tile_field_combination_fields_combination`: `(tile_id, field_label)` →
`tile_field_combinations (tile_id, field_label)` `ON DELETE CASCADE`. `input_label` nie ma FK (decyzja 31).

### custom_data_view.tile_form_fields

| Column           | Type         | Constraints                                   | Description                                  |
|------------------|--------------|-----------------------------------------------|----------------------------------------------|
| tile_id          | uuid         | PK, NOT NULL, FK → tiles.id ON DELETE CASCADE | Kafelek `form`                               |
| position         | integer      | PK, NOT NULL, CHECK (position >= 0)           | Pozycja pola formularza (od 0)               |
| label            | varchar(100) | NOT NULL                                      | Etykieta pola; unikalna w kafelku            |
| normalized_label | varchar(100) | NOT NULL, GENERATED ALWAYS AS (lower(btrim(label))) STORED | Jak w `tile_fields`             |
| type             | varchar(10)  | NOT NULL, CHECK (type IN ('text', 'number', 'boolean', 'date')) | Typ wartości         |
| required         | boolean      | NOT NULL, DEFAULT false                       | Czy wartość jest wymagana                    |
| default_value    | json         | NULL                                          | Wartość domyślna zgodna z typem; NULL = brak |

PK: `(tile_id, position)`. UNIQUE INDEX `ux_tile_form_fields_tile_id_normalized_label` na
`(tile_id, normalized_label)`. Zgodność `display = 'form'` z obecnością wierszy w `tile_form_fields` (i ich brakiem
w `tile_fields` i `tile_sources`) sprawdza aplikacja.

### custom_data_view.tile_children

| Column         | Type         | Constraints                                   | Description                                  |
|----------------|--------------|-----------------------------------------------|----------------------------------------------|
| parent_tile_id | uuid         | PK, NOT NULL, FK → tiles.id ON DELETE CASCADE | Kafelek rodzica                              |
| key            | varchar(50)  | PK, NOT NULL                                  | Klucz osadzenia, unikalny w rodzicu          |
| child_tile_id  | uuid         | NOT NULL, FK → tiles.id ON DELETE RESTRICT    | Osadzony kafelek                             |
| position       | integer      | NOT NULL, CHECK (position >= 0)               | Kolejność wśród rodzeństwa                   |
| css_class      | varchar(200) | NULL                                          | Ułożenie w rodzicu; NULL = brak              |
| scope          | varchar(4)   | NOT NULL, CHECK (scope IN ('tile', 'item'))   | Jedna instancja albo instancja na element    |
| load           | varchar(5)   | NOT NULL, CHECK (load IN ('eager', 'lazy'))   | Wykonanie z rodzicem albo na żądanie frontu  |
| label          | varchar(100) | NULL                                          | Tytuł osadzenia (zakładki); wymagany u rodzica `tabs` — pilnuje aplikacja |

PK: `(parent_tile_id, key)`. UNIQUE `uq_tile_children_parent_tile_id_position` na `(parent_tile_id, position)`.
CHECK `ck_tile_children_not_self`: `parent_tile_id <> child_tile_id`. Dłuższe cykle sprawdza aplikacja; głębokość
nie jest ograniczona.

### custom_data_view.tile_bindings

| Column       | Type         | Constraints                                   | Description                                  |
|--------------|--------------|-----------------------------------------------|----------------------------------------------|
| tile_id      | uuid         | PK, NOT NULL, FK → tiles.id ON DELETE CASCADE | Kafelek definiujący wiązanie                 |
| target_node  | text         | PK, NOT NULL                                  | Klucz źródła, osadzenia albo ścieżka osadzeń |
| target_kind  | varchar(9)   | PK, NOT NULL, CHECK (target_kind IN ('parameter', 'input')) | Rodzaj wejścia celu            |
| target_name  | varchar(50)  | PK, NOT NULL                                  | Nazwa parametru albo wejścia                 |
| source_node  | text         | NOT NULL                                      | Klucz źródła, osadzenia, ścieżka osadzeń albo `parent` |
| source_field | varchar(100) | NOT NULL                                      | Etykieta pola węzła źródłowego               |
| source_scope | varchar(8)   | NULL, CHECK (source_scope IN ('selected', 'all', 'item')) | Zakres wartości; NULL = brak     |
| position     | integer      | NOT NULL, CHECK (position >= 0)               | Kolejność mapowania w definicji (od 0)       |

PK: `(tile_id, target_node, target_kind, target_name)` — jeden cel ma jedno źródło w kafelku. CHECK
`ck_tile_bindings_nodes`: `source_node <> target_node`. Węzły nie mają kluczy obcych; ich istnienie sprawdza
aplikacja.

### custom_data_view.tile_actions

| Column         | Type         | Constraints                                   | Description                                  |
|----------------|--------------|-----------------------------------------------|----------------------------------------------|
| tile_id        | uuid         | PK, NOT NULL, FK → tiles.id ON DELETE CASCADE | Kafelek z akcją                              |
| key            | varchar(50)  | PK, NOT NULL                                  | Klucz akcji, unikalny w kafelku              |
| position       | integer      | NOT NULL, CHECK (position >= 0)               | Kolejność akcji w definicji (od 0)           |
| label          | varchar(100) | NOT NULL                                      | Etykieta przycisku albo linku                |
| type           | varchar(6)   | NOT NULL, CHECK (type IN ('button', 'link'))  | Sposób wyświetlenia                          |
| target_tile_id | uuid         | NOT NULL, FK → tiles.id ON DELETE NO ACTION   | Kafelek docelowy (nowy korzeń; także ten sam kafelek) |
| scope          | varchar(4)   | NOT NULL, CHECK (scope IN ('tile', 'item'))   | Raz na kafelek albo przy każdym elemencie    |

PK: `(tile_id, key)`. UNIQUE `uq_tile_actions_tile_id_position` na `(tile_id, position)`.

### custom_data_view.tile_action_mappings

| Column       | Type         | Constraints                                   | Description                                  |
|--------------|--------------|-----------------------------------------------|----------------------------------------------|
| tile_id      | uuid         | PK, NOT NULL, FK (z action_key)               | Kafelek z akcją                              |
| action_key   | varchar(50)  | PK, NOT NULL, FK (z tile_id)                  | Akcja                                        |
| target_kind  | varchar(9)   | PK, NOT NULL, CHECK (target_kind IN ('parameter', 'input')) | Rodzaj wejścia celu            |
| target_name  | varchar(50)  | PK, NOT NULL                                  | Nazwa parametru albo wejścia celu            |
| source_field | varchar(100) | NOT NULL                                      | Etykieta pola kafelka                        |
| position     | integer      | NOT NULL, CHECK (position >= 0)               | Kolejność mapowania w akcji (od 0)           |

PK: `(tile_id, action_key, target_kind, target_name)` — każde wejście celu najwyżej raz w akcji. FK:
`(tile_id, action_key)` → `tile_actions (tile_id, key)` `ON DELETE CASCADE`.

## Relationships

| From table                  | To table    | Type        | Foreign key column                                        |
|-----------------------------|-------------|-------------|-----------------------------------------------------------|
| connection_path_parameters  | connections | many-to-one | connection_path_parameters.connection_id → connections.id  |
| connection_query_parameters | connections | many-to-one | connection_query_parameters.connection_id → connections.id |
| connection_body_parameters  | connections | many-to-one | connection_body_parameters.connection_id → connections.id  |
| tile_sources                | tiles       | many-to-one | tile_sources.tile_id → tiles.id                            |
| tile_sources                | connections | many-to-one | tile_sources.connection_id → connections.id                |
| tile_fields                 | tiles       | many-to-one | tile_fields.tile_id → tiles.id                             |
| tile_fields                 | tile_sources | many-to-one | (tile_id, source_key) → tile_sources (tile_id, key)       |
| tile_field_transformations  | tile_fields | many-to-one | (tile_id, field_label) → tile_fields (tile_id, label)      |
| tile_field_combinations     | tile_fields | one-to-one  | (tile_id, field_label) → tile_fields (tile_id, label)      |
| tile_field_combination_fields | tile_field_combinations | many-to-one | (tile_id, field_label) → tile_field_combinations (tile_id, field_label) |
| tile_form_fields            | tiles       | many-to-one | tile_form_fields.tile_id → tiles.id                        |
| tile_children               | tiles       | many-to-one | tile_children.parent_tile_id → tiles.id                    |
| tile_children               | tiles       | many-to-one | tile_children.child_tile_id → tiles.id                     |
| tile_bindings               | tiles       | many-to-one | tile_bindings.tile_id → tiles.id                           |
| tile_actions                | tiles       | many-to-one | tile_actions.tile_id → tiles.id                            |
| tile_actions                | tiles       | many-to-one | tile_actions.target_tile_id → tiles.id                     |
| tile_action_mappings        | tile_actions | many-to-one | (tile_id, action_key) → tile_actions (tile_id, key)       |

Wszystkie klucze obce: `ON DELETE CASCADE`, z wyjątkiem `tile_sources.connection_id → connections.id` i
`tile_children.child_tile_id → tiles.id` (`ON DELETE RESTRICT`) i `tile_actions.target_tile_id → tiles.id`
(`ON DELETE NO ACTION`).

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
- `pk_tile_sources`, `pk_tile_fields` (`(tile_id, position)`), `pk_tile_field_transformations`
  (`(tile_id, field_label, position)`), `pk_tile_field_combinations`, `pk_tile_field_combination_fields`,
  `pk_tile_form_fields`, `pk_tile_children`, `pk_tile_bindings` —
  wczytanie definicji kafelka (`WHERE tile_id = @id ORDER BY …`) i kaskadowe usuwanie; wiodąca kolumna `tile_id`
  (`parent_tile_id`) obsługuje FK.
- `ix_tile_fields_tile_id_source_key` (`tile_fields(tile_id, source_key)`) — obsługa FK pól do źródeł.
- `ix_tile_sources_connection_id` (`tile_sources(connection_id)`) — wyszukanie kafelków korzystających z połączenia
  przy blokadzie jego usunięcia (TEMP-020) i obsługa FK `RESTRICT`.
- `ux_tile_fields_tile_id_normalized_label`, `ux_tile_form_fields_tile_id_normalized_label` — unikalna etykieta pola
  w kafelku (TEMP-030).
- `uq_tile_fields_tile_id_label` (`tile_fields(tile_id, label)`) — cel FK transformacji wskazujących pole etykietą.
- `ix_tile_children_child_tile_id` (`tile_children(child_tile_id)`) — blokada usunięcia kafelka osadzonego jako
  dziecko, walidacja cykli, blokada edycji pól używanych w wiązaniach rodziców; obsługa FK `RESTRICT`.
- `ix_tile_actions_target_tile_id` (`tile_actions(target_tile_id)`) — blokada usunięcia kafelka wskazanego przez akcję
  (TEMP-043) i obsługa FK.

## Mapowanie EF Core

- `CustomDataViewDbContext` (`CustomDataView.Infrastructure/Persistence/`) ma domyślny schemat `custom_data_view`
  i ładuje konfiguracje z `Persistence/Configurations/` (`IEntityTypeConfiguration<T>`, jedna klasa na tabelę).
- Encje EF to modele wierszy w `Persistence/Rows/` (`ConnectionRow`, `TileFieldRow`, `TileChildRow` itd.), oddzielone
  od modelu domeny. Repozytoria mapują wiersze na `Connection` i `Tile` oraz z powrotem.
- Konfiguracje odwzorowują ten plan 1:1: nazwy tabel i kolumn, typy (`json`, `text`, `varchar(n)`, `timestamptz`),
  klucze złożone, nazwy `pk_*`, `fk_*`, `ix_*`, `ux_*`, `uq_*`, `ck_*`, `OnDelete` (`Cascade`, `Restrict`), CHECK przez `HasCheckConstraint`, kolumny generowane przez `HasComputedColumnSql(..., stored: true)`
  oraz `UNIQUE (…, position)` i `uq_tile_fields_tile_id_label` jako klucze alternatywne.
- Identyfikatory i pozycje nadaje aplikacja (`ValueGeneratedNever`). `created_at` i `updated_at` przy dodaniu
  wypełnia `DEFAULT now()` bazy; edycja ustawia `updated_at` w `ExecuteUpdate` (`DateTime.UtcNow` tłumaczone na
  `now()`).
- Repozytoria są singletonami i tworzą krótkożyjący kontekst z `IDbContextFactory<CustomDataViewDbContext>` na każdą
  operację. Odczyty używają `AsNoTracking`, uporządkowanych `Include` i `AsSplitQuery`. Edycja to `ExecuteUpdate`
  wiersza głównego, `ExecuteDelete` wierszy podrzędnych i `SaveChanges` nowych wierszy w jednej transakcji
  (decyzje 5 i 16). Usunięcie to `ExecuteDelete`, a kaskady i `RESTRICT` wykonuje baza.
- Schemat tworzą migracje EF Core w `Persistence/Migrations/` z tabelą historii
  `custom_data_view.__ef_migrations_history`. Migracja `Initial` tworzy cały schemat od zera, `AddTabsPaginationAndActions` dodaje
  zakładki, stronicowanie i akcje (TEMP-041 … TEMP-043), a `AddFieldCombinations` pola złożone (TEMP-044); schemat z dawnych
  migracji SQL nie jest migrowany (dane trzeba odtworzyć przez API). Uruchamianie migracji na bazie wymaga zgody
  dewelopera; testy repozytoriów stosują je na bazie testowej.

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
    przez źródła kafelków (TEMP-020) sprawdza aplikacja, a FK `tile_sources.connection_id` z `ON DELETE RESTRICT`
    zabezpiecza ją w bazie (decyzja 19).
11. **Brak outboxa dla połączeń.** Operacje na połączeniach nie publikują zdarzeń integracyjnych w MVP.
12. **Brak RLS.** W MVP wszystkie definicje są wspólne i nie ma użytkowników.
13. **Brak `created_at` i `updated_at` w tabelach parametrów** (decyzja dewelopera). Parametry są zapisywane zawsze
    razem z połączeniem (edycja odtwarza wszystkie wiersze — decyzja 5), więc ich znaczniki czasu wynikają
    z `connections.created_at` i `connections.updated_at`.
14. **Brak tabeli nagłówków** (decyzja dewelopera). Wszystkie połączenia wysyłają te same nagłówki, predefiniowane
    w kodzie (treść POST zawsze jako `Content-Type: application/json`), więc definicja połączenia ich nie zawiera.
15. **Transformacje w jednej tabeli, bez kolumn JSON** (decyzja dewelopera, 2026-10-23). Każda transformacja z katalogu
    ma najwyżej jeden parametr, więc parametr jest kolumną `parameter` wiersza transformacji, a nie osobną tabelą;
    klucz transformacji jest zapisany jako tekst (`key`).
16. **Pola identyfikowane pozycją, transformacje etykietą pola i pozycją w łańcuchu** (decyzja dewelopera,
    2026-10-23). PK `tile_fields` z `position` zachowuje kolejność pól. Transformacja wskazuje pole etykietą
    (`field_label`), tak jak wiązania (decyzja 23), a nie pozycją pola, która łatwo się zmienia; `position`
    transformacji wyznacza tylko kolejność wykonania w łańcuchu pola. Edycja zastępuje całą definicję:
    `UPDATE tiles` + `DELETE` źródeł, pól, pól formularza, dzieci i wiązań (transformacje kaskadowo) i ponowny
    `INSERT` w jednej transakcji.
17. **Klucz transformacji bez FK.** Katalog transformacji żyje w kodzie; istnienie klucza sprawdza walidator przy
    zapisie.
18. **Parametr transformacji jako `text`** (decyzja dewelopera, 2026-10-23). Wartość jest zapisywana jako tekst,
    a klasa transformacji sama ją interpretuje (np. `round` i `truncate` parsują liczbę całkowitą). Wartość parametru
    nie jest walidowana przy zapisie — brak, zły format lub zły zakres ujawnia podgląd kafelka
    (`transformation_failed`).
19. **`ON DELETE RESTRICT` ze źródeł do połączeń.** Usunięcie kafelka nie zmienia połączeń; usunięcie połączenia
    używanego przez źródło jest blokowane. Aplikacja zwraca 409 z listą kafelków (`WHERE connection_id = @id`), a FK
    chroni przed wyścigiem.
20. **Kolejność listy kafelków po `created_at, id`** — jak w połączeniach (decyzja 7).
21. **Wartości parametrów połączeń nie są przechowywane.** Przychodzą z frontu przy każdym podglądzie lub pobraniu
    danych, więc jeden kafelek obsługuje wielu klientów. Wyniki podglądu nie są zapisywane.
22. **Widoki usunięte** (decyzja dewelopera, 2026-10-23). Tabel `views` i `view_tiles` nie ma w schemacie;
    rolę widoku pełni kafelek złożony z dziećmi i wiązaniami. Dawne decyzje o siatce widoku
    nie obowiązują.
23. **Unikalna etykieta przez kolumnę generowaną.** `normalized_label` (`lower(btrim(label))`, `STORED`) z indeksem
    unikalnym zabezpiecza w bazie regułę walidatora (bez rozróżniania wielkości liter, po przycięciu). Dane przenoszone ze
    starego schematu trzeba wcześniej sprawdzić zapytaniem z sekcji „Przenoszenie danych ze starego schematu”.
24. **Źródła w osobnej tabeli `tile_sources`.** Pole wskazuje źródło kluczem (FK złożone), a połączenie należy do
    źródła. Przy ręcznym przenoszeniu starych kafelków tworzy się jedno źródło `connection` na każde użyte połączenie,
    z kluczem `source_<n>` w kolejności pierwszego użycia. Kolumna `position` zachowuje kolejność źródeł z definicji (szczegóły
    kafelka zwracają je w tej kolejności).
25. **Wiązania jako jeden wiersz na mapowanie** (`tile_bindings`). Grupowanie `from` → `to`
    odtwarza repozytorium przy odczycie (kolejność z `position`). PK celu gwarantuje, że wejście celu ma jedno źródło
    w definicji. Węzły kafelka (źródła, osadzenia, `parent`, ścieżki) nie mają FK, bo wskazują różne tabele; ich
    istnienie sprawdza walidator.
26. **`ON DELETE RESTRICT` z `tile_children.child_tile_id`.** Usunięcie kafelka osadzonego jako dziecko zepsułoby
    definicję rodzica, dlatego aplikacja zwraca 409 `tile_in_use`, a FK chroni przed wyścigiem.
27. **Typ wyświetlania i elementy w `tiles`.** `display` i trzy kolumny `items_*` z CHECK spójności; kafelki przenoszone ze
    starego schematu dostają `details`. Pola formularza mają własną tabelę z typem i wartością domyślną w `json`.

28. **Stronicowanie w kolumnach `tiles`** (TEMP-042). Stronicowanie ma stały kształt i jest jedno na kafelek, więc
    cztery kolumny `pagination_*` z CHECK spójności z `display` zamiast osobnej tabeli.
29. **Akcje w osobnych tabelach** (TEMP-043), jak wiązania: jeden wiersz na akcję i jeden na mapowanie. FK celu ma
    `ON DELETE NO ACTION`, nie `RESTRICT`: akcja może wskazywać ten sam kafelek, a `NO ACTION` sprawdza referencję
    po kaskadowym usunięciu jego akcji. Usunięcie kafelka wskazanego przez akcję innego kafelka blokuje aplikacja
    (409 `tile_in_use`), a FK chroni przed wyścigiem.
30. **Ścieżki osadzeń bez limitu** (TEMP-041). `source_node` i `target_node` w `tile_bindings` są `text`; ścieżkę
    `from` do potomka rozwiązuje repozytorium przy blokadzie `field_in_use` (wiązania ze znakiem `/` w `source_node`
    i drzewo `tile_children`).
31. **Kombinatory pól jak transformacje** (TEMP-044). Klucz i parametr kombinatora są tekstem bez FK, bo katalog żyje
    w kodzie. Pola wejściowe to osobne wiersze, bez kolumn JSON. Etykiety wejść nie mają FK do `tile_fields`:
    walidator przy zapisie odrzuca nieznaną etykietę, a zmiana etykiety pola wejściowego odrzuca zapis. Pole złożone
    nie ma źródła, więc edycja kafelka usuwa pola jawnie (`ExecuteDelete` na `tile_fields`), a nie kaskadowo przez
    źródła.

## Przenoszenie danych ze starego schematu

- Duplikaty etykiet w starej tabeli `tile_fields` (nowy indeks `ux_tile_fields_tile_id_normalized_label` ich nie
  przyjmie):

  ```sql
  SELECT tile_id, lower(btrim(label)) AS normalized_label, count(*)
  FROM custom_data_view.tile_fields
  GROUP BY tile_id, lower(btrim(label))
  HAVING count(*) > 1;
  ```

- Źródła z dotychczasowych `tile_fields.connection_id`: decyzja 24.
