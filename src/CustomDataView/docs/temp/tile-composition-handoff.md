# Przekazanie: kafelki złożone, osadzenia i wiązania

Pracujesz w projekcie CustomDataView (PostgreSQL przez EF Core). Ten dokument zastępuje wcześniejsze przekazanie
„wiązania w widoku i w kafelku”. Zawiera wszystkie ustalenia z sesji analitycznych z użytkownikiem: wcześniejsze,
które nadal obowiązują, oraz nową wizję kafelków złożonych. Twoje zadanie:

1. Zaktualizować PRD modułu i plan bazy danych modułu zgodnie z ustaleniami (sekcja „Zmiany w dokumentacji”).
2. Zaimplementować funkcję etapami z sekcji „Plan realizacji”, metodą TDD: małe kroki Red → Green → Refactor → Commit.

Nie zmieniaj ustaleń bez pytania użytkownika. Jeśli coś jest niejasne albo sprzeczne z kodem, zapytaj. Punkty
z sekcji „Do potwierdzenia” rozstrzygnij z użytkownikiem przed etapem, którego dotyczą.

## Zasady pracy

- Komunikacja i dokumentacja po polsku; kod, identyfikatory i klucze JSON po angielsku.
- Nie czytaj plików konfiguracyjnych aplikacji z ustawieniami środowiska i sekretami.
- Bez synchronizacji z GitHubem: nie twórz issue ani elementów tablicy projektu.
- Commity: tylko tytuł, bez treści, w stylu historii repozytorium (`feat: ...`, `test: ...`, `docs: ...`,
  `refactor: ...`).
- Przed implementacją ustal z użytkownikiem gałąź i poziom testów. Potem pracuj bez pauz po każdym kroku, a na końcu
  zdaj raport.
- Persystencja to EF Core. Zmiany schematu rób migracjami EF Core zgodnie z konwencją projektu (konfiguracja encji,
  nazwy migracji i ich lokalizacja jak w istniejących migracjach). Tworzyć migracje można, ale ich uruchamianie
  (`database update`, skrypty na bazie) wymaga zgody użytkownika. Nie dodawaj pakietów NuGet bez zgody.
- Jeśli kod modułu nadal używa Dappera i migracji SQL, a nie EF Core, zapytaj użytkownika przed pierwszą zmianą
  schematu, czy najpierw przejść na EF Core, czy kontynuować w obecnej technologii.
- Tabele, kolumny, klucze i indeksy opisane w „Planie bazy danych” to model docelowy. Odwzoruj je w konfiguracji
  encji EF Core (nazwy, klucze złożone, `OnDelete`, indeksy unikalne, CHECK przez `HasCheckConstraint`).
- Styl kodu:
  - NRT wyłączone;
  - klamry zawsze;
  - bez sufiksu `Async` w nazwach metod;
  - testy w NUnit, nazwy `Given_When_Then`;
  - bez bibliotek do mocków, z własnymi fake'ami w stylu `TestTileRepository`, `TestViewRepository`,
    `FakeExternalApi`.
- Zanim zaczniesz, przeczytaj PRD modułu, plan bazy danych, README i istniejące scenariusze. Szczególnie ważne są
  scenariusze podglądu kafelka i podglądu widoku z danymi, bo nowa funkcja je rozszerza.

## Cel

Kafelek ma być samodzielnym, reużywalnym komponentem, który buduje się sam na podstawie własnej definicji i wartości
swoich wejść. Kafelek ma typ wyświetlania, dzięki któremu front wie, jak go pokazać. Kafelek może zawierać inne
kafelki. Dane przechodzą między połączeniami, między kafelkami w kafelku i między kafelkami w widoku wyłącznie przez
jawne wiązania kafelek → kafelek, także dynamiczne: źródłem może być element kolekcji wybrany przez użytkownika.
Wartości wpisywane przez użytkownika również pochodzą z kafelka: kafelka-formularza (`display: form`).

Przykład:
- kafelek „Filtry” (`form`) przyjmuje od użytkownika kraj; jego pole „Kraj” zasila parametr `country` kafelka
  „Miasta”;
- kafelek „Miasta” (`table`) pokazuje listę miast z kluczem `ID`;
- kafelek „Pogoda” (`details`) potrzebuje parametru `cityId`;
- w widoku z oboma kafelkami wybór wiersza w „Miastach” zasila `cityId` w „Pogodzie”;
- kafelek „Zamówienia” (`table`) zawiera w każdym wierszu kafelek „Pozycje zamówienia” (`table`), który dostaje
  surowy JSON pozycji od rodzica, oraz kafelek „Dostawa” (`details`), który pobiera dane własnym połączeniem po `ID`
  zamówienia przekazanym od rodzica.

Front (rendering i obsługa wyboru) jest poza zakresem modułu (PRD 4). Moduł dostarcza definicje, walidację, kontrakt
kafelka, wykonanie po stronie serwera i odpowiedź, z której front wie, co wyświetlić i co doładować.

## Pojęcia

| Pojęcie | Co określa | Czego nie wie |
|---|---|---|
| Kafelek | co pokazuje i jak: `display`, pola z rolami, `items` z `key`, źródła danych, osadzone dzieci, wiązania wewnętrzne | gdzie jest osadzony i skąd dostaje wejścia |
| Osadzenie | gdzie i jak stoi kafelek: `key`, `tileId`, `position`, `cssClass`, `scope`, `load` | — |
| Widok | korzeń drzewa osadzeń: osadzenia i wiązania; w przyszłości przypisanie użytkowników | — |
| Wiązanie | źródło (węzeł, pole, zakres) → cel (węzeł, parametr albo wejście danych) | — |
| Kontrakt kafelka | wejścia (parametry i wejścia danych) oraz wyjścia (pola) kafelka, wyliczane z definicji | — |

- Kafelki nic o sobie nie wiedzą. Kafelek odwołuje się do innych kafelków wyłącznie przez osadzenia dzieci.
- Wiązanie nie jest samodzielnym zasobem. Nie ma własnego CRUD ani identyfikatora i nie jest współdzielone. Zawsze
  jest częścią definicji widoku albo kafelka.
- **Wiązania są wyłącznie jawne.** Nie ma automatycznego dopasowania wartości, np. po nazwie pola i parametru, ani
  automatycznego przekazywania wartości w dół drzewa. Bez wiązania nic nie jest przekazywane. Jeśli wnuk potrzebuje
  danych od dziadka, dziadek definiuje wiązanie, którego celem jest wnuk (ścieżka osadzeń, patrz „Wiązania”).
- **Każda wartość ma źródło w kafelku.** Wartość z połączenia pochodzi z pola kafelka, wartość wpisana przez
  użytkownika z pola kafelka `form`, a wybór elementu z kafelka z `items`. Nie ma płaskiej mapy parametrów rozsyłanej
  po nazwie ani pseudo-węzła dla wartości z zewnątrz. Zastępuje to obecne zachowanie podglądu widoku („płaska mapa
  trafia do wszystkich kafelków”).
- Wyjątkiem są wartości testowe w podglądzie pojedynczego kafelka (patrz „Żądanie podglądu”): służą do sprawdzenia
  konfiguracji i do doładowania osadzenia przez front, ale nie są źródłem wiązań.

## Kafelek

### Typ wyświetlania `display`

| `display` | Dane | Jak wyświetla front | Wymaga w definicji |
|---|---|---|---|
| `table` | wiele elementów | wiersze i kolumny (kolumna = pole) | `items` z `key` |
| `list` | wiele elementów | element jako tytuł i opis | `items` z `key`, pole z rolą `title` |
| `details` | jeden rekord | pary etykieta–wartość | bez `items` |
| `summary` | jeden rekord | kilka wyróżnionych wartości | bez `items`, co najmniej jedno pole z rolą `primary` |
| `form` | wartości wpisane przez użytkownika | formularz | pola formularza; bez źródeł, `items`, dzieci i wiązań |

