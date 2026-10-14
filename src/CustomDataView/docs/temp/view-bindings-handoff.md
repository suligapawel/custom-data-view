# Przekazanie: wiązania kafelków w widoku (prompt dla kolejnego agenta)

Jesteś agentem pracującym w repozytorium CustomDataView (moduł `src/CustomDataView`, prosty moduł CRUD: `Api` + `Core`).
Poniżej są wszystkie ustalenia z sesji analitycznej z użytkownikiem. Twoje zadanie:

1. Zaktualizować `src/CustomDataView/docs/foundation/prd.md` i `src/CustomDataView/docs/foundation/db-plan.md` zgodnie
   z ustaleniami (sekcja „Zmiany w dokumentacji”).
2. Utworzyć scenariusze (skill `scenario-create-single-module`) — dwa osobne, w kolejności z sekcji „Plan realizacji”.
3. Zaimplementować je (skill `scenario-implement-single-module`, TDD, mikrocykle Red → Green → Refactor → Commit).

Nie zmieniaj ustaleń bez pytania użytkownika. Jeśli coś jest niejasne lub sprzeczne z kodem — zapytaj.

## Zasady pracy (z AGENTS.md i pamięci użytkownika)

- Komunikacja i pliki po polsku (`.ai/settings.yaml`: `language.communication` i `language.files` = polish); kod,
  identyfikatory i klucze JSON po angielsku.
- Nie czytaj `appsettings.json` ani `*.settings.json` modułu.
- Bez synchronizacji z GitHubem: nie uruchamiaj skilli `github-*`, nie twórz issue. Historyjki dostają identyfikatory
  `TEMP-xxx` (ostatni użyty: `TEMP-029`, więc kolejne to `TEMP-030`, `TEMP-031`, …).
- Commity: tylko tytuł, bez treści, styl jak w historii, np. `feat: ... (scenario_TEMP-030 #1)`,
  `test: ...`, `docs: ...`.
- Podczas `scenario-implement-single-module`: zadaj pytania wstępne (poziom testów, gałąź, commit dokumentacji), pokaż
  backlog, a potem idź przez wszystkie scenariusze Gherkin bez pauzy po każdym; raport na końcu.
- Nowe migracje SQL (`YYYYMMDDHHMMSS_description.sql` w `CustomDataView.Core/Persistence/Migrations/`) — utworzyć można,
  ale **uruchomienie migracji / skryptów na bazie wymaga zgody użytkownika**. Bez nowych pakietów NuGet bez zgody.
- NRT wyłączone, klamry zawsze, bez sufiksu `Async`, testy NUnit z `[Test(Description = "<folder> - #<krok>")]`
  i nazwami `Given_When_Then`, bez mocków (własne fake'i jak `TestTileRepository`, `TestViewRepository`,
  `FakeExternalApi` w `tests/CustomDataView/CustomDataView.Api.Tests`).
- Zanim zaczniesz: przeczytaj `prd.md`, `db-plan.md`, `README.md` i scenariusze w `src/CustomDataView/docs/scenarios/`
  (szczególnie `scenario_TEMP-029` — podgląd widoku — bo nowa funkcja go rozszerza).

## Cel funkcji

Po pobraniu danych kafelka A wartość jednego z jego pól ma zasilić parametr połączenia kafelka B — **w ramach widoku**.
Przykład: kafelek A przyjmuje parametr `name`, w odpowiedzi ma `cityId`; kafelek B potrzebuje parametru `cityId`.
Na widoku z A i B użytkownik podaje tylko `name`, a `cityId` dla B pochodzi z A. Na widoku z samym B użytkownik podaje
`cityId` ręcznie, jak każdy inny parametr.

## Ustalenia (wszystkie zatwierdzone przez użytkownika)

### Niezależność kafelków

- Kafelki nic o sobie nie wiedzą. Definicja kafelka nie zawiera żadnych odwołań do innych kafelków.
- Wiązania definiuje **wyłącznie widok**, w osobnej sekcji `bindings` (nie zagnieżdżone we wpisie kafelka w `tiles`).
- Wartości przechodzą między kafelkami **wyłącznie przez jawne wiązanie**. Brak automatycznego dopasowania (np. po
  nazwie pola i parametru) — bez wiązania nic nie jest przekazywane.
- Podgląd pojedynczego kafelka (`/tiles/{id}/preview`, `/tiles/preview`) nie zna wiązań i działa jak dotąd.

### Zmiany w kafelku: unikalna etykieta i flaga `hidden`

- Etykieta pola (`label`) staje się **unikalna w obrębie kafelka**; porównanie bez rozróżniania wielkości liter i po
  przycięciu białych znaków. Walidacja w `TileDefinitionValidator` (błąd pod kluczem pola, komunikat po polsku) oraz
  ograniczenie w bazie.
