# Przekazanie: wiązania kafelków w widoku

Pracujesz w projekcie CustomDataView (moduł CRUD z warstwami `Api` i `Core`, PostgreSQL przez Dapper). Poniżej są
wszystkie ustalenia z sesji analitycznej z użytkownikiem. Twoje zadanie:

1. Zaktualizować PRD modułu i plan bazy danych modułu zgodnie z ustaleniami (sekcja „Zmiany w dokumentacji”).
2. Zaimplementować funkcję etapami z sekcji „Plan realizacji”, metodą TDD: małe kroki Red → Green → Refactor → Commit.

Nie zmieniaj ustaleń bez pytania użytkownika. Jeśli coś jest niejasne albo sprzeczne z kodem, zapytaj.

## Zasady pracy

- Komunikacja i dokumentacja po polsku; kod, identyfikatory i klucze JSON po angielsku.
- Nie czytaj plików konfiguracyjnych aplikacji z ustawieniami środowiska i sekretami.
- Bez synchronizacji z GitHubem: nie twórz issue ani elementów tablicy projektu.
- Commity: tylko tytuł, bez treści, w stylu historii repozytorium (`feat: ...`, `test: ...`, `docs: ...`,
  `refactor: ...`).
- Przed implementacją ustal z użytkownikiem gałąź i poziom testów. Potem pracuj bez pauz po każdym kroku, a na końcu
  zdaj raport.
- Nowe migracje SQL nazywaj `YYYYMMDDHHMMSS_description.sql`, obok istniejących migracji modułu. Tworzyć je można,
  ale uruchamianie migracji i skryptów na bazie wymaga zgody użytkownika. Nie dodawaj pakietów NuGet bez zgody.
- Styl kodu:
  - NRT wyłączone;
  - klamry zawsze;
  - bez sufiksu `Async` w nazwach metod;
  - testy w NUnit, nazwy `Given_When_Then`;
  - bez bibliotek do mocków, z własnymi fake'ami w stylu `TestTileRepository`, `TestViewRepository`,
    `FakeExternalApi`.
- Zanim zaczniesz, przeczytaj PRD modułu, plan bazy danych, README i istniejące scenariusze. Szczególnie ważny jest
  scenariusz podglądu widoku z danymi, bo nowa funkcja go rozszerza.

## Cel funkcji

Po pobraniu danych kafelka A wartość jednego z jego pól ma zasilić parametr połączenia kafelka B, w ramach widoku.

Przykład:
- kafelek A przyjmuje parametr `name`, a w odpowiedzi ma `cityId`;
- kafelek B potrzebuje parametru `cityId`;
- w widoku z A i B użytkownik podaje tylko `name`, a `cityId` dla B pochodzi z A;
- w widoku z samym B użytkownik podaje `cityId` ręcznie, jak każdy inny parametr.

## Ustalenia (zatwierdzone przez użytkownika)

### Niezależność kafelków

- Kafelki nic o sobie nie wiedzą. Definicja kafelka nie zawiera odwołań do innych kafelków.
- Wiązania definiuje wyłącznie widok, w osobnej sekcji `bindings`. Nie są zagnieżdżone we wpisie kafelka w `tiles`.
- Wartości przechodzą między kafelkami tylko przez jawne wiązanie. Nie ma automatycznego dopasowania, np. po nazwie
  pola i parametru. Bez wiązania nic nie jest przekazywane.
- Podgląd pojedynczego kafelka (`/tiles/{id}/preview`, `/tiles/preview`) nie zna wiązań i działa jak dotąd.

### Zmiany w kafelku: unikalna etykieta i flaga `hidden`

- **Unikalna etykieta.** Etykieta pola (`label`) staje się unikalna w obrębie kafelka; porównanie bez rozróżniania
  wielkości liter i po przycięciu białych znaków. Walidacja w `TileDefinitionValidator` (błąd pod kluczem pola,
  komunikat po polsku) oraz ograniczenie w bazie.
- **Flaga `hidden`.** Nowa flaga pola, typ bool, domyślnie `false` (brak w żądaniu oznacza `false`). Dodać do
  `TileFieldRequest`, `TileFieldInput`, `TileField`, `TileFieldDto`, repozytorium Postgres i fake'ów testowych.