- `display` jest wymagany. Istniejące kafelki dostają w migracji `details`.
- Opcjonalna rola pola (`role`): `title`, `subtitle`, `badge` dla `list`; `primary`, `secondary` dla `summary`.
  Rola niezgodna z `display` to błąd walidacji. `table` i `details` nie przyjmują ról.
- `cssClass` w osadzeniu służy do ułożenia kafelka w rodzicu, a nie do wyboru jego rodzaju.

### Kafelek `form`

Kafelek `form` zbiera wartości od użytkownika i wystawia je jako swoje pola. Nic nie pobiera.

```json
{
  "name": "Filtry",
  "display": "form",
  "fields": [
    { "label": "Kraj",  "type": "text", "required": true },
    { "label": "Od",    "type": "date" },
    { "label": "Limit", "type": "number", "default": 20 }
  ]
}
```

- Pole formularza ma `label` (unikalna w kafelku, jak w innych kafelkach), `type`, `required` (domyślnie `false`)
  i opcjonalne `default`. Nie ma `path`, `source`, `transformations`, `hidden` ani `role`.
- Typy MVP: `text`, `number`, `boolean`, `date` (tekst ISO 8601 `yyyy-MM-dd`). `default` musi pasować do typu.
- Formularz nie ma źródeł, `items`, dzieci ani własnych wiązań. Może być osadzony w widoku i w kafelku złożonym, tylko
  ze `scope: tile`.
- Wartość pola formularza to wartość podana dla tego osadzenia formularza w żądaniu (patrz „Żądanie podglądu”), a bez
  niej `default`.
- Wynik formularza w odpowiedzi to pola z wartościami. Front renderuje je jako kontrolki z bieżącą wartością.
- Pole formularza jest zwykłym źródłem wiązań: `from` to klucz osadzenia formularza, a `field` to etykieta pola.
- Brak wartości:
  - pole wymagane bez wartości i bez `default`: wiązania z tego pola nie mają wartości, a ich cele dostają stan
    `awaiting_input` z `pending` wskazującym pole formularza (tak samo jak przy niewybranym elemencie);
  - pole opcjonalne bez wartości i bez `default`: wartość `null`, czyli brak wartości (wartość domyślna parametru celu
    albo błąd brakującego parametru).
- Wartość niezgodna z typem pola (np. `"abc"` dla `number`) daje błąd pola formularza `invalid_value`, a cele wiązań
  z tego pola dostają `dependency_failed`.

### Źródła danych

Kafelek ma listę źródeł z unikalnym w kafelku kluczem `key`:

- `connection`: kafelek sam pobiera dane wskazanym połączeniem;
- `input`: wejście danych. Kafelek dostaje gotowy JSON (obiekt, tablicę albo wartość prostą). Klucz źródła jest
  nazwą wejścia danych w kontrakcie kafelka.

Każde pole wskazuje swoje źródło (`source`) zamiast połączenia. Kafelek może mieszać oba rodzaje źródeł.

### Elementy `items`

Kafelek `table` i `list` ma sekcję `items`:

- `source`: klucz źródła z tablicą elementów;
- `path`: ścieżka JSONPath do elementów, z wildcardem (np. `$.cities[*]`);
- `key`: etykieta pola, które identyfikuje element.

Ścieżki pól są względne wobec elementu. Wszystkie pola kafelka z `items` pochodzą ze źródła `items.source`.
Pozostałe źródła takiego kafelka służą tylko jako źródła wiązań (np. połączenie zasilające parametr połączenia
z elementami). Pole może mieć wildcard; jego wartość w elemencie jest wtedy tablicą.

### Przykład kafelka

```json
{
  "name": "Zamówienia klienta",
  "display": "table",
  "sources": [
    { "key": "customer", "kind": "connection", "connectionId": "<ID_KLIENT>" },
    { "key": "orders",   "kind": "connection", "connectionId": "<ID_ZAMOWIENIA>" }
  ],
  "items": { "source": "orders", "path": "$.orders[*]", "key": "ID" },
  "fields": [
    { "path": "$.id",    "label": "ID" },
    { "path": "$.total", "label": "Wartość", "transformations": [ { "key": "round", "parameters": { "decimals": 2 } } ] },
    { "path": "$.lines", "label": "Linie", "hidden": true }
  ],
  "children": [
    { "key": "lines",    "tileId": "<ID_POZYCJE>", "scope": "item", "load": "eager", "position": 0, "cssClass": "col-8" },
    { "key": "shipping", "tileId": "<ID_DOSTAWA>", "scope": "item", "load": "lazy",  "position": 1, "cssClass": "col-4" }
  ],
  "bindings": [
    { "from": "customer", "to": "orders",   "map": [ { "field": "ID klienta", "parameter": "customerId" } ] },
    { "from": "parent",   "to": "lines",    "map": [ { "field": "Linie", "input": "rows", "scope": "item" } ] },
    { "from": "parent",   "to": "shipping", "map": [ { "field": "ID", "parameter": "orderId", "scope": "item" } ] }
  ]
}
```

W tym przykładzie pole „ID klienta” musi istnieć w kafelku i pochodzić ze źródła `customer`. W kafelku z `items` takie
pole nie byłoby polem elementu, dlatego reguła „wszystkie pola pochodzą z `items.source`” ma wyjątek: pole źródła
innego niż `items.source` jest dozwolone tylko z `hidden: true` i tylko jako źródło wiązania źródło → źródło.

Kafelek dziecka „Pozycje zamówienia” deklaruje wejście danych i nie wie nic o rodzicu:

```json
{
  "name": "Pozycje zamówienia",
  "display": "table",
  "sources": [ { "key": "rows", "kind": "input" } ],
  "items": { "source": "rows", "path": "$[*]", "key": "SKU" },
  "fields": [
    { "path": "$.sku", "label": "SKU" },
    { "path": "$.qty", "label": "Ilość" }
  ]
}
```

### Unikalna etykieta i flaga `hidden`

- **Unikalna etykieta.** Etykieta pola (`label`) jest unikalna w obrębie kafelka; porównanie bez rozróżniania
  wielkości liter i po przycięciu białych znaków. Walidacja w `TileDefinitionValidator` (błąd pod kluczem pola,
  komunikat po polsku) oraz ograniczenie w bazie.
- **Flaga `hidden`.** Typ bool, domyślnie `false` (brak w żądaniu oznacza `false`). Dodać do żądań, wejść, definicji,
  DTO, konfiguracji encji EF Core i fake'ów testowych.
- **Zachowanie `hidden`:**
  - podgląd kafelka zwraca pola ukryte z `"hidden": true`, bo ten podgląd służy sprawdzaniu konfiguracji;
  - podgląd widoku pomija pola ukryte na każdym poziomie drzewa;
  - pole ukryte może być źródłem wiązania, tak jak widoczne;
  - pole ukryte nadal przechodzi przez transformacje.
- **Duplikaty w bazie.** Przed dodaniem ograniczenia unikalności sprawdź, czy w bazie nie ma zdublowanych etykiet:
  pokaż zapytanie użytkownikowi i uruchom je za jego zgodą. Jeśli duplikaty są, zapytaj użytkownika, co z nimi
  zrobić.

### Kontrakt kafelka

Kontrakt jest wyliczany z definicji i zwracany w szczegółach kafelka (`GET /tiles/{id}`) w sekcji `contract`:

- `parameters`: wejścia-parametry, czyli parametry zadeklarowane w połączeniach źródeł kafelka, o ile w danym
  połączeniu nie są związane wiązaniem wewnętrznym (także z osadzonego formularza). Parametr związany w jednym
  połączeniu, a niezwiązany w innym, nadal jest wejściem, ale tylko dla tego drugiego połączenia;
- `inputs`: wejścia danych (źródła `input`);
- `outputs`: etykiety pól kafelka (także ukrytych i pól formularza) oraz informacja, czy kafelek ma elementy
  (`items`).

Kontrakt nie obejmuje niezwiązanych wejść potomków. Potomek dostaje wartość tylko wtedy, gdy rodzic albo przodek
jawnie ją związał. Niezwiązany wymagany parametr potomka bez wartości domyślnej daje w podglądzie błąd
`invalid_parameters` pól tego potomka.