- Nowa flaga pola `hidden` (bool), **domyślnie `false`** (brak w żądaniu = `false`). Dodać do `TileFieldRequest`,
  `TileFieldInput`, `TileField`, DTO (`TileFieldDto`), repozytorium Postgres i fake'ów testowych.
- Zachowanie `hidden`:
  - podgląd kafelka (`/tiles/.../preview`) zwraca pola ukryte z `"hidden": true` (podgląd służy sprawdzaniu konfiguracji);
  - podgląd widoku **pomija** pola ukryte w `fields` kafelka;
  - pole ukryte może być źródłem wiązania (tak jak widoczne);
  - pole ukryte nadal przechodzi przez transformacje.
- Przed dodaniem `UNIQUE (tile_id, label)` sprawdzić, czy w bazie nie ma zdublowanych etykiet (zapytanie pokazać
  użytkownikowi; uruchomienie za jego zgodą). Jeśli są — zapytać użytkownika, co z nimi zrobić. Uwzględnić to w
  scenariuszu.
- **Blokada edycji kafelka:** edycja (PUT), która usuwa pole albo zmienia etykietę pola użytego jako źródło wiązania
  w jakimkolwiek widoku, jest blokowana — 409 z kodem (np. `field_in_use`) i listą widoków (wzorzec jak
  `ConnectionInUseException` / `ConnectionInUseResponse` / `CustomDataViewExceptionHandler`).

### Wiązania w widoku — kształt API

Wiązania są **pogrupowane po parze kafelków** (`from` → `to`) z tablicą mapowań `map` (pole → parametr):

```json
{
  "name": "Pulpit pogodowy",
  "tiles": [
    { "tileId": "<ID_A>", "position": 0 },
    { "tileId": "<ID_B>", "position": 1, "cssClass": "col-6" },
    { "tileId": "<ID_C>", "position": 2 }
  ],
  "bindings": [
    {
      "from": "<ID_A>",
      "to": "<ID_B>",
      "map": [
        { "field": "ID miasta", "parameter": "cityId" },
        { "field": "Kraj",      "parameter": "country" }
      ]
    },
    {
      "from": "<ID_C>",
      "to": "<ID_B>",
      "map": [ { "field": "Dni", "parameter": "days" } ]
    }
  ]
}
```

- `field` = etykieta pola kafelka źródłowego (unikalna w kafelku).
- `parameter` = nazwa parametru zadeklarowanego w którymkolwiek połączeniu kafelka docelowego, dowolnego rodzaju
  (path, query, body) — rodzaj nie jest częścią wiązania.
- `bindings` jest opcjonalne (brak = pusta lista). Szczegóły widoku (`GET /views/{id}`) zwracają `bindings` w tym
  samym kształcie. PUT widoku zastępuje całość, łącznie z wiązaniami.

### Walidacja zapisu widoku (`ViewDefinitionValidator` / `ViewReferencesValidator`)

- `from` i `to` są kafelkami z `tiles` tego widoku i są różne.
- Para `from` → `to` występuje najwyżej raz.
- `map` ma co najmniej jedno mapowanie.
- `field` istnieje w kafelku `from` (po etykiecie).
- `parameter` jest zadeklarowany w którymś połączeniu kafelka `to`.
- Parametr kafelka docelowego ma **najwyżej jedno źródło łącznie we wszystkich wpisach** (np. `days` nie może przyjść
  jednocześnie z A i z C).
- Wiązania nie tworzą cyklu (A → B → A, także dłuższe).
- Pole z wildcardem (`[*]`, `.*`) **jest dozwolone** jako źródło — błędny typ wyjdzie dopiero w podglądzie.
- Zdjęcie kafelka z `tiles` przy pozostawionym wiązaniu do niego = zwykły błąd walidacji (wiązanie wskazuje kafelek
  spoza widoku).

### Podgląd widoku z wiązaniami (`ViewPreviewService`)

- Kolejność wykonania według grafu zależności (sortowanie topologiczne / fale): kafelki bez zależności równolegle
  (`Task.WhenAll`, jak dziś), kafelek zależny dopiero po wszystkich swoich źródłach. Wynik nadal w kolejności pozycji.
- Każdy kafelek dostaje **własną mapę parametrów**: płaska mapa z żądania + wartości z wiązań, w których jest `to`.
- **Wiązanie wygrywa** z ręcznie podanym parametrem o tej samej nazwie — ale tylko dla kafelka docelowego. Inne
  kafelki deklarujące ten parametr (bez wiązania) dostają wartość z żądania. Przykład: żądanie
  `{ "name": "Warszawa", "cityId": 5 }`, wiązanie A → B (`ID miasta` → `cityId`), kafelek C też deklaruje `cityId`:
  A nic nie dostaje (nie deklaruje), B dostaje wartość z A, C dostaje 5.