- **Zachowanie `hidden`:**
  - podgląd kafelka zwraca pola ukryte z `"hidden": true`, bo ten podgląd służy sprawdzaniu konfiguracji;
  - podgląd widoku pomija pola ukryte w `fields` kafelka;
  - pole ukryte może być źródłem wiązania, tak jak widoczne;
  - pole ukryte nadal przechodzi przez transformacje.
- **Duplikaty w bazie.** Przed dodaniem ograniczenia unikalności sprawdź, czy w bazie nie ma zdublowanych etykiet:
  pokaż zapytanie użytkownikowi i uruchom je za jego zgodą. Jeśli duplikaty są, zapytaj użytkownika, co z nimi
  zrobić.
- **Blokada edycji kafelka.** Edycja kafelka (PUT), która usuwa pole albo zmienia etykietę pola użytego jako źródło
  wiązania w jakimkolwiek widoku, jest blokowana: odpowiedź 409 z kodem (np. `field_in_use`) i listą widoków. Wzorzec
  jak `ConnectionInUseException`, `ConnectionInUseResponse` i `CustomDataViewExceptionHandler`.

### Wiązania w widoku: kształt API

Wiązania są pogrupowane po parze kafelków (`from` → `to`), a każda para ma tablicę mapowań `map` (pole → parametr):

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

- `field` to etykieta pola kafelka źródłowego (unikalna w kafelku).
- `parameter` to nazwa parametru zadeklarowanego w którymkolwiek połączeniu kafelka docelowego, dowolnego rodzaju
  (path, query, body). Rodzaj nie jest częścią wiązania.
- `bindings` jest opcjonalne; brak oznacza pustą listę.
- Szczegóły widoku (`GET /views/{id}`) zwracają `bindings` w tym samym kształcie.
- PUT widoku zastępuje całość, łącznie z wiązaniami.

### Walidacja zapisu widoku (`ViewDefinitionValidator` / `ViewReferencesValidator`)

- `from` i `to` są kafelkami z `tiles` tego widoku i są różne.
- Para `from` → `to` występuje najwyżej raz.
- `map` ma co najmniej jedno mapowanie.
- `field` istnieje w kafelku `from` (szukane po etykiecie).
- `parameter` jest zadeklarowany w którymś połączeniu kafelka `to`.
- Parametr kafelka docelowego ma najwyżej jedno źródło łącznie we wszystkich wpisach. Na przykład `days` nie może
  przyjść jednocześnie z A i z C.
- Wiązania nie tworzą cyklu (A → B → A, także dłuższe).
- Pole z wildcardem (`[*]`, `.*`) jest dozwolone jako źródło; błędny typ wyjdzie dopiero w podglądzie.
- Zdjęcie kafelka z `tiles` przy pozostawionym wiązaniu do niego to zwykły błąd walidacji, bo wiązanie wskazuje
  kafelek spoza widoku.

### Podgląd widoku z wiązaniami (`ViewPreviewService`)

- **Kolejność wykonania.** Według grafu zależności (sortowanie topologiczne albo fale): kafelki bez zależności
  równolegle (`Task.WhenAll`, jak dziś), kafelek zależny dopiero po wszystkich swoich źródłach. Wynik nadal jest
  w kolejności pozycji.
- **Mapa parametrów.** Każdy kafelek dostaje własną mapę: płaska mapa z żądania plus wartości z wiązań, w których
  jest kafelkiem `to`.
- **Pierwszeństwo wiązania.** Wiązanie wygrywa z ręcznie podanym parametrem o tej samej nazwie, ale tylko dla kafelka
  docelowego. Inne kafelki deklarujące ten parametr (bez wiązania) dostają wartość z żądania. Przykład: żądanie
  `{ "name": "Warszawa", "cityId": 5 }`, wiązanie A → B (`ID miasta` → `cityId`), kafelek C też deklaruje `cityId`:
  - A nic nie dostaje, bo nie deklaruje `cityId`;
  - B dostaje wartość z A;
  - C dostaje 5.
- **Przekazywana wartość** to wartość pola po transformacjach (JSON z `TileFieldPreview.Value`). Do
  `TileParameterValues.For` trafia jako `JsonElement`.
- **Typy wartości** sprawdza istniejący `ParameterValuesValidator`:
  - path: niepusty tekst; liczba jest zamieniana na tekst;
  - query: tekst albo niepusta lista tekstów; liczba jest zamieniana na tekst;
  - body: dowolny JSON.