## Osadzenia

Osadzenie to umieszczenie samodzielnego kafelka w rodzicu (kafelku albo widoku). Ma ten sam kształt w obu miejscach.

| Atrybut | Znaczenie |
|---|---|
| `key` | Nazwa osadzenia, unikalna w rodzicu; węzeł wiązań i identyfikator dla frontu |
| `tileId` | Odwołanie do samodzielnego kafelka |
| `position` | Kolejność wśród rodzeństwa, unikalna w rodzicu |
| `cssClass` | Opcjonalny sposób ułożenia w rodzicu (do 200 znaków) |
| `scope` | Tylko w kafelku: `tile` (jedna instancja) albo `item` (instancja na element rodzica) |
| `load` | Tylko w kafelku: `eager` (z rodzicem) albo `lazy` (na żądanie frontu) |

- Dzieci to wyłącznie odwołania do samodzielnych, reużywalnych kafelków. Nie ma definicji wpisanych w rodzica.
- Pozycja i `cssClass` należą do osadzenia, a nie do kafelka. Ten sam kafelek może stać w różnych rodzicach z różnym
  układem.
- Ten sam kafelek może być osadzony kilka razy w jednym rodzicu, także w widoku. Węzłem wiązania jest `key`
  osadzenia, a nie `tileId`. Znosi to obecny zakaz powtórzenia kafelka w widoku.
- `scope: item` jest dozwolone tylko u rodzica z `items`.
- Pola kafelka wyświetlają się przed dziećmi; dzieci w kolejności `position`. Dzieci `scope: item` trafiają do obszaru
  elementu (np. rozwinięcie wiersza tabeli).
- Brak cykli zawierania (A zawiera B, B zawiera A, także dłuższe) i najwyżej 3 poziomy zagnieżdżenia kafelków
  w MVP (kafelek, dziecko, wnuk). Widok nie jest liczony jako poziom.

## Wiązania

### Wiązanie to strzałka kafelek → kafelek

Wiązanie: wartość pola węzła źródłowego (wskazanego etykietą, w danym zakresie) zasila parametr albo wejście danych
węzła docelowego.

```
[Filtry: form] ── "Kraj" ──▶ "country" ── [Miasta: table] ── "ID" (wybrany) ──▶ "cityId" ── [Pogoda: details]
```

```json
"bindings": [
  { "from": "filters", "to": "cities",  "map": [ { "field": "Kraj", "parameter": "country" } ] },
  { "from": "cities",  "to": "weather", "map": [ { "field": "ID", "parameter": "cityId", "scope": "selected" } ] }
]
```

- `from` i `to` to klucze osadzeń, a nie `tileId`. Każde osadzenie wskazuje jeden konkretny kafelek, ale ten sam
  kafelek może być osadzony kilka razy (np. „Pogoda wylot” i „Pogoda przylot”), więc samo `tileId` nie wystarcza.
- Poziomy różnią się tylko węzłami:

  | Poziom | Węzły | Przykład | Gdzie zapisane |
  |---|---|---|---|
  | Widok | osadzenia widoku, ścieżki do zagnieżdżonych | `cities` → `weather`; `filters` → `orders/shipping` | definicja widoku |
  | Kafelek złożony | `parent`, osadzenia dzieci, ścieżki do potomków, źródła kafelka | `parent` → `lines`; `customer` → `shipping`; `filter` → `orders` | definicja rodzica |
  | Wnętrze kafelka | źródła kafelka | `search` → `forecast` | definicja kafelka |

- W kafelku złożonym węzłami jednego grafu są jego źródła i osadzenia dzieci. `parent` oznacza pola samego kafelka;
  krawędź prowadzi wtedy od źródła, z którego pochodzi wskazane pole.

### Kształt mapowania

```json
{ "field": "ID miasta", "parameter": "cityId" }
{ "field": "Linie", "input": "rows", "scope": "item" }
```

- `field` to etykieta pola węzła źródłowego: pola tego kafelka pochodzącego z danego źródła, pola tego kafelka
  (`parent`), pola osadzonego kafelka albo pola formularza.
- Mapowanie ma dokładnie jedno z `parameter` (parametr połączenia albo wejście-parametr kafelka) i `input` (wejście
  danych).
- `scope` określa wartość ze źródła z elementami:

  | Źródło | `scope` | Wartość |
  |---|---|---|
  | pole kafelka bez `items`, pole źródła, pole formularza | brak | wartość pola |
  | kafelek z `items` | `selected` (domyślny) | pole elementu wybranego przez użytkownika |
  | kafelek z `items` | `all` | lista wartości pola ze wszystkich elementów |
  | `parent` dla dziecka `scope: item` | `item` | pole bieżącego elementu rodzica |

- Wiązania w dzieciach `scope: item` działają w obrębie jednego elementu: `parent` ze `scope: item` oznacza bieżący
  element, a rodzeństwo `scope: item` wiąże się z instancjami tego samego elementu.

### Cel wskazany ścieżką osadzeń

Wiązanie kafelka i widoku może zasilać kafelek osadzony głębiej, np. wnuka:

```json
{ "from": "parent", "to": "shipping/carrier", "map": [ { "field": "Kraj", "parameter": "country", "scope": "item" } ] }
```

- `to` to ścieżka kluczy osadzeń rozdzielonych `/`: `shipping` jest dzieckiem kafelka, a `carrier` dzieckiem
  osadzonym w definicji kafelka `shipping`. Przy limicie 3 poziomów ścieżka w kafelku ma najwyżej 2 segmenty, a w
  widoku najwyżej 3.
- Źródło pozostaje na poziomie definiującego wiązanie: jego źródła, `parent` albo bezpośrednie osadzenia.
- Celem może być tylko wejście kafelka docelowego, którego nie wiąże żaden kafelek pośredni na ścieżce.
- Przy `scope: item` wartość bieżącego elementu trafia do wszystkich instancji celu pod tym elementem.
- Przodek zna klucze osadzeń wewnątrz potomka. To świadome i jawne sprzężenie: definicja pokazuje je wprost.

### Wartości od użytkownika

Wartości wpisywane przez użytkownika pochodzą wyłącznie z kafelków `form` (patrz „Kafelek `form`”). Formularz
osadza się tam, gdzie jego wartości są potrzebne, i wiąże jak każdy inny kafelek:

- w widoku: `filters` → `cities` albo `filters` → `orders/shipping/carrier`;
- w kafelku złożonym: osadzony formularz `filter` → źródło `orders` tego kafelka (dziecko → źródło rodzica). Ten sam
  kafelek złożony działa wtedy identycznie w każdym widoku, bo jego formularz jest częścią jego definicji.

Kafelek, który potrzebuje wartości od użytkownika, a nie ma formularza ani wiązania, dostaje w podglądzie
`invalid_parameters` (albo wartość domyślną parametru). Nic nie trafia do niego „samo”.

### Zapis wiązań

- `bindings` jest opcjonalne; brak oznacza pustą listę. Szczegóły kafelka i widoku zwracają `bindings` w tym samym
  kształcie. PUT kafelka i widoku zastępuje całość, łącznie z osadzeniami i wiązaniami. W bazie jest jeden wiersz na
  mapowanie, a grupowanie jest odtwarzane przy odczycie.

### Reguły wartości

- **Tylko jawne wiązania.** Parametr bez wiązania dostaje wyłącznie swoją wartość domyślną. Wyjątek: wartości testowe
  i doręczone w żądaniu podglądu pojedynczego kafelka (patrz „Żądanie podglądu”).
- **Najbliższe wiązanie wygrywa.** Wiązanie w definicji kafelka wygrywa z wiązaniem przodka albo widoku do tego samego
  celu, a także z wartością testową. Wiązanie przodka „zabrane” po późniejszej edycji potomka przestaje działać bez
  błędu.
- **Przekazywana wartość** to wartość pola po transformacjach. Do surowej wartości służy pole techniczne bez
  transformacji z `hidden: true` (np. `$.lines` przekazywane do wejścia danych).