- Przekazywana wartość to wartość pola **po transformacjach** (JSON z `TileFieldPreview.Value`). Do
  `TileParameterValues.For` trafia jako `JsonElement`.
- Typy wartości sprawdza istniejący `ParameterValuesValidator`: path — niepusty tekst (liczba zamieniana na tekst),
  query — tekst albo niepusta lista tekstów (liczba zamieniana na tekst), body — dowolny JSON. **Do poprawienia:**
  `TileParameterValues.AsText` zamienia dziś tylko liczbę na najwyższym poziomie — lista liczb dla path/query ma też
  być zamieniana na listę tekstów (żeby pole z wildcardem zwracające liczby działało w query).
- Błędy:
  - źródło zakończyło się błędem kafelka (np. `invalid_parameters`) albo pole źródłowe ma błąd (`source_unavailable`,
    `path_not_found`, `transformation_failed`) → kafelek docelowy dostaje błąd `dependency_failed` (nowa stała
    w `ViewTileErrorCodes`) z informacją o przyczynie (kafelek źródłowy, pole, kod błędu); kafelek docelowy nie jest
    wywoływany;
  - niepoprawny typ wartości z wiązania → dzisiejszy `invalid_parameters` kafelka docelowego, z informacją, że wartość
    pochodzi z wiązania;
  - pozostałe kafelki zwracają dane; status HTTP zawsze 200 (zasada częściowych wyników).
- Podgląd widoku pomija pola `hidden` w odpowiedzi (patrz wyżej), ale używa ich jako źródeł wiązań.
- `TilePreviewService` nie musi znać wiązań — `ViewPreviewService` buduje mapę parametrów per kafelek i woła istniejące
  `Preview(id, parameters, ct)`.

### Usuwanie kafelka

- `DELETE /tiles/{id}` kafelka, który jest **źródłem** (`from`) wiązania w jakimkolwiek widoku, jest **blokowane**:
  409 z kodem (np. `tile_in_use`) i listą widoków (wzorzec `ConnectionInUseException`).
- Kafelek, który jest tylko celem (`to`) albo nie uczestniczy w wiązaniach, usuwa się jak dotąd — kaskadowo znika
  z widoków; wiązania, w których był `to`, znikają kaskadowo razem z nim.

### Poza zakresem (MVP)

- Różne wartości dla parametrów o tej samej nazwie w różnych połączeniach/rodzajach jednego kafelka.
- Składanie jednego parametru z kilku pól (szablon, np. `"{lat},{lon}"`).
- Wielokrotne wywołanie kafelka docelowego dla każdego elementu tablicy (fan-out).
- Wiązania w podglądzie pojedynczego kafelka.

## Zmiany w dokumentacji

### `prd.md`

- 3.3 Definicje kafelków: etykieta pola unikalna w kafelku (bez rozróżniania wielkości liter, po przycięciu); flaga
  `hidden` (domyślnie `false`), pola ukryte są pomijane w podglądzie widoku, widoczne w podglądzie kafelka; edycja
  kafelka usuwająca lub zmieniająca etykietę pola użytego w wiązaniu jest blokowana z listą widoków; usunięcie kafelka
  będącego źródłem wiązania jest blokowane z listą widoków.
- 3.4 Definicje widoków: sekcja wiązań (para kafelków + mapowania pole → parametr) i reguły walidacji z tego dokumentu;
  zasada „wartości przechodzą między kafelkami wyłącznie przez jawne wiązanie w widoku; kafelki nie znają się
  nawzajem”.
- 3.5 Test i podgląd: zmienić zdanie „Każdy kafelek wykonuje własny podgląd…” / „trafiają do wszystkich kafelków” —
  dopisać kolejność wykonania według wiązań, mapę parametrów per kafelek, pierwszeństwo wiązania, `dependency_failed`,
  pomijanie pól ukrytych.
- 4 Granice produktu: dopisać elementy z „Poza zakresem (MVP)”.
- 6 Metryki (opcjonalnie): 100% prób usunięcia kafelka będącego źródłem wiązania jest blokowanych z listą widoków.

### `db-plan.md`

- `tile_fields`: kolumna `hidden boolean NOT NULL DEFAULT false`; unikalność etykiety w kafelku z porównaniem bez
  wielkości liter i przycięciem — np. unikalny indeks `ux_tile_fields_tile_id_label` na
  `(tile_id, lower(btrim(label)))`; zmienić opis kolumny `label` („bez wymogu unikalności” → unikalna w kafelku).
