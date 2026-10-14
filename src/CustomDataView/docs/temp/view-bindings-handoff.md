# Przekazanie: wiązania w widoku i w kafelku

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

Ten sam mechanizm działa wewnątrz kafelka, między jego połączeniami: pole kafelka pochodzące z połączenia A zasila
parametr połączenia B tego samego kafelka.

## Wiązanie jako ogólne pojęcie

Wiązanie w widoku i wiązanie w kafelku to to samo pojęcie: wartość pola (wskazanego etykietą) zasila parametr węzła
docelowego. Różnią się tylko węzłami:

| | W widoku | W kafelku |
|---|---|---|
| Węzeł | kafelek | połączenie |
| Źródło | kafelek `from` + etykieta pola | połączenie `from` + etykieta pola tego kafelka |
| Cel | parametr kafelka docelowego | parametr konkretnego połączenia tego kafelka |
| Gdzie zapisane | definicja widoku | definicja kafelka |

- **Wspólne w kodzie:** jeden model wiązania oraz jeden mechanizm grafu zależności używany przez podgląd widoku
  i podgląd kafelka. Mechanizm odpowiada za:
  - wykrywanie cykli (używane też w walidatorach);
  - wykonanie falami: niezależne węzły równolegle, zależne po swoich źródłach;
  - budowanie mapy parametrów dla każdego węzła, z pierwszeństwem wiązania nad ręcznym parametrem;
  - zamianę wartości na tekst dla path i query;
  - propagację błędu `dependency_failed`.
- **Osobne:** zapis (`view_bindings` i `tile_bindings`, bo klucze obce wskazują inne tabele) i walidacja specyficzna
  dla kontekstu.

### Mechanizm dwupoziomowy

Mechanizm działa na abstrakcyjnych węzłach. Każdy poziom (widok i kafelek) dostarcza adapter, który mówi, jak wykonać
węzeł i jak zamienić błąd zależności na odpowiedź. Szkic:

```
Engine(nodes, edges, externalParameters, executeNode)
  edges: (sourceNode, fieldLabel) → (targetNode, parameter)
  1. sprawdź cykle
  2. podziel węzły na fale według zależności
  3. dla każdego węzła w fali (węzły jednej fali równolegle):
     - jeśli któreś pole źródłowe ma błąd → wynik węzła = dependency_failed
     - inaczej: mapa = parametry z zewnątrz + wartości z wiązań (wiązanie wygrywa),
       zamiana na tekst dla path i query
     - executeNode(węzeł, mapa) → pola (etykieta → wartość albo błąd)
```

- **Dwa poziomy działają jednocześnie.** Widok wykonuje graf kafelków, a każdy kafelek przy wykonaniu uruchamia
  własny graf połączeń.
- **Adapter widoku:**
  - węzeł to kafelek;
  - `executeNode` woła podgląd kafelka, czyli mechanizm drugi raz, dla połączeń;
  - parametr celu jest nazwą; kafelek sam rozdziela go na połączenia, w których jest wejściem;
  - `dependency_failed` to błąd całego kafelka (`ViewTileError`).
- **Adapter kafelka:**
  - węzeł to połączenie;
  - `executeNode` waliduje parametry tego połączenia, wywołuje je i wylicza jego pola (JSONPath i transformacje);
  - źródło i cel wskazują konkretne połączenia (`from` i `to`);
  - `dependency_failed` to błąd każdego pola tego połączenia (`TileFieldError`).
- **Sprawdzane jest konkretne pole źródłowe, a nie cały węzeł.** Jeśli kafelek A ma błąd w polu „Temperatura”, ale
  „ID miasta” jest poprawne, wiązanie z „ID miasta” działa. Ta sama reguła obowiązuje na obu poziomach.

### Walidacja parametrów per połączenie (decyzja użytkownika: zmiana obecnego zachowania)

- **Dziś:** parametry wszystkich połączeń kafelka są walidowane razem, przed wywołaniem czegokolwiek
  (`TilePreviewService.ValidateValues`). Brak lub zły parametr któregokolwiek połączenia odrzuca podgląd kafelka
  (błąd walidacji), a w podglądzie widoku daje błąd całego kafelka `invalid_parameters`.
- **Docelowo:** każde połączenie jest walidowane osobno, tuż przed swoim wywołaniem, z mapą zawierającą także wartości
  z wiązań. Brak wymaganego parametru albo zły typ daje błąd pól tego połączenia, np. nowy kod `invalid_parameters`
  w `TileFieldErrorCodes` ze szczegółami (nazwa parametru i komunikat). Pozostałe pola kafelka zwracają dane.