- **`null` z pola źródłowego oznacza brak wartości.** Działa wtedy wartość domyślna parametru, a dla wymaganego
  parametru bez wartości domyślnej pojawia się zwykły błąd brakującego parametru.
- **Zamiana na tekst dla path i query.** Liczba i wartość logiczna są zamieniane na tekst; lista liczb dla path i query
  jest zamieniana na listę tekstów (do poprawienia w `TileParameterValues.AsText`, która dziś zamienia tylko liczbę na
  najwyższym poziomie). Typy sprawdza istniejący `ParameterValuesValidator`:
  - path: niepusty tekst;
  - query: tekst albo niepusta lista tekstów;
  - body: dowolny JSON.
- **Wejście danych** przyjmuje dowolny JSON. Brak wartości wejścia danych oznacza, że pola z tego źródła dostają błąd
  `invalid_parameters` (brak wejścia), a kafelek z `items` z tego źródła ma błąd elementów.
- **Wyszukiwanie pola po etykiecie** używa tej samej normalizacji co unikalność (bez rozróżniania wielkości liter, po
  przycięciu białych znaków).

### Walidacja zapisu kafelka

- `display` jest wymagany; reguły `items`, ról i pól formularza zależą od `display` (patrz „Typ wyświetlania”
  i „Kafelek `form`”).
- Klucze źródeł są unikalne; `connection` wymaga istniejącego połączenia, `input` go nie ma.
- Pole wskazuje istniejące źródło; w kafelku z `items` reguła źródła pól jak w „Elementy `items`”.
- `items.key` wskazuje istniejące pole kafelka.
- Klucze osadzeń dzieci i pozycje są unikalne; kafelek dziecka istnieje; `scope: item` tylko u rodzica z `items`;
  formularz tylko ze `scope: tile`.
- Klucze źródeł, klucze osadzeń i słowo `parent` tworzą jedną przestrzeń nazw węzłów kafelka; nie mogą się powtarzać.
  Klucze nie zawierają znaku `/`.
- Brak cykli zawierania i najwyżej 3 poziomy. Sprawdzane przy zapisie każdego kafelka, także dziecka, bo dodanie
  dzieci do kafelka osadzonego głęboko może przekroczyć limit u jego przodków.
- Wiązania:
  - `from` jest węzłem tego kafelka (źródło, `parent`, osadzenie dziecka); `to` jest źródłem, osadzeniem dziecka
    albo ścieżką osadzeń do potomka; `from` i `to` są różne;
  - dozwolone pary: źródło → źródło (`connection` → `connection`), dziecko `scope: tile` → źródło, `parent` → dziecko
    lub potomek, dziecko → dziecko lub potomek;
  - każdy segment ścieżki `to` istnieje w definicji odpowiedniego kafelka, a cel nie jest związany przez kafelek
    pośredni na ścieżce;
  - para `from` → `to` występuje najwyżej raz, `map` ma co najmniej jedno mapowanie;
  - `field` istnieje w węźle źródłowym (szukane po etykiecie); dla źródła pole pochodzi z tego źródła;
  - `parameter` jest zadeklarowany w połączeniu docelowym (źródło) albo jest wejściem-parametrem kontraktu kafelka
    docelowego (dziecko, potomek); `input` jest wejściem danych kafelka docelowego;
  - parametr albo wejście węzła docelowego ma najwyżej jedno źródło łącznie we wszystkich wpisach;
  - `scope` zgodny ze źródłem: `selected` i `all` tylko dla węzła z `items`; `item` tylko dla `parent` z `items`
    i docelowego dziecka `scope: item` (albo rodzeństwa `scope: item` w obrębie elementu);
  - wiązania nie tworzą cyklu we wspólnym grafie źródeł i dzieci kafelka (np. źródło → dziecko → to samo źródło);
  - pole z wildcardem jest dozwolone jako źródło; błędny typ wyjdzie dopiero w podglądzie.
- Usunięcie z kafelka pola, źródła albo osadzenia użytego w jego własnym wiązaniu to zwykły błąd walidacji tego
  kafelka, bo PUT zastępuje całość.

### Walidacja zapisu widoku

- Klucze osadzeń i pozycje są unikalne; kafelek istnieje; ten sam kafelek może wystąpić kilka razy; klucze nie
  zawierają `/`.
- `from` jest osadzeniem pierwszego poziomu widoku; `to` jest osadzeniem albo ścieżką osadzeń do kafelka
  zagnieżdżonego; `from` i `to` są różne; para występuje najwyżej raz; `map` ma co najmniej jedno mapowanie.
- `field` istnieje w kafelku `from`; `parameter` jest wejściem-parametrem, a `input` wejściem danych kafelka
  docelowego, niezwiązanym przez kafelek pośredni na ścieżce.
- Parametr albo wejście osadzenia docelowego ma najwyżej jedno źródło łącznie we wszystkich wpisach.
- `scope`: `selected` albo `all` tylko dla kafelka `from` z `items`; `item` niedozwolony.
- Wiązania nie tworzą cyklu.
- Zdjęcie osadzenia z widoku przy pozostawionym wiązaniu do niego to zwykły błąd walidacji.

## Wykonanie

### Gdzie wykonują się wiązania

Zasada: wiązanie zależne od działania użytkownika wykonuje front, a pozostałe wykonuje serwer. Obie strony stosują te
same reguły wartości.

| Wiązanie | Wykonuje |
|---|---|
| źródło → źródło w kafelku | serwer |
| `parent` → dziecko `eager` (zakres brak, `item`, `all`) | serwer, w tej samej odpowiedzi |
| dziecko → dziecko `eager` bez `selected` | serwer |
| formularz → źródło, dziecko albo potomek | serwer, z wartościami formularzy z żądania |
| wiązanie do potomka (ścieżka) bez `selected` | serwer, wartość wyliczona u przodka i przekazana w dół |
| wiązanie widoku bez `selected` | serwer (podgląd widoku) |
| dowolne wiązanie `selected` | front |
| dziecko `lazy` | front, wołając podgląd kafelka dziecka z wejściami wyliczonymi przez serwer |

Po zmianie wartości w formularzu front ponawia podgląd korzenia z nowymi wartościami formularzy albo woła tylko
zależne osadzenia, jeśli ma dla nich komplet wejść.

### Wspólny mechanizm grafu

Jeden mechanizm na abstrakcyjnych węzłach, używany przez podgląd kafelka (źródła, dzieci) i podgląd widoku
(osadzenia). Każdy poziom dostarcza adapter, który mówi, jak wykonać węzeł i jak zamienić błąd zależności na odpowiedź.

```
Engine(nodes, edges, deliveredValues, executeNode)
  edges: (sourceNode, fieldLabel, scope) → (targetNode, parameter | input)
  1. sprawdź cykle
  2. podziel węzły na fale według zależności
  3. dla każdego węzła w fali (węzły jednej fali równolegle):
     - jeśli któreś wiązanie ma scope selected albo wskazuje wymagane pole formularza bez wartości
       → węzeł awaiting_input (z listą brakujących wiązań)
     - jeśli któreś pole źródłowe ma błąd → węzeł dependency_failed
     - jeśli wiązanie wskazuje parametr albo wejście, którego węzeł już nie deklaruje → invalid_binding
     - inaczej: mapa = wartości doręczone + wartości z wiązań (wiązanie wygrywa),
       zamiana na tekst dla path i query
     - executeNode(węzeł, mapa) → pola (etykieta → wartość albo błąd) albo elementy
```

- **Adapter źródeł kafelka:** węzeł to źródło. `executeNode` dla `connection` waliduje parametry tego połączenia,
  wywołuje je i wylicza jego pola; dla `input` bierze wartość wejścia danych. `dependency_failed` i
  `invalid_parameters` to błędy pól tego źródła (`TileFieldError`).
- **Adapter dzieci kafelka:** węzeł to osadzenie dziecka (dla `scope: item` osobno w każdym elemencie). `executeNode`
  woła podgląd kafelka dziecka, czyli mechanizm rekurencyjnie. Błędy to stan osadzenia.