- Nowa tabela `custom_data_view.view_bindings`:

  | Column           | Type         | Constraints                                         |
  |------------------|--------------|-----------------------------------------------------|
  | view_id          | uuid         | PK, NOT NULL                                        |
  | target_tile_id   | uuid         | PK, NOT NULL                                        |
  | target_parameter | varchar(50)  | PK, NOT NULL — jak `name` w `connection_*_parameters` |
  | source_tile_id   | uuid         | NOT NULL                                            |
  | source_field     | varchar(100) | NOT NULL — etykieta pola źródłowego                 |

  - PK `(view_id, target_tile_id, target_parameter)` — jeden parametr kafelka docelowego ma jedno źródło.
  - FK `(view_id, target_tile_id)` → `view_tiles (view_id, tile_id)` `ON DELETE CASCADE`.
  - FK `(view_id, source_tile_id)` → `view_tiles (view_id, tile_id)` `ON DELETE RESTRICT` (blokada usunięcia źródła;
    aplikacja i tak sprawdza to wcześniej i zwraca 409 z listą widoków).
  - CHECK `source_tile_id <> target_tile_id`.
  - Indeks na `source_tile_id` (sprawdzanie blokady usunięcia kafelka i edycji etykiety).
  - Grupowanie `from`/`to`/`map` w API odtwarzane przy odczycie; w bazie jeden wiersz na mapowanie. Repozytorium
    widoków zapisuje `views`, `view_tiles` i `view_bindings` w jednej transakcji (PUT zastępuje całość).
- Zaktualizować sekcje Relationships i Indexes oraz opis w Overview.

## Plan realizacji

1. Aktualizacja `prd.md` i `db-plan.md` (commit `docs: ...`). Pokaż użytkownikowi diff przed commitem.
2. Scenariusz `TEMP-030` „Unikalna etykieta i ukryte pole kafelka” (`scenario-create-single-module`): walidacja
   unikalności, `hidden` w zapisie/odczycie/podglądzie kafelka, migracja (`ALTER TABLE tile_fields` + indeks
   unikalny), sprawdzenie duplikatów przed migracją. Implementacja (`scenario-implement-single-module`).
3. Scenariusz `TEMP-031` „Wiązania kafelków w widoku”: zapis/odczyt/edycja wiązań z walidacją, migracja
   `view_bindings`, podgląd widoku z wiązaniami (kolejność, mapa per kafelek, pierwszeństwo wiązania,
   `dependency_failed`, pomijanie pól ukrytych, lista liczb → lista tekstów), blokada usunięcia kafelka-źródła,
   blokada edycji kafelka usuwającej/zmieniającej etykietę pola-źródła. Jeśli scenariusz wyjdzie zbyt duży, zaproponuj
   użytkownikowi podział (np. `TEMP-031` zapis wiązań, `TEMP-032` podgląd z wiązaniami, `TEMP-033` blokady).
4. Na koniec: zaktualizować `README.md` (przykład widoku z wiązaniem i flagą `hidden`) i
   `src/CustomDataView/docs/scenarios/issue_tracker.yaml` zgodnie z konwencją istniejących scenariuszy.

## Punkty w kodzie

- `CustomDataView.Core/Views/Preview/ViewPreviewService.cs` — dziś wszystkie kafelki równolegle z tą samą mapą.
- `CustomDataView.Core/Views/Preview/ViewTileErrorCodes.cs`, `ViewTileError.cs`, `ViewTilePreview.cs`.
- `CustomDataView.Core/Tiles/Preview/TilePreviewService.cs`, `TileParameterValues.cs` (`AsText`),
  `TileFieldPreview.cs` + `TileFieldPreviewJsonConverter.cs` (serializacja `hidden`).
- `CustomDataView.Core/Connections/Validation/ParameterValuesValidator.cs` — reguły typów path/query/body.
- `CustomDataView.Core/Tiles/Definitions/*`, `Tiles/Validation/TileDefinitionValidator.cs`,
  `Tiles/Management/TileService.cs` (`Update`, `Delete`), `Tiles/Persistence/PostgresTileRepository.cs`.
- `CustomDataView.Core/Views/Definitions/*`, `Views/Validation/*`, `Views/Management/*`,
  `Views/Persistence/PostgresViewRepository.cs`.
- `CustomDataView.Api/Requests/TileRequest.cs`, `ViewRequest.cs`, `Endpoints/TileEndpoints.cs`,
  `Endpoints/ViewEndpoints.cs`, `Errors/CustomDataViewExceptionHandler.cs`, `Responses/ConnectionInUseResponse.cs`
  (wzorzec 409).
- `CustomDataView.Core/Persistence/Migrations/` — ostatnia migracja `20261014120000_create_views.sql`.
- Testy: `tests/CustomDataView/CustomDataView.Api.Tests` (fake'i repozytoriów i `FakeExternalApi`),
  `CustomDataView.Core.Tests`, `CustomDataView.Core.Persistence.Tests` (Postgres w kontenerze z README).