- **Skutki do wprowadzenia:**
  - podgląd kafelka (zapisanego i niezapisanego, GET i POST) z brakującym lub złym parametrem zwraca 200 z błędami
    pól zamiast odrzucenia żądania. Walidacja samego kształtu żądania (np. brak treści) zostaje bez zmian;
  - w podglądzie widoku znika błąd całego kafelka `invalid_parameters` i obsługa `ValidationException`
    w `ViewPreviewService`. Kafelek z brakującym parametrem zwraca pola z błędami. `ViewTileError` zostaje tylko dla
    `dependency_failed`;
  - zmienić istniejące scenariusze, testy, PRD (3.5) i README, które opisują obecne zachowanie (odrzucenie podglądu
    kafelka i błąd kafelka `invalid_parameters` w widoku);
  - niepoprawny typ wartości z wiązania to zwykły błąd `invalid_parameters` pól połączenia docelowego, ze wskazaniem,
    że wartość pochodzi z wiązania.
- **Wiązanie nie jest samodzielnym zasobem.** Nie ma własnego CRUD ani identyfikatora i nie jest współdzielone między
  widokami czy kafelkami. Zawsze jest częścią definicji widoku albo kafelka.

## Ustalenia (zatwierdzone przez użytkownika)

### Niezależność kafelków

- Kafelki nic o sobie nie wiedzą. Definicja kafelka nie zawiera odwołań do innych kafelków.
- Wiązania definiuje wyłącznie widok, w osobnej sekcji `bindings`. Nie są zagnieżdżone we wpisie kafelka w `tiles`.
- Wartości przechodzą między kafelkami tylko przez jawne wiązanie. Nie ma automatycznego dopasowania, np. po nazwie
  pola i parametru. Bez wiązania nic nie jest przekazywane.
- Podgląd pojedynczego kafelka (`/tiles/{id}/preview`, `/tiles/preview`) nie zna wiązań widoku. Wykonuje tylko
  wiązania zdefiniowane w samym kafelku (patrz „Wiązania w kafelku”).

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
- `parameter` to nazwa wejścia kafelka docelowego: parametru zadeklarowanego w którymkolwiek jego połączeniu,
  dowolnego rodzaju (path, query, body), z pominięciem parametrów związanych wewnątrz kafelka (patrz „Wejścia
  kafelka”). Rodzaj nie jest częścią wiązania.
- `bindings` jest opcjonalne; brak oznacza pustą listę.
- Szczegóły widoku (`GET /views/{id}`) zwracają `bindings` w tym samym kształcie.
- PUT widoku zastępuje całość, łącznie z wiązaniami.

### Walidacja zapisu widoku (`ViewDefinitionValidator` / `ViewReferencesValidator`)

- `from` i `to` są kafelkami z `tiles` tego widoku i są różne.
- Para `from` → `to` występuje najwyżej raz.
- `map` ma co najmniej jedno mapowanie.
- `field` istnieje w kafelku `from` (szukane po etykiecie).
- `parameter` jest wejściem kafelka `to` (zadeklarowany w którymś połączeniu i nie wszędzie związany wewnątrz
  kafelka).
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
  - gdy pole źródłowe ma błąd (`invalid_parameters`, `source_unavailable`, `path_not_found`,
    `transformation_failed`, `dependency_failed`), kafelek docelowy dostaje błąd `dependency_failed` (nowa stała
    w `ViewTileErrorCodes`) z przyczyną: kafelek źródłowy, pole i kod błędu. Kafelek docelowy nie jest wtedy
    wywoływany;
  - niepoprawny typ wartości z wiązania daje błąd `invalid_parameters` pól połączeń kafelka docelowego, które
    dostały tę wartość, ze wskazaniem, że wartość pochodzi z wiązania (patrz „Walidacja parametrów per połączenie”);
  - pozostałe kafelki zwracają dane, a status HTTP to zawsze 200 (zasada częściowych wyników).
- Podgląd widoku pomija pola `hidden` w odpowiedzi, ale używa ich jako źródeł wiązań.
- `TilePreviewService` nie musi znać wiązań. `ViewPreviewService` buduje mapę parametrów dla każdego kafelka i woła
  istniejące `Preview(id, parameters, ct)`.

### Wiązania w kafelku: kształt API

Kafelek dostaje opcjonalną sekcję `bindings` w tym samym kształcie co widok (decyzja użytkownika: spójność obu
poziomów). Wpisy są pogrupowane po parze połączeń (`from` → `to`), a każdy ma tablicę mapowań `map` (pole → parametr).