- **Adapter widoku:** węzeł to osadzenie widoku. `executeNode` woła podgląd kafelka. Błędy to stan osadzenia.
- **Formularz jako węzeł:** `executeNode` bierze wartości formularza z żądania (po adresie osadzenia), uzupełnia
  `default`, sprawdza typy i zwraca pola. Nie wywołuje niczego zewnętrznego.
- **Wartości doręczone** to wartości, które węzeł dostał z zewnątrz swojego grafu: od przodka (wiązanie ze ścieżką)
  albo z żądania podglądu pojedynczego kafelka (wartości testowe, wejścia dla doładowania). Nie ma innych wartości
  z zewnątrz.
- **Wiązania do potomków.** W grafie przodka węzłem docelowym jest pierwszy segment ścieżki `to`, więc kolejność fal
  się nie zmienia. Przodek wylicza wartość i przekazuje ją przy wykonaniu dziecka razem z resztą ścieżki. Dziecko
  doręcza ją dalej, aż dotrze do celu. Kafelki pośrednie nie mogą użyć tej wartości jako źródła własnych wiązań.
- **Sprawdzane jest konkretne pole źródłowe, a nie cały węzeł.** Błąd w polu „Temperatura” nie blokuje wiązania
  z poprawnego pola „ID miasta”. Ta sama reguła obowiązuje na wszystkich poziomach.
- **Każde połączenie użyte w podglądzie jednego kafelka jest wywoływane najwyżej raz** (w obrębie instancji
  kafelka).
- **Deduplikacja w obrębie żądania.** Wywołanie tego samego połączenia z tymi samymi wartościami parametrów
  (po zamianie na tekst) jest wykonywane raz na całe żądanie podglądu, a wynik trafia do wszystkich instancji. To nie
  jest cache: nic nie jest przechowywane między żądaniami. Zastępuje to zdanie PRD „połączenie wspólne dla kilku
  kafelków może zostać wywołane kilka razy”.
- **Bez limitu długości łańcucha.** Kolejne fale sumują czas wywołań; ogranicza je tylko limit czasu całego żądania
  (patrz „Limity”).

### Walidacja parametrów per połączenie

- Każde połączenie jest walidowane osobno, tuż przed swoim wywołaniem, z mapą zawierającą także wartości z wiązań.
- Brak wymaganego parametru albo zły typ daje błąd pól tego połączenia: nowy kod `invalid_parameters`
  w `TileFieldErrorCodes` ze szczegółami (nazwa parametru, komunikat, informacja, czy wartość pochodzi z wiązania).
  Pozostałe pola kafelka zwracają dane.
- Podgląd kafelka (zapisanego i niezapisanego, GET i POST) zawsze zwraca 200, także dla brakującego lub złego
  parametru podanego jako wartość testowa. Walidacja samego kształtu żądania (np. brak treści) zostaje bez zmian.
- W podglądzie widoku znika błąd całego kafelka `invalid_parameters` i obsługa `ValidationException`
  w `ViewPreviewService`.

### Limity

Limity chronią korzeń przed wykładniczym wzrostem liczby instancji (np. 50 × 50 × 50 przy 3 poziomach `scope: item`).
Wartości są stałymi w kodzie.

| Limit | Wartość | Po przekroczeniu |
|---|---|---|
| Elementy na osadzenie `eager` + `scope: item` | 50 | instancje ponad limit dostają `awaiting_load` |
| Wywołania zewnętrzne na żądanie (po deduplikacji) | 200 | niewykonane osadzenia dostają `awaiting_load` |
| Czas całego żądania podglądu | 10 s | niewykonane osadzenia dostają `awaiting_load` |
| Równoległe wywołania instancji dzieci | 8 (do potwierdzenia) | kolejne czekają w kolejce |

- Dziecko `scope: item` z własnym połączeniem może być `eager`.
- Budżet i czas dotyczą osadzeń. Źródła samego korzenia wykonują się zawsze; limit czasu pojedynczego wywołania
  działa jak dotąd.
- Błąd jednej instancji nie przerywa pozostałych.
- Dzięki `awaiting_load` z kompletem wejść front doładuje każde niewykonane osadzenie osobno.

### Stany osadzenia w odpowiedzi

| Stan | Znaczenie | Dodatkowe dane |
|---|---|---|
| `data` | kafelek wykonany (pola mogą mieć błędy) | `tile`: zagnieżdżony podgląd |
| `awaiting_input` | czeka na wybór elementu albo na wymagane pole formularza | `inputs` (wyliczone), `pending` (brakujące wiązania) |
| `awaiting_load` | `lazy` albo przekroczony limit | `inputs` (wyliczone) |
| `error` | kafelek nie został wykonany | `error`: `dependency_failed` albo `invalid_binding` z przyczyną |

- Każde osadzenie w odpowiedzi ma `address`: ścieżkę instancji od korzenia, z kluczami elementów dla `scope: item`,
  np. `orders[17]/shipping`. Front wstawia po niej doładowany wynik we właściwe miejsce.
- `inputs` to komplet wartości gotowych do wysłania w podglądzie kafelka osadzonego, w kształcie żądania podglądu
  (`parameters`, `inputs`, `descendants`): wartości z wiązań rodzica i przodków, także te doręczane dalej do jego
  potomków. Front woła podgląd bez znajomości definicji rodzica.
- `pending` opisuje brakujące wiązania (`from` jako `address` węzła źródłowego, `field`, `scope`, `parameter` albo
  `input`), z wyboru elementu albo z wymaganego pola formularza. `address` wskazuje instancję źródła w tym samym elemencie, a nie dowolną instancję o tym `key`. Front po
  wyborze uzupełnia wartości i woła podgląd kafelka osadzonego.
- `dependency_failed` zawiera przyczynę: węzeł źródłowy, pole i kod jego błędu.
- `invalid_binding` zawiera informację „wiązanie wskazuje parametr `<nazwa>`, którego kafelek albo połączenie nie
  deklaruje”.

### Kształt odpowiedzi podglądu kafelka

```json
{
  "id": "<ID>",
  "name": "Zamówienia klienta",
  "display": "table",
  "itemKey": "ID",
  "items": [
    {
      "key": 17,
      "fields": [
        { "label": "ID", "value": 17 },
        { "label": "Wartość", "value": 120.5 },
        { "label": "Linie", "value": [ ], "hidden": true }
      ],
      "children": [
        { "key": "lines", "address": "[17]/lines", "tileId": "<ID_POZYCJE>", "position": 0, "cssClass": "col-8",
          "state": "data", "tile": { "display": "table", "itemKey": "SKU", "items": [ ] } },
        { "key": "shipping", "address": "[17]/shipping", "tileId": "<ID_DOSTAWA>", "position": 1, "cssClass": "col-4",
          "state": "awaiting_load", "inputs": { "orderId": 17 } }
      ]
    }
  ],
  "children": [ ]
}
```

- Kafelek bez `items` zwraca `fields` zamiast `items`.
- Błąd źródła elementów (np. `source_unavailable`, `path_not_found`) zwraca `itemsError` zamiast `items`.
- `children` na poziomie kafelka zawiera osadzenia `scope: tile`; `children` w elemencie zawiera osadzenia
  `scope: item`.
- Adres jest względny wobec kafelka podglądanego (korzenia). W podglądzie widoku zaczyna się od klucza osadzenia
  widoku, np. `orders[17]/shipping`.
- Formularz w odpowiedzi ma `display: form` i `fields` z `label`, `type`, `required`, `default`, bieżącą wartością
  (`value`) i ewentualnym błędem `invalid_value`.

### Żądanie podglądu

```json
{
  "forms": {
    "filters":      { "Kraj": "PL" },
    "orders/filter": { "Status": "new" }
  },
  "parameters":  { "cityId": 5 },
  "inputs":      { "rows": [ { "sku": "A-1", "qty": 2 } ] },
  "descendants": { "shipping/carrier": { "parameters": { "country": "PL" } } }
}
```