- **Do poprawienia:** `TileParameterValues.AsText` zamienia dziś tylko liczbę na najwyższym poziomie. Lista liczb dla
  path i query ma też być zamieniana na listę tekstów, żeby pole z wildcardem zwracające liczby działało w query.
- **Błędy:**
  - gdy kafelek źródłowy zakończy się błędem (np. `invalid_parameters`) albo pole źródłowe ma błąd
    (`source_unavailable`, `path_not_found`, `transformation_failed`), kafelek docelowy dostaje błąd
    `dependency_failed` (nowa stała w `ViewTileErrorCodes`) z przyczyną: kafelek źródłowy, pole i kod błędu. Kafelek
    docelowy nie jest wtedy wywoływany;
  - niepoprawny typ wartości z wiązania daje dzisiejszy `invalid_parameters` kafelka docelowego, z informacją, że
    wartość pochodzi z wiązania;
  - pozostałe kafelki zwracają dane, a status HTTP to zawsze 200 (zasada częściowych wyników).
- Podgląd widoku pomija pola `hidden` w odpowiedzi, ale używa ich jako źródeł wiązań.
- `TilePreviewService` nie musi znać wiązań. `ViewPreviewService` buduje mapę parametrów dla każdego kafelka i woła
  istniejące `Preview(id, parameters, ct)`.

### Usuwanie kafelka

- `DELETE /tiles/{id}` kafelka, który jest źródłem (`from`) wiązania w jakimkolwiek widoku, jest blokowane:
  odpowiedź 409 z kodem (np. `tile_in_use`) i listą widoków (wzorzec `ConnectionInUseException`).
- Kafelek, który jest tylko celem (`to`) albo nie uczestniczy w wiązaniach, usuwa się jak dotąd: kaskadowo znika
  z widoków, a wiązania, w których był `to`, znikają razem z nim.

### Poza zakresem (MVP)

- Różne wartości dla parametrów o tej samej nazwie w różnych połączeniach lub rodzajach jednego kafelka.
- Składanie jednego parametru z kilku pól (szablon, np. `"{lat},{lon}"`).
- Wielokrotne wywołanie kafelka docelowego dla każdego elementu tablicy (fan-out).
- Wiązania w podglądzie pojedynczego kafelka.

## Zmiany w dokumentacji

### PRD modułu

- **3.3 Definicje kafelków:**
  - etykieta pola unikalna w kafelku (bez rozróżniania wielkości liter, po przycięciu);
  - flaga `hidden` (domyślnie `false`); pola ukryte są pomijane w podglądzie widoku, a widoczne w podglądzie kafelka;
  - edycja kafelka, która usuwa lub zmienia etykietę pola użytego w wiązaniu, jest blokowana z listą widoków;
  - usunięcie kafelka będącego źródłem wiązania jest blokowane z listą widoków.
- **3.4 Definicje widoków:** sekcja wiązań (para kafelków plus mapowania pole → parametr) i reguły walidacji z tego
  dokumentu. Dodać zasadę: „Wartości przechodzą między kafelkami wyłącznie przez jawne wiązanie w widoku; kafelki nie
  znają się nawzajem”.
- **3.5 Test i podgląd:** zmienić zdania „Każdy kafelek wykonuje własny podgląd…” i „trafiają do wszystkich
  kafelków”. Dopisać kolejność wykonania według wiązań, mapę parametrów dla każdego kafelka, pierwszeństwo wiązania,
  `dependency_failed` i pomijanie pól ukrytych.
- **4 Granice produktu:** dopisać elementy z sekcji „Poza zakresem (MVP)”.
- **6 Metryki (opcjonalnie):** 100% prób usunięcia kafelka będącego źródłem wiązania jest blokowanych z listą widoków.
- **Historyjki użytkownika:** dopisać nowe historyjki dla etapów z „Planu realizacji”, z kryteriami akceptacji
  i scenariuszami Gherkin, zgodnie z konwencją istniejących historyjek i scenariuszy.

### Plan bazy danych

- **`tile_fields`:**
  - kolumna `hidden boolean NOT NULL DEFAULT false`;
  - unikalność etykiety w kafelku, bez wielkości liter i po przycięciu, np. unikalny indeks
    `ux_tile_fields_tile_id_label` na `(tile_id, lower(btrim(label)))`;
  - w opisie kolumny `label` zmienić „bez wymogu unikalności” na „unikalna w kafelku”.