```json
{
  "name": "Pogoda",
  "fields": [
    { "connectionId": "<ID_WYSZUKIWANIE_MIASTA>", "path": "$.cityId", "label": "ID miasta", "hidden": true },
    { "connectionId": "<ID_PROGNOZA>", "path": "$.temperature", "label": "Temperatura" }
  ],
  "bindings": [
    {
      "from": "<ID_WYSZUKIWANIE_MIASTA>",
      "to": "<ID_PROGNOZA>",
      "map": [ { "field": "ID miasta", "parameter": "cityId" } ]
    }
  ]
}
```

- `from` to połączenie źródłowe, używane przez kafelek (przez któreś z jego pól).
- `field` to etykieta pola tego kafelka, które pochodzi z połączenia `from` (tak samo jak w widoku: pole kafelka
  `from`).
- `to` to połączenie docelowe, używane przez kafelek (przez któreś z jego pól).
- `from` jest informacją wynikającą z pola (etykiety są unikalne w kafelku), ale jest podawane jawnie dla spójności
  z widokiem i czytelności zależności. Dlatego walidacja pilnuje zgodności `from` z polem.
- `parameter` to parametr zadeklarowany w połączeniu `to` (path, query albo body).
- Wiązanie dotyczy tylko połączenia `to`. Inne połączenia kafelka deklarujące parametr o tej nazwie dalej dostają
  wartość z zewnątrz.
- `bindings` jest opcjonalne; brak oznacza pustą listę. Szczegóły kafelka (`GET /tiles/{id}`) zwracają `bindings`
  w tym samym kształcie. PUT kafelka zastępuje całość, łącznie z wiązaniami.

### Walidacja zapisu kafelka (`TileDefinitionValidator` / `TileReferencesValidator`)

- `from` i `to` są połączeniami używanymi przez pola kafelka i są różne.
- Para `from` → `to` występuje najwyżej raz.
- `map` ma co najmniej jedno mapowanie.
- `field` istnieje w kafelku (szukane po etykiecie) i pochodzi z połączenia `from`.
- `parameter` jest zadeklarowany w połączeniu `to` i ma w nim najwyżej jedno źródło.
- Wiązania między połączeniami nie tworzą cyklu (A → B → A, także dłuższe).
- Pole z wildcardem jest dozwolone jako źródło, tak jak w widoku.
- Usunięcie z kafelka pola albo połączenia użytego w jego własnym wiązaniu to zwykły błąd walidacji tego kafelka,
  bo PUT zastępuje całość.

### Podgląd kafelka z wiązaniami (`TilePreviewService`)

- Połączenia wykonują się falami według zależności: niezależne równolegle, zależne po swoich źródłach. Reguła „każde
  połączenie użyte w podglądzie kafelka jest wywoływane najwyżej raz” nadal obowiązuje.
- Mapa parametrów połączenia docelowego to wartości z zewnątrz (żądanie albo widok) plus wartości z wiązań kafelka.
  Wiązanie wygrywa z wartością z zewnątrz dla tego połączenia.
- Przekazywana wartość to wartość pola po transformacjach, jak w widoku.
- Błędy dotyczą pól, a nie całego kafelka:
  - gdy pole źródłowe ma błąd albo jego połączenie zawiedzie, pola z połączenia docelowego dostają nowy błąd pola
    `dependency_failed` (nowa stała w `TileFieldErrorCodes`) z przyczyną (pole źródłowe i kod błędu). Połączenie
    docelowe nie jest wtedy wywoływane;
  - pozostałe pola zwracają dane, a status to 200 (zasada częściowych wyników).
- Walidacja parametrów (brak wymaganego, zły typ) odbywa się per połączenie, tuż przed jego wywołaniem, i uwzględnia
  wartości z wiązań. Błąd trafia do pól tego połączenia i nie przerywa podglądu kafelka. Dotyczy to wszystkich
  połączeń, także tych bez zależności (patrz „Walidacja parametrów per połączenie”).
- Ten sam mechanizm działa w podglądzie widoku: widok przekazuje kafelkowi swoją mapę parametrów, a kafelek wykonuje
  wewnątrz własne wiązania.

### Wejścia kafelka

- Wejściem kafelka jest parametr zadeklarowany w którymś z jego połączeń, o ile w tym połączeniu nie jest związany
  wiązaniem kafelka.
- Parametr związany w jednym połączeniu, a niezwiązany w innym, nadal jest wejściem, ale tylko dla tego drugiego
  połączenia.
- Wiązanie w widoku może wskazywać tylko wejście kafelka (walidacja zapisu widoku). Wartość z wiązania widoku trafia
  tylko do połączeń, w których parametr jest wejściem.