| Pole | Znaczenie | Podgląd kafelka | Podgląd widoku |
|---|---|---|---|
| `forms` | wartości formularzy według adresu osadzenia formularza | tak | tak |
| `parameters` | wartości testowe albo doręczone dla wejść-parametrów kontraktu podglądanego kafelka | tak | nie |
| `inputs` | wejścia danych podglądanego kafelka | tak | nie |
| `descendants` | wartości doręczane przez przodków do potomków (ścieżka → `parameters`/`inputs`) | tak | nie |

- `parameters`, `inputs` i `descendants` to dokładnie to, co serwer zwraca w `inputs` stanów `awaiting_load`
  i `awaiting_input`. Służą do testu pojedynczego kafelka i do doładowania osadzenia przez front. Nie są źródłem
  wiązań; wiązanie w definicji kafelka wygrywa z nimi.
- Podgląd widoku przyjmuje tylko `forms`. Zastępuje to dzisiejszą płaską mapę parametrów widoku (POST w treści, GET
  w adresie).
- Wszystkie pola są opcjonalne. Dokładny kształt i sposób przekazania w GET ustal na podstawie istniejących
  `TilePreviewRequest` i `TileDraftPreviewRequest` (patrz „Do potwierdzenia”).

### Podgląd widoku

- Zwraca osadzenia widoku w kolejności pozycji z `key`, `cssClass` i stanem, tak jak dzieci kafelka.
- Wykonuje wiązania widoku bez `selected` według grafu (fale, równolegle niezależne), z wartościami formularzy
  z `forms`. Kafelki docelowe wiązań `selected` i wymaganych pustych pól formularza mają stan `awaiting_input`.
- Każdy kafelek dostaje wyłącznie wartości z jawnych wiązań, w których jest celem. Parametr bez wiązania ma tylko
  swoją wartość domyślną.
- Pomija pola `hidden` na każdym poziomie drzewa, ale używa ich jako źródeł wiązań.
- Status HTTP to zawsze 200 (zasada częściowych wyników).

## Blokady

- **Usunięcie kafelka** jest blokowane (409, kod np. `tile_in_use`, lista widoków i kafelków), gdy kafelek:
  - jest źródłem (`from`) wiązania w jakimkolwiek widoku;
  - jest osadzony jako dziecko w innym kafelku.

  Kafelek, który jest w widoku tylko celem albo nie uczestniczy w wiązaniach, usuwa się jak dotąd: kaskadowo znika
  z widoków, a wiązania, w których był celem, znikają razem z nim.
- **Edycja kafelka (PUT)**, która usuwa pole (także pole formularza) albo zmienia etykietę pola użytego jako źródło
  wiązania w jakimkolwiek widoku albo w jakimkolwiek rodzicu (wiązanie dziecko → dziecko albo dziecko → źródło), jest
  blokowana: 409, kod np. `field_in_use`, lista widoków i kafelków.
- Zmiana pola źródłowego bez zmiany etykiety (ścieżka, źródło, transformacje) nie jest blokowana; wiązanie przekazuje
  nową wartość.
- Edycja połączenia albo kafelka, która usuwa albo zmienia nazwę parametru lub wejścia będącego celem wiązania, nie
  jest blokowana. W podglądzie węzeł docelowy dostaje `invalid_binding` i nie jest wywoływany.
- Wewnętrzne wiązanie kafelka, które „zabiera” parametr wiązaniu widoku albo przodka, nie jest blokowane. Wiązanie
  zewnętrzne przestaje wtedy działać bez błędu (najbliższe wygrywa).
- Edycja kafelka, która usuwa albo zmienia `key` osadzenia wskazanego ścieżką w wiązaniu przodka albo widoku, nie jest
  blokowana. W podglądzie przodka wiązanie daje `invalid_binding` w osadzeniu, do którego ścieżka jeszcze sięga.
- Wzorzec odpowiedzi 409: `ConnectionInUseException`, `ConnectionInUseResponse`, `CustomDataViewExceptionHandler`.

## Poza zakresem (MVP)

- Agregaty, także w `summary`.
- Wielokrotny wybór elementów.
- Wybór zapisany w URL widoku.
- `region`: dziecko wstawione między pola.
- Kafelki prywatne (widoczne tylko w rodzicu).
- Połączenie widoku i kafelka w jedno pojęcie (widok pozostaje osobną encją na wspólnym modelu osadzeń i wiązań).
- Składanie jednego parametru z kilku pól (szablon, np. `"{lat},{lon}"`).
- Różne wartości dla parametrów o tej samej nazwie w różnych połączeniach lub rodzajach jednego kafelka.
- Wiązania widoku w podglądzie pojedynczego kafelka.
- Limit długości łańcucha wiązań.
- Wiązanie jako samodzielny, współdzielony zasób.
- Frontend (renderowanie, obsługa wyboru i formularzy); moduł dostarcza tylko kontrakt i odpowiedzi.
- Formularz: listy wyboru (`options`), wiele wartości w jednym polu, walidacja wzorcem, wartości startowe z wiązań
  i formularz zależny od innego kafelka.
- Wartości przekazywane do kafelków poza wiązaniami (płaska mapa parametrów widoku, przekazywanie po nazwie).

## Zmiany w dokumentacji

### PRD modułu

- **1 Przegląd:** kafelek jako samodzielny, reużywalny komponent z typem wyświetlania; kafelki złożone; wiązania.
- **3.3 Definicje kafelków:**
  - `display` (`table`, `list`, `details`, `summary`, `form`), role pól, `items` z `key`;
  - kafelek `form`: pola formularza (`type`, `required`, `default`), brak wartości i `invalid_value`;
  - źródła danych `connection` i `input`; pole wskazuje źródło;
  - unikalna etykieta pola (bez rozróżniania wielkości liter, po przycięciu) i flaga `hidden`;
  - osadzenia dzieci (`key`, `position`, `cssClass`, `scope`, `load`), limit 3 poziomów, brak cykli;
  - wiązania w kafelku (źródło → źródło, dziecko → źródło, `parent` → dziecko, dziecko → dziecko, cele wskazane
    ścieżką do potomków) z zakresami i walidacją;
  - kontrakt kafelka (wejścia i wyjścia);
  - blokady usunięcia i edycji kafelka;
  - zmienić „Usunięcie kafelka usuwa go ze wszystkich widoków” zgodnie z „Blokady”.
- **3.4 Definicje widoków:**
  - osadzenia z `key`; ten sam kafelek może wystąpić kilka razy (zmiana reguły „dany kafelek występuje w widoku
    najwyżej raz”);
  - wiązania widoku z zakresami `selected` i `all` oraz reguły walidacji;
  - wiązania do kafelków zagnieżdżonych (ścieżka osadzeń);
  - zasada: „Wartości przechodzą między kafelkami wyłącznie przez jawne wiązanie kafelek → kafelek; wartości od
    użytkownika pochodzą z kafelków `form`; kafelki nie znają się nawzajem”.
- **3.5 Test i podgląd:**
  - zmienić zdania „Każdy kafelek wykonuje własny podgląd…” i „Wartości parametrów połączeń są przekazywane
    w żądaniu podglądu jako jedna płaska mapa … i trafiają do wszystkich kafelków”: podgląd widoku przyjmuje wartości
    formularzy (`forms`), a podgląd kafelka także wartości testowe i doręczone (`parameters`, `inputs`,
    `descendants`);
  - kolejność wykonania według wiązań, mapa parametrów dla każdego węzła i pierwszeństwo wiązania;
  - podział wykonania serwer/front i stany osadzeń (`data`, `awaiting_input`, `awaiting_load`, `error`);
  - `dependency_failed` i `invalid_binding`; limity (50 elementów, 200 wywołań, 10 s) i deduplikacja wywołań;
  - pomijanie pól ukrytych w podglądzie widoku;
  - walidacja parametrów per połączenie i status 200 (zastępuje odrzucenie podglądu kafelka oraz zdanie „Błąd
    parametrów (np. brak wymaganego) dotyczy tylko kafelka, którego połączenia go wymagają”);
  - adnotacja, że czas podglądu rośnie z długością łańcucha wiązań i głębokością drzewa.