- **Nowa tabela `custom_data_view.view_bindings`:**

  | Column           | Type         | Constraints                                           |
  |------------------|--------------|-------------------------------------------------------|
  | view_id          | uuid         | PK, NOT NULL                                          |
  | target_tile_id   | uuid         | PK, NOT NULL                                          |
  | target_parameter | varchar(50)  | PK, NOT NULL; jak `name` w `connection_*_parameters`  |
  | source_tile_id   | uuid         | NOT NULL                                              |
  | source_field     | varchar(100) | NOT NULL; etykieta pola źródłowego                    |

  - PK `(view_id, target_tile_id, target_parameter)`: jeden parametr kafelka docelowego ma jedno źródło.
  - FK `(view_id, target_tile_id)` → `view_tiles (view_id, tile_id)` `ON DELETE CASCADE`.
  - FK `(view_id, source_tile_id)` → `view_tiles (view_id, tile_id)` `ON DELETE RESTRICT`. To blokada usunięcia
    źródła; aplikacja i tak sprawdza to wcześniej i zwraca 409 z listą widoków.
  - CHECK `source_tile_id <> target_tile_id`.
  - Indeks na `source_tile_id` (sprawdzanie blokady usunięcia kafelka i edycji etykiety).
  - Grupowanie `from`/`to`/`map` w API jest odtwarzane przy odczycie; w bazie jest jeden wiersz na mapowanie.
  - Repozytorium widoków zapisuje `views`, `view_tiles` i `view_bindings` w jednej transakcji (PUT zastępuje całość).
- Zaktualizować sekcje Relationships i Indexes oraz opis w Overview.

## Plan realizacji

1. **Dokumentacja.** Zaktualizuj PRD i plan bazy danych, pokaż użytkownikowi diff i zrób commit `docs: ...`.
2. **Etap „Unikalna etykieta i ukryte pole kafelka”:**
   - walidacja unikalności etykiety;
   - `hidden` w zapisie, odczycie i podglądzie kafelka;
   - migracja (`ALTER TABLE tile_fields` plus indeks unikalny);
   - sprawdzenie duplikatów przed migracją.
3. **Etap „Wiązania kafelków w widoku”:**
   - zapis, odczyt i edycja wiązań z walidacją;
   - migracja `view_bindings`;
   - podgląd widoku z wiązaniami: kolejność, mapa dla każdego kafelka, pierwszeństwo wiązania, `dependency_failed`,
     pomijanie pól ukrytych, zamiana listy liczb na listę tekstów;
   - blokada usunięcia kafelka będącego źródłem;
   - blokada edycji kafelka, która usuwa albo zmienia etykietę pola będącego źródłem.

   Jeśli etap wyjdzie zbyt duży, zaproponuj użytkownikowi podział, np. zapis wiązań / podgląd z wiązaniami / blokady.
4. **README:** na koniec zaktualizuj README o przykład widoku z wiązaniem i flagą `hidden`.

## Punkty w kodzie

- `ViewPreviewService`: dziś wykonuje wszystkie kafelki równolegle z tą samą mapą parametrów.
- `ViewTileErrorCodes`, `ViewTileError`, `ViewTilePreview`.
- `TilePreviewService`, `TileParameterValues` (`AsText`), `TileFieldPreview` i `TileFieldPreviewJsonConverter`
  (serializacja `hidden`).
- `ParameterValuesValidator`: reguły typów path, query i body.
- Definicje kafelka (`TileDefinition`, `TileField`, `TileFieldInput`, `TileInput`), `TileDefinitionValidator`,
  `TileService` (`Update`, `Delete`), `PostgresTileRepository`.
- Definicje widoku (`ViewDefinition`, `ViewTile`, `ViewInput`, `ViewTileInput`), `ViewDefinitionValidator`,
  `ViewReferencesValidator`, `ViewService`, DTO widoku, `PostgresViewRepository`.
- `TileRequest`, `ViewRequest`, `TileEndpoints`, `ViewEndpoints`, `CustomDataViewExceptionHandler`,
  `ConnectionInUseResponse` (wzorzec odpowiedzi 409).
- Migracje modułu: ostatnia to `20261014120000_create_views.sql`.
- Testy:
  - testy API z fake'ami repozytoriów i `FakeExternalApi`;
  - testy `Core`;
  - testy persystencji na Postgresie w kontenerze (instrukcja w README).