- Wartość z zewnątrz dla parametru związanego wewnątrz kafelka jest ignorowana w połączeniu, którego dotyczy to
  wiązanie.

### Usuwanie kafelka

- `DELETE /tiles/{id}` kafelka, który jest źródłem (`from`) wiązania w jakimkolwiek widoku, jest blokowane:
  odpowiedź 409 z kodem (np. `tile_in_use`) i listą widoków (wzorzec `ConnectionInUseException`).
- Kafelek, który jest tylko celem (`to`) albo nie uczestniczy w wiązaniach, usuwa się jak dotąd: kaskadowo znika
  z widoków, a wiązania, w których był `to`, znikają razem z nim.

### Poza zakresem (MVP)

- Różne wartości dla parametrów o tej samej nazwie w różnych połączeniach lub rodzajach jednego kafelka.
- Składanie jednego parametru z kilku pól (szablon, np. `"{lat},{lon}"`).
- Wielokrotne wywołanie kafelka docelowego dla każdego elementu tablicy (fan-out).
- Wiązania widoku w podglądzie pojedynczego kafelka.
- Wiązanie jako samodzielny, współdzielony zasób.

### Otwarte kwestie (zapytaj użytkownika przed implementacją)

- **Edycja połączenia.** Co z edycją połączenia, która usuwa albo zmienia nazwę parametru będącego celem wiązania
  (w kafelku albo w widoku)? Dziś wartości parametrów niezadeklarowanych są po cichu pomijane, więc wiązanie
  przestałoby działać bez żadnego błędu. Propozycja do potwierdzenia: blokada edycji połączenia (409 z listą kafelków
  i widoków), spójnie z pozostałymi blokadami.
- **Wiązanie w kafelku a wiązanie w widoku.** Co, jeśli kafelek doda wewnętrzne wiązanie parametru, który jest już
  celem wiązania w jakimś widoku? Wiązanie widoku wskazywałoby wtedy coś, co nie jest już wejściem kafelka.
  Propozycja do potwierdzenia: blokada edycji kafelka (409 z listą widoków).

## Zmiany w dokumentacji

### PRD modułu

- **3.3 Definicje kafelków:**
  - etykieta pola unikalna w kafelku (bez rozróżniania wielkości liter, po przycięciu);
  - flaga `hidden` (domyślnie `false`); pola ukryte są pomijane w podglądzie widoku, a widoczne w podglądzie kafelka;
  - edycja kafelka, która usuwa lub zmienia etykietę pola użytego w wiązaniu, jest blokowana z listą widoków;
  - usunięcie kafelka będącego źródłem wiązania jest blokowane z listą widoków;
  - wiązania w kafelku (pole → parametr konkretnego połączenia), ich walidacja i pojęcie wejść kafelka.
- **3.4 Definicje widoków:** sekcja wiązań (para kafelków plus mapowania pole → parametr) i reguły walidacji z tego
  dokumentu. Dodać zasadę: „Wartości przechodzą między kafelkami wyłącznie przez jawne wiązanie w widoku; kafelki nie
  znają się nawzajem”.
- **3.5 Test i podgląd:** zmienić zdania „Każdy kafelek wykonuje własny podgląd…” i „trafiają do wszystkich
  kafelków”. Dopisać:
  - kolejność wykonania według wiązań (kafelki w widoku, połączenia w kafelku);
  - mapę parametrów dla każdego węzła i pierwszeństwo wiązania;
  - `dependency_failed` dla kafelka w widoku i dla pola w kafelku;
  - pomijanie pól ukrytych w podglądzie widoku;
  - walidację parametrów per połączenie: brak lub zły parametr daje błąd `invalid_parameters` pól tego połączenia
    i status 200. Zastępuje to obecne odrzucenie podglądu kafelka oraz zdanie „Błąd parametrów (np. brak
    wymaganego) dotyczy tylko kafelka, którego połączenia go wymagają”.
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
- **Nowa tabela `custom_data_view.tile_bindings`:**

  | Column               | Type         | Constraints                                           |
  |----------------------|--------------|-------------------------------------------------------|
  | tile_id              | uuid         | PK, NOT NULL, FK → tiles.id ON DELETE CASCADE         |
  | target_connection_id | uuid         | PK, NOT NULL, FK → connections.id ON DELETE RESTRICT  |
  | source_connection_id | uuid         | NOT NULL, FK → connections.id ON DELETE RESTRICT      |
  | target_parameter     | varchar(50)  | PK, NOT NULL; jak `name` w `connection_*_parameters`  |
  | source_field         | varchar(100) | NOT NULL; etykieta pola tego kafelka                  |

  - PK `(tile_id, target_connection_id, target_parameter)`: jeden parametr połączenia ma jedno źródło w kafelku.
  - CHECK `source_connection_id <> target_connection_id`.
  - Etykiety nie da się objąć kluczem obcym (unikalność jest na wyrażeniu), dlatego istnienie pola i jego zgodność
    z `source_connection_id` sprawdza aplikacja przy zapisie kafelka.
  - Grupowanie `from`/`to`/`map` w API jest odtwarzane przy odczycie; w bazie jest jeden wiersz na mapowanie.
  - Repozytorium kafelków zapisuje `tiles`, pola, transformacje i `tile_bindings` w jednej transakcji (PUT zastępuje
    całość).