- **4 Granice produktu:** dopisać elementy z sekcji „Poza zakresem (MVP)”.
- **6 Metryki:** przy metryce „poniżej 2 s” dopisać, że dotyczy kafelków bez łańcuchów wiązań i bez dzieci
  z własnymi połączeniami. Opcjonalnie: 100% prób usunięcia kafelka używanego jako źródło wiązania albo dziecko jest
  blokowanych z listą zależności.
- **Historyjki użytkownika:** dopisać nowe historyjki dla etapów z „Planu realizacji”, z kryteriami akceptacji
  i scenariuszami Gherkin, zgodnie z konwencją istniejących historyjek i scenariuszy.

### Plan bazy danych

- **`tiles`:**
  - `display varchar(10) NOT NULL` z CHECK (`table`, `list`, `details`, `summary`, `form`); istniejące wiersze:
    `details`;
  - `items_source_key varchar(50) NULL`, `items_path text NULL`, `items_key_label varchar(100) NULL`;
  - CHECK: wszystkie trzy kolumny `items_*` są ustawione wtedy i tylko wtedy, gdy `display IN ('table', 'list')`.
- **Nowa tabela `tile_sources`:**

  | Column        | Type        | Constraints                                         |
  |---------------|-------------|-----------------------------------------------------|
  | tile_id       | uuid        | PK, NOT NULL, FK → tiles.id ON DELETE CASCADE       |
  | key           | varchar(50) | PK, NOT NULL                                        |
  | kind          | varchar(10) | NOT NULL, CHECK (kind IN ('connection', 'input'))   |
  | connection_id | uuid        | NULL, FK → connections.id ON DELETE RESTRICT        |

  - CHECK: `connection_id` ustawione wtedy i tylko wtedy, gdy `kind = 'connection'`.
  - Indeks `ix_tile_sources_connection_id` (blokada usunięcia połączenia; przejmuje rolę
    `ix_tile_fields_connection_id`).
- **`tile_fields`:**
  - `connection_id` zastąpione przez `source_key varchar(50) NOT NULL`, FK `(tile_id, source_key)` →
    `tile_sources (tile_id, key)`;
  - `hidden boolean NOT NULL DEFAULT false`;
  - `role varchar(10) NULL` z CHECK dozwolonych wartości;
  - unikalność etykiety: kolumna `normalized_label varchar(100) NOT NULL` (etykieta po przycięciu i zamianie na małe
    litery, ustawiana przez aplikację albo jako kolumna generowana `HasComputedColumnSql(..., stored: true)`)
    z indeksem unikalnym `ux_tile_fields_tile_id_normalized_label` na `(tile_id, normalized_label)`; w opisie `label`
    zmienić „bez wymogu unikalności” na „unikalna w kafelku”;
  - migracja danych: dla każdego kafelka jedno źródło `connection` na każde użyte połączenie, z kluczem
    wygenerowanym w kolejności pierwszego użycia (np. `source_1`, `source_2`).
- **Nowa tabela `tile_form_fields`** (pola kafelka `form`; kafelek `form` nie ma wierszy w `tile_fields`
  i `tile_sources`):

  | Column           | Type         | Constraints                                                  |
  |------------------|--------------|--------------------------------------------------------------|
  | tile_id          | uuid         | PK, NOT NULL, FK → tiles.id ON DELETE CASCADE                |
  | position         | integer      | PK, NOT NULL, CHECK (position >= 0)                          |
  | label            | varchar(100) | NOT NULL; unikalna w kafelku                                 |
  | normalized_label | varchar(100) | NOT NULL; jak w `tile_fields`                                |
  | type             | varchar(10)  | NOT NULL, CHECK (type IN ('text', 'number', 'boolean', 'date')) |
  | required         | boolean      | NOT NULL, DEFAULT false                                      |
  | default_value    | json         | NULL; wartość domyślna zgodna z typem; NULL = brak           |

  - Indeks unikalny `ux_tile_form_fields_tile_id_normalized_label` na `(tile_id, normalized_label)`.
  - Zgodność `display = 'form'` z obecnością wierszy w `tile_form_fields` (i ich brakiem w `tile_fields`) sprawdza
    aplikacja.
- **Nowa tabela `tile_children`:**

  | Column         | Type         | Constraints                                    |
  |----------------|--------------|------------------------------------------------|
  | parent_tile_id | uuid         | PK, NOT NULL, FK → tiles.id ON DELETE CASCADE  |
  | key            | varchar(50)  | PK, NOT NULL                                   |
  | child_tile_id  | uuid         | NOT NULL, FK → tiles.id ON DELETE RESTRICT     |
  | position       | integer      | NOT NULL, CHECK (position >= 0)                |
  | css_class      | varchar(200) | NULL                                           |
  | scope          | varchar(4)   | NOT NULL, CHECK (scope IN ('tile', 'item'))    |
  | load           | varchar(5)   | NOT NULL, CHECK (load IN ('eager', 'lazy'))    |

  - UNIQUE `(parent_tile_id, position)`; CHECK `parent_tile_id <> child_tile_id`.
  - Indeks `ix_tile_children_child_tile_id` (blokada usunięcia, walidacja cykli i głębokości).
  - Cykle dłuższe niż jeden krok i głębokość sprawdza aplikacja.
- **Nowa tabela `tile_bindings`:**

  | Column        | Type         | Constraints                                               |
  |---------------|--------------|-----------------------------------------------------------|
  | tile_id       | uuid         | PK, NOT NULL, FK → tiles.id ON DELETE CASCADE             |
  | target_node   | varchar(110) | PK, NOT NULL; klucz źródła, osadzenia albo ścieżka osadzeń (do 2 segmentów) |
  | target_kind   | varchar(9)   | PK, NOT NULL, CHECK (target_kind IN ('parameter', 'input')) |
  | target_name   | varchar(50)  | PK, NOT NULL; jak `name` w `connection_*_parameters`      |
  | source_node   | varchar(50)  | NOT NULL; klucz źródła, osadzenia albo `parent`           |
  | source_field  | varchar(100) | NOT NULL; etykieta pola                                   |
  | source_scope  | varchar(8)   | NULL, CHECK (source_scope IN ('selected', 'all', 'item')) |

  - PK `(tile_id, target_node, target_kind, target_name)`: jeden cel ma jedno źródło w kafelku.
  - CHECK `source_node <> target_node`.
  - Węzły nie mają kluczy obcych (wskazują źródła albo osadzenia); ich istnienie sprawdza aplikacja.
- **`view_tiles`:**
  - nowa kolumna `key varchar(50) NOT NULL`; PK zmienia się na `(view_id, key)`; ten sam `tile_id` może wystąpić
    kilka razy; migracja danych generuje klucze dla istniejących wierszy (np. `tile_<position>`);
  - indeks `ix_view_tiles_tile_id` zostaje.
- **Nowa tabela `view_bindings`:**

  | Column       | Type         | Constraints                                               |
  |--------------|--------------|-----------------------------------------------------------|
  | view_id      | uuid         | PK, NOT NULL                                              |
  | target_key   | varchar(50)  | PK, NOT NULL; osadzenie pierwszego poziomu                |
  | target_path  | varchar(110) | PK, NOT NULL, DEFAULT ''; dalsza ścieżka osadzeń (pusta = sam kafelek) |
  | target_kind  | varchar(9)   | PK, NOT NULL, CHECK (target_kind IN ('parameter', 'input')) |
  | target_name  | varchar(50)  | PK, NOT NULL                                              |
  | source_key   | varchar(50)  | NOT NULL; osadzenie źródłowe pierwszego poziomu           |
  | source_field | varchar(100) | NOT NULL; etykieta pola                                   |
  | source_scope | varchar(8)   | NULL, CHECK (source_scope IN ('selected', 'all'))         |

  - FK `(view_id, target_key)` → `view_tiles (view_id, key)` `ON DELETE CASCADE`.
  - FK `(view_id, source_key)` → `view_tiles (view_id, key)` `ON DELETE RESTRICT` (aplikacja i tak sprawdza blokadę
    wcześniej i zwraca 409).
  - CHECK `source_key <> target_key OR target_path <> ''` (kafelek może zasilać kafelek zagnieżdżony w sobie samym
    tylko przez ścieżkę; cykle sprawdza aplikacja).
  - Segmenty `target_path` nie mają kluczy obcych (wskazują osadzenia w definicjach kafelków); ich istnienie
    sprawdza aplikacja przy zapisie.
  - Blokada usunięcia kafelka i edycji etykiety szuka wiązań po `view_tiles.tile_id` osadzenia źródłowego.