- Zaktualizować sekcje Relationships i Indexes oraz opis w Overview.

## Plan realizacji

1. **Dokumentacja.** Zaktualizuj PRD i plan bazy danych, pokaż użytkownikowi diff i zrób commit `docs: ...`.
2. **Etap „Unikalna etykieta i ukryte pole kafelka”:**
   - walidacja unikalności etykiety;
   - `hidden` w zapisie, odczycie i podglądzie kafelka;
   - migracja (`ALTER TABLE tile_fields` plus indeks unikalny);
   - sprawdzenie duplikatów przed migracją.
3. **Etap „Walidacja parametrów per połączenie”** (bez wiązań; upraszcza dalsze etapy):
   - walidacja każdego połączenia osobno, tuż przed wywołaniem;
   - nowy błąd pola `invalid_parameters`;
   - podgląd kafelka z brakującym lub złym parametrem zwraca 200 z błędami pól;
   - usunięcie błędu kafelka `invalid_parameters` z podglądu widoku;
   - aktualizacja istniejących scenariuszy, testów i README.
4. **Etap „Wiązania kafelków w widoku”:**
   - ogólny model wiązania i dwupoziomowy mechanizm grafu zależności (cykle, fale, mapa parametrów dla węzła,
     pierwszeństwo, zamiana na tekst, `dependency_failed`, adaptery), od razu zaprojektowany do użycia także
     w kafelku;
   - zapis, odczyt i edycja wiązań z walidacją;
   - migracja `view_bindings`;
   - podgląd widoku z wiązaniami: kolejność, mapa dla każdego kafelka, pierwszeństwo wiązania, `dependency_failed`,
     pomijanie pól ukrytych, zamiana listy liczb na listę tekstów;
   - blokada usunięcia kafelka będącego źródłem;
   - blokada edycji kafelka, która usuwa albo zmienia etykietę pola będącego źródłem.

   Jeśli etap wyjdzie zbyt duży, zaproponuj użytkownikowi podział, np. zapis wiązań / podgląd z wiązaniami / blokady.
5. **Etap „Wiązania połączeń w kafelku”:**
   - zapis, odczyt i edycja wiązań kafelka z walidacją;
   - migracja `tile_bindings`;
   - podgląd kafelka z wiązaniami na wspólnym mechanizmie z etapu 4 (adapter kafelka, fale połączeń,
     `dependency_failed` na polach);
   - wejścia kafelka i ich uwzględnienie w walidacji oraz podglądzie widoku.

   Przed tym etapem rozstrzygnij z użytkownikiem „Otwarte kwestie”.
6. **README:** na koniec zaktualizuj README o przykłady widoku z wiązaniem, kafelka z wiązaniem i flagi `hidden`.

## Punkty w kodzie

- `ViewPreviewService`: dziś wykonuje wszystkie kafelki równolegle z tą samą mapą parametrów.
- `ViewTileErrorCodes`, `ViewTileError`, `ViewTilePreview`, `TileFieldErrorCodes`, `TileFieldError`.
- `TilePreviewService.Call` i `ValidateValues`: dziś wywołuje wszystkie połączenia kafelka równolegle i waliduje
  parametry wszystkich połączeń razem przed wywołaniem, rzucając `ValidationException`. Do zmiany na walidację per
  połączenie (etap 3) i wykonanie falami (etap 5).
- `ViewPreviewService`: dziś łapie `ValidationException` z podglądu kafelka i zamienia go na błąd kafelka
  `invalid_parameters`; do usunięcia w etapie 3.
- Endpointy podglądu kafelka w `TileEndpoints`: dziś brak parametru odrzuca podgląd; po etapie 3 zwracają 200
  z błędami pól.
- `TileReferencesValidator`: walidacja odwołań kafelka do połączeń (miejsce na walidację parametrów celu wiązań
  kafelka).
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