- Zapis kafelka obejmuje `tiles`, źródła, pola, transformacje, dzieci i wiązania w jednym `SaveChanges`; zapis widoku
  obejmuje `views`, `view_tiles` i `view_bindings` w jednym `SaveChanges` (PUT zastępuje całość).
- Zaktualizować sekcje Overview, Relationships, Indexes i Design decisions (m.in. decyzje 16, 24 i 25).

## Plan realizacji

1. **Dokumentacja.** Zaktualizuj PRD i plan bazy danych, pokaż użytkownikowi diff i zrób commit `docs: ...`.
2. **Unikalna etykieta i ukryte pole:** walidacja unikalności, `hidden` w zapisie, odczycie i podglądzie, migracja
   (`hidden`, `normalized_label`, indeks), sprawdzenie duplikatów przed migracją.
3. **Walidacja parametrów per połączenie** (bez wiązań): walidacja każdego połączenia tuż przed wywołaniem, błąd pola
   `invalid_parameters`, podgląd kafelka zawsze 200, usunięcie błędu kafelka `invalid_parameters` z podglądu widoku,
   aktualizacja scenariuszy, testów i README.
4. **Typ wyświetlania i elementy:** `display`, role pól, `items` z `key`, walidacja zależna od `display`, odpowiedź
   z `items`/`itemKey`/`itemsError`, migracja (`display` = `details` dla istniejących).
5. **Źródła i wiązania źródło → źródło:**
   - `tile_sources`, pole wskazuje źródło, migracja danych z `connection_id`;
   - wspólny mechanizm grafu (cykle, fale, mapa dla węzła, pierwszeństwo, zamiana na tekst, `dependency_failed`,
     `invalid_binding`, `null` jako brak wartości), od razu zaprojektowany dla wszystkich poziomów;
   - `tile_bindings` w zakresie źródło → źródło, walidacja, podgląd falami;
   - kontrakt kafelka w szczegółach.
6. **Źródło `input` i żądanie podglądu:** wejścia danych w definicji, kontrakcie i wykonaniu; żądanie podglądu
   kafelka z `parameters` i `inputs` (wartości testowe zamiast dzisiejszej płaskiej mapy).
7. **Kafelek `form`:** `display: form`, `tile_form_fields`, walidacja pól i wartości domyślnych, `forms` w żądaniu
   podglądu, `invalid_value`, podgląd formularza.
8. **Dzieci `scope: tile`:** `tile_children`, wiązania `parent` → dziecko, dziecko → dziecko i dziecko → źródło
   (formularz filtrujący połączenie rodzica), cele wskazane ścieżką do potomków i `descendants` w żądaniu, walidacja
   cykli i głębokości 3, wykonanie rekurencyjne, stany osadzeń z `address` i kompletem `inputs`, blokady usunięcia
   i edycji wynikające z dzieci.
9. **Dzieci `scope: item` i limity:** zakres `item`, `eager`/`lazy`, adresy z kluczami elementów, limity (50
   elementów, 200 wywołań, 10 s, równoległość), deduplikacja wywołań w obrębie żądania, `awaiting_load`.
10. **Wiązania widoku:** `key` osadzenia i zniesienie zakazu powtórzeń w widoku, `view_bindings` z walidacją (także
   cele wskazane ścieżką), podgląd widoku z grafem, stanami i `forms` zamiast płaskiej mapy (zmiana scenariusza
   podglądu widoku TEMP-029 i jego testów), zakres `all`, blokady usunięcia i edycji wynikające z widoków.
11. **Wybór elementu:** zakres `selected` w widoku i w kafelku, stan `awaiting_input` z `pending`.
12. **README:** przykłady kafelków `table`/`list`/`details`/`summary`/`form`, kafelka złożonego, wiązań na każdym
    poziomie, widoku z formularzem i wyborem oraz flagi `hidden`.

Jeśli któryś etap wyjdzie zbyt duży, zaproponuj użytkownikowi podział.

## Punkty w kodzie

Nazwy klas pochodzą z obecnego układu projektu. Jeśli po zmianach klasa ma inną nazwę albo jej odpowiedzialność jest
rozdzielona, znajdź odpowiednik i umieść zmianę we właściwej warstwie.

- `TileDefinition`, `TileField`, `TileFieldInput`, `TileInput`, `TileDefinitionValidator`,
  `TileReferencesValidator`, `TileService` (`Update`, `Delete`), `ITileRepository` i `PostgresTileRepository`.
- `TilePreviewService` (`Preview`, `Call`, `ValidateValues`, `PreviewField`): dziś wywołuje wszystkie połączenia
  równolegle i waliduje parametry wszystkich połączeń razem, rzucając `ValidationException`.
- `TilePreview`, `TileFieldPreview`, `TileFieldPreviewJsonConverter`, `TileFieldError`, `TileFieldErrorCodes`,
  `TileParameterValues` (`AsText`, `For`).
- `ParameterValuesValidator`: reguły typów path, query i body.
- `ViewTile`, `ViewTileInput`, `ViewDefinitionValidator`, `ViewReferencesValidator`, `ViewService`, DTO widoku,
  `PostgresViewRepository`.
- `ViewPreviewService`: dziś wykonuje wszystkie kafelki równolegle z tą samą mapą i łapie `ValidationException`.
- `ViewTilePreview`, `ViewTileError`, `ViewTileErrorCodes`: do zastąpienia odpowiedzią osadzenia ze stanem.
- `TileRequest`, `TilePreviewRequest`, `TileDraftPreviewRequest`, `ViewRequest`, `TileEndpoints`, `ViewEndpoints`,
  `CustomDataViewExceptionHandler`, `ConnectionInUseResponse` (wzorzec odpowiedzi 409).
- Migracje modułu: nowe dodawaj po ostatniej istniejącej.
- Testy:
  - testy API z fake'ami repozytoriów i `FakeExternalApi`;
  - testy jednostkowe logiki (walidacja, mechanizm grafu, transformacje, podgląd);
  - testy persystencji na Postgresie w kontenerze (instrukcja w README).

## Do potwierdzenia

1. **Symulacja wyboru na serwerze.** Czy podgląd kafelka i widoku ma przyjmować opcjonalne
   `"selection": { "<węzeł>": <klucz elementu> }`, żeby sprawdzić całą konfigurację z wyborem bez frontu? Rozstrzygnij
   przed etapem 11. Byłoby to pole żądania analogiczne do `forms`.
2. **Blokada usunięcia kafelka osadzonego jako dziecko.** Przyjęto blokadę 409, bo usunięcie zepsułoby definicję
   rodzica. Potwierdź przed etapem 8.
3. **Wartość ograniczonej równoległości** wywołań instancji dzieci (propozycja: 8). Potwierdź przed etapem 9.
4. **Pole spoza `items.source` w kafelku z elementami** dozwolone tylko jako ukryte źródło wiązania źródło → źródło.
   Potwierdź przed etapem 5.
5. **GET podglądu.** Dziś podgląd kafelka i widoku ma wariant GET z parametrami w adresie. Czy GET zostaje (wtedy
   ustal zapis `forms`, np. `forms[filters][Kraj]=PL`), czy podgląd z formularzami i wartościami doręczonymi działa
   tylko przez POST? Rozstrzygnij przed etapem 6.
