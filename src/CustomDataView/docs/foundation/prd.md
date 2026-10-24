# Dokument wymagań produktu (PRD) - CustomDataView

## 1. Przegląd produktu

Moduł CustomDataView pozwala zdefiniować, skąd i w jaki sposób mają być pobierane dane z zewnętrznych REST API oraz jak
mają zostać zaprezentowane użytkownikowi. Przechowuje definicje połączeń i kafelków (także złożonych, pełniących rolę
widoku), udostępnia katalogi gotowych transformacji i kombinatorów pól, a także umożliwia sprawdzenie, czy połączenie i definicja
kafelka generują dane zgodne z oczekiwaniami. Kafelek jest samodzielnym, reużywalnym komponentem: buduje się sam na
podstawie własnej definicji i wartości swoich wejść, ma typ wyświetlania (`table`, `list`, `details`, `summary`,
`form`, `pagination_table`, `tabs`), z którego front wie, jak go pokazać, może zawierać inne kafelki (kafelki złożone)
i akcje prowadzące do innych kafelków. Dane przechodzą między
połączeniami i między kafelkami w kafelku złożonym wyłącznie przez jawne wiązania kafelek → kafelek, a wartości
wpisywane przez użytkownika pochodzą z kafelków-formularzy.
Docelowe pobieranie danych i ich renderowanie należą do innych modułów i frontendu, które nie są częścią tego MVP.

## 2. Problem użytkownika

Użytkownik chce zebrać w jednym widoku dane pochodzące z wielu zewnętrznych API, ale dziś każda zmiana źródła danych,
wybranego pola czy sposobu prezentacji wymaga zmian w kodzie. Brakuje miejsca, w którym można zapisać konfigurację
połączeń, wskazać potrzebne wartości w odpowiedziach, nadać im czytelną postać, zgrupować je w kafelki i ułożyć kafelki
w widok. Brakuje też sposobu, aby przed użyciem definicji sprawdzić, czy połączenie działa i czy kafelek zwraca
poprawne dane. Problem dotyczy osób konfigurujących widoki danych.

## 3. Wymagania funkcjonalne

Rola: użytkownik konfigurujący widoki danych (bez uwierzytelniania; w MVP wszystkie definicje są wspólne).

3.1. Katalog transformacji pól

- Transformacje pól są predefiniowane w kodzie aplikacji; użytkownik nie może ich tworzyć, edytować ani usuwać.
- Każda transformacja implementuje wspólny interfejs i może dekorować inne transformacje; uporządkowana lista
  transformacji pola jest składana w łańcuch dekoratorów stosowanych kolejno do wartości.
- Transformacja ma klucz, nazwę, opis oraz najwyżej jeden parametr; wartość parametru (tekst) podaje pole kafelka,
  a transformacja sama ją interpretuje.
- Katalog MVP:
    - round(decimals) - zaokrąglenie liczby do 0-10 miejsc po przecinku,
    - date_format(pattern) - formatowanie daty według wzorca w kulturze niezmiennej,
    - prefix(text) - doklejenie tekstu przed wartością,
    - suffix(text) - doklejenie tekstu po wartości,
    - uppercase - zamiana tekstu na wielkie litery,
    - lowercase - zamiana tekstu na małe litery,
    - truncate(max_length) - przycięcie tekstu do maksymalnej liczby znaków,
    - mapper(dictionary) - zamiana wartości na wartość ze słownika; parametr `"<tileId>:<etykieta pola wartości>"`
      wskazuje kafelek-słownik (kafelek z `items`, kluczem słownika jest pole `items.key`) i pole elementu z wartością.
- Słowniki transformacji `mapper` są ładowane do pamięci aplikacji raz po starcie (ze wszystkich odwołań `mapper`)
  oraz w tle po zapisie kafelka-słownika albo kafelka z niezaładowanym odwołaniem; nie są odświeżane okresowo.
  Klucze są porównywane jako tekst, a wartość zachowuje typ. Brak klucza daje błąd `transformation_failed`
  z powodem `missing_dictionary_key`, a niezaładowany słownik — z powodem `dictionary_unavailable`.
- Użytkownik może odczytać katalog dostępnych transformacji wraz z opisem parametru.
- Kombinatory pól są predefiniowane w kodzie jak transformacje, ale składają jedną wartość z wartości kilku pól
  kafelka. Kombinator implementuje wspólny interfejs, ma klucz, nazwę, opis, najwyżej jeden parametr (tekst) i sam
  interpretuje parametr oraz listę pól wejściowych (etykiet): wskazuje, których pól potrzebuje, waliduje je i składa
  wartość.
- Katalog kombinatorów MVP:
    - join(separator) - sklejenie wartości pól w kolejności listy separatorem (brak parametru = pusty separator);
      `null` jest pomijany, tablica jest spłaszczana, a jej elementy łączone tym samym separatorem,
    - template(pattern) - tekst ze wzorca z etykietami pól w klamrach, np. `"{Ulica} {Nr}, {Miasto}"` (`{{` i `}}`
      to dosłowne klamry); pola wynikają ze wzorca, `null` wstawia pusty tekst,
    - object - obiekt JSON, w którym kluczem jest etykieta pola wejściowego, a wartością wartość pola (także `null`
      i tablica),
    - coalesce - pierwsza wartość różna od `null` z co najmniej dwóch pól.
- Użytkownik może odczytać katalog dostępnych kombinatorów wraz z opisem parametru i wymaganą liczbą pól.

3.2. Definicje połączeń

- Dodawanie, odczyt listy, odczyt szczegółów, edycja i usuwanie definicji połączenia.
- Połączenie jest szablonem żądania: nazwa (wymagana, 1-100 znaków, bez wymogu unikalności), szablon URL (http lub
  https) z parametrami ścieżki `{name}`, rodzaj GET lub POST, deklaracje parametrów ścieżki i query oraz dla POST szablon
  body JSON z deklaracjami parametrów body.
- Połączenie nie definiuje nagłówków: wszystkie połączenia wysyłają te same nagłówki predefiniowane w aplikacji (treść
  POST zawsze jako `Content-Type: application/json`).
- Deklaracja parametru zawiera nazwę, informację, czy jest wymagany, oraz opcjonalną wartość domyślną. Wartości
  parametrów są przekazywane w czasie wykonania i podmieniane w szablonie.
- Brak obsługi uwierzytelniania do zewnętrznych API.
- Usunięcie połączenia używanego przez źródło dowolnego kafelka jest blokowane.

3.3. Definicje kafelków

- Dodawanie, odczyt listy, odczyt szczegółów, edycja i usuwanie definicji kafelka.
- Kafelek jest samodzielnym, reużywalnym komponentem. Określa, co pokazuje i jak (typ wyświetlania, pola z rolami,
  elementy, źródła danych, osadzone dzieci, wiązania wewnętrzne), ale nie wie, gdzie jest osadzony ani skąd dostaje
  wejścia. Kafelki nic o sobie nie wiedzą; do innych kafelków odwołują się wyłącznie przez osadzenia dzieci
  i akcje.
- Typ wyświetlania `display` jest wymagany:

  | `display` | Dane | Jak wyświetla front | Wymaga w definicji |
  |---|---|---|---|
  | `table` | wiele elementów | wiersze i kolumny (kolumna = pole) | `items` z `key` |
  | `list` | wiele elementów | element jako tytuł i opis | `items` z `key`, pole z rolą `title` |
  | `details` | jeden rekord | pary etykieta–wartość | bez `items` |
  | `summary` | jeden rekord | kilka wyróżnionych wartości | bez `items`, co najmniej jedno pole z rolą `primary` |
  | `form` | wartości wpisane przez użytkownika | formularz | pola formularza; bez źródeł, `items`, dzieci i wiązań |
  | `pagination_table` | strona elementów z API | tabela z paginacją | jak `table` oraz `pagination` |
  | `tabs` | zakładki | zakładki z osadzonymi kafelkami | dzieci `scope: tile` z `label`; bez pól, źródeł i `items` |

- Opcjonalna rola pola (`role`): `title`, `subtitle`, `badge` dla `list`; `primary`, `secondary` dla `summary`. Rola
  niezgodna z `display` to błąd walidacji; `table`, `pagination_table` i `details` nie przyjmują ról.
- Źródła danych: lista z unikalnym w kafelku kluczem `key`. Źródło `connection` pobiera dane wskazanym połączeniem;
  źródło `input` to wejście danych, przez które kafelek dostaje gotowy JSON (obiekt, tablicę albo wartość prostą),
  a klucz źródła jest nazwą wejścia w kontrakcie kafelka. Kafelek może mieszać oba rodzaje źródeł.
- Pole kafelka (poza formularzem) zawiera źródło (`source`), ścieżkę JSONPath, etykietę, opcjonalną rolę, flagę
  `hidden` (domyślnie `false`) oraz uporządkowaną listę transformacji z katalogu (może być pusta). Transformacja ma
  `key` i opcjonalny `parameter` — prostą wartość (tekst albo liczba, zapisywana jako tekst).
- Pole złożone ma zamiast `source` i `path` kombinator `combine`: `key` z katalogu, opcjonalny `parameter` (tekst)
  i opcjonalne `fields` (etykiety pól wejściowych tego kafelka). Wejściem jest wartość pola po jego transformacjach;
  wejście nie może być polem złożonym ani samym polem. Wynik przechodzi przez łańcuch transformacji pola (pole
  z `object` nie ma transformacji), może być ukryty, mieć rolę i zasilać wiązania z `parent` i z dziecka oraz akcje;
  nie należy do żadnego źródła, więc nie zasila wiązań źródło → źródło. `template` nie przyjmuje pól, których ścieżka
  zwraca tablicę. W kafelku z `items` wejścia pochodzą ze źródła elementów, a pole złożone jest polem elementu i nie
  może być kluczem `items.key`.
- Ścieżka JSONPath obsługuje podzbiór składni: `$`, `.name`, `['name']`, `[n]` (także ujemny), `[*]`, `.*` i filtr
  `[?@.a.b == 'x']` (także `[?(...)]`). Filtr porównuje operatorem `==` lub `!=` wartość pod ścieżką względną `@`
  (segmenty `.name`, `['name']`, `[n]`) z literałem: tekstem, liczbą, `true`, `false` albo `null`; liczby porównywane są
  wartością, wartości różnych typów są różne, a brak wartości nie spełnia `==` i spełnia `!=`. Ścieżka bez wildcardu
  i filtra wskazuje jedną wartość; ścieżka z wildcardem lub filtrem zwraca płaską tablicę dopasowań, a każdy jej
  element przechodzi przez łańcuch transformacji pola.
- Etykieta pola jest unikalna w kafelku (porównanie bez rozróżniania wielkości liter, po przycięciu białych znaków).
- Pole ukryte (`hidden: true`) jest zwracane w podglądzie kafelka z oznaczeniem `"hidden": true` (front go nie
  pokazuje), przechodzi przez transformacje i może być źródłem wiązania.
- Elementy (`items`) kafelka `table`, `list` i `pagination_table`: `source` (klucz źródła z tablicą elementów), `path` (JSONPath
  z wildcardem lub filtrem, np. `$.cities[*]`) i `key` (etykieta pola identyfikującego element). Ścieżki pól są
  względne wobec elementu, a pola pochodzą ze źródła `items.source`. Pole innego źródła jest dozwolone tylko
  z `hidden: true` i tylko jako źródło wiązania źródło → źródło.
- Kafelek `form` zbiera wartości od użytkownika i wystawia je jako swoje pola; nic nie pobiera i nie ma źródeł,
  `items`, dzieci ani wiązań. Pole formularza ma unikalną `label`, `type` (`text`, `number`, `boolean`, `date` jako tekst
  ISO 8601 `yyyy-MM-dd`), `required` (domyślnie `false`) i opcjonalne `default` zgodne z typem. Wartością pola jest
  wartość podana w żądaniu podglądu dla tego osadzenia formularza, a bez niej `default`. Pole wymagane bez wartości
  i bez `default` wstrzymuje cele swoich wiązań (`awaiting_input`); pole opcjonalne bez wartości oznacza brak wartości.
  Wartość niezgodna z typem daje błąd pola `invalid_value`, a cele wiązań z tego pola dostają `dependency_failed`.
- Stronicowanie (`pagination`, wymagane tylko i wyłącznie dla `pagination_table`): stronicuje API, a moduł przekazuje
  mu numer i rozmiar strony parametrami połączenia źródła elementów (źródło `connection`) o nazwach
  `pageNumberParameter` (domyślnie `pageNumber`) i `pageSizeParameter` (domyślnie `pageSize`), różnych
  i zadeklarowanych w połączeniu. `pageSize` (1–100) to domyślny rozmiar strony, a opcjonalny `totalPath` (JSONPath bez
  wildcardu i filtra) wskazuje łączną liczbę elementów w odpowiedzi. Strony są numerowane od 1; oba parametry zostają
  w kontrakcie kafelka.
- Kafelek `tabs` układa dzieci w zakładki: ma co najmniej jedno dziecko, wszystkie `scope: tile` z `label` (tytuł
  zakładki), w kolejności `position`; nie ma pól, źródeł, `items`, `pagination` ani akcji. Zakładką może być dowolny
  kafelek z ładowaniem `eager` albo `lazy`. Wiązania kafelka `tabs` łączą zakładki (dziecko → dziecko lub potomek).
  Aktywna zakładka to stan frontu.
- Akcje (`actions`, opcjonalne; nie dla `form` i `tabs`) prowadzą do innego kafelka-korzenia: `key` (unikalny wśród
  akcji, bez znaku `/`), `label` (1–100 znaków), `type` (`button` — domyślny, `link`), `tileId` (kafelek docelowy, także
  ten sam), `scope` (`item` — przy każdym elemencie, tylko u kafelka z `items` i u niego domyślny; `tile` — raz na
  kafelek) i `map` (może być puste): `field` (dla `item` pole źródła elementów, dla `tile` pole spoza niego) i dokładnie
  jedno z `parameter` (parametr kontraktu celu) i `input` (wejście danych celu), każde wejście celu najwyżej raz.
- Osadzenia dzieci (`children`): `key` (unikalny w rodzicu, bez znaku `/`), `tileId` (samodzielny kafelek), `position`
  (unikalna w rodzicu), opcjonalny `label` (do 100 znaków; tytuł zakładki, wymagany u rodzica `tabs`), opcjonalny
  `cssClass` (do 200 znaków, sposób ułożenia w rodzicu), `scope` (`tile` — jedna
  instancja, `item` — instancja na element rodzica, tylko u rodzica z `items`) i `load` (`eager` — z rodzicem, `lazy` —
  na żądanie frontu). Formularz może być osadzony tylko ze `scope: tile`. Ten sam kafelek może być osadzony kilka razy.
  Pola kafelka wyświetlają się przed dziećmi, dzieci w kolejności `position`; dzieci `scope: item` trafiają do obszaru
  elementu. Brak cykli zawierania; liczba poziomów zagnieżdżenia nie jest ograniczona.
- Wiązania w kafelku (`bindings`, opcjonalne): `from` → `to` z listą mapowań `map` (`field` — etykieta pola węzła
  źródłowego, dokładnie jedno z `parameter` i `input`, opcjonalny `scope`). Jedno wiązanie przekazuje dowolną liczbę
  wartości: `map` może zawierać wiele mapowań (np. pola „Lat” i „Lon” do parametrów `lat` i `lon`), a jedno pole może
  zasilać kilka parametrów lub wejść celu. Węzły kafelka to jego źródła, osadzenia
  dzieci i `parent` (pola samego kafelka); tworzą jedną przestrzeń nazw. Dozwolone pary: źródło → źródło
  (`connection` → `connection`), dziecko `scope: tile` → źródło, `parent` → dziecko lub potomek, dziecko → dziecko lub
  potomek. Cel może być wskazany ścieżką osadzeń (`shipping/carrier`) i musi być wejściem kafelka docelowego
  niezwiązanym przez kafelek pośredni. Źródło też może być ścieżką osadzeń `scope: tile` (`filters/byName`): pole
  pochodzi wtedy z kontraktu ostatniego kafelka ścieżki, a wiązanie działa jak wiązanie z dziecka pierwszego segmentu.
  Ścieżki nie mają limitu długości. Zakres `scope`: brak (wartość pola), `selected` (domyślny dla
  węzła z `items`; pole elementu wybranego przez użytkownika), `all` (lista wartości pola ze wszystkich elementów),
  `item` (pole bieżącego elementu rodzica dla dziecka `scope: item`).
- Walidacja wiązań kafelka: węzły istnieją, `from` i `to` są różne, para występuje najwyżej raz, `map` ma co najmniej
  jedno mapowanie, `field` istnieje w węźle źródłowym (dla źródła pochodzi z tego źródła), `parameter` jest
  zadeklarowany w połączeniu docelowym albo jest wejściem-parametrem kontraktu kafelka docelowego, `input` jest wejściem
  danych celu, każde wejście celu ma najwyżej jedno źródło, `scope` jest zgodny ze źródłem, a wiązania nie tworzą cyklu
  we wspólnym grafie źródeł i dzieci.
- Kontrakt kafelka (sekcja `contract` w szczegółach) jest wyliczany z definicji: `parameters` (parametry połączeń
  źródeł niezwiązane wiązaniem wewnętrznym w danym połączeniu), `inputs` (wejścia danych) i `outputs` (etykiety pól,
  także ukrytych i pól formularza, oraz informacja, czy kafelek ma elementy). Kontrakt nie obejmuje niezwiązanych wejść
  potomków.
- Usunięcie kafelka osadzonego jako dziecko w innym kafelku albo wskazanego przez akcję jest blokowane (409
  `tile_in_use` z listą kafelków); połączenia pozostają.
- Edycja kafelka, która usuwa pole albo zmienia etykietę pola użytego jako źródło wiązania w rodzicu albo w przodku
  (ścieżka `from`), jest blokowana
  (409 `field_in_use` z listą pól i kafelków). Zmiana ścieżki, źródła lub transformacji pola bez
  zmiany etykiety nie jest blokowana. Usunięcie albo zmiana nazwy parametru, wejścia lub klucza osadzenia będącego
  celem wiązania nie jest blokowana; w podglądzie wiązanie daje `invalid_binding`.

3.4. Widoki

- Moduł nie ma osobnych definicji widoków (decyzja dewelopera z 2026-10-23, zastępuje wcześniejsze TEMP-024 … TEMP-029).
  Rolę widoku pełni kafelek złożony: korzeń drzewa osadzeń z dziećmi, formularzami i wiązaniami.
- Wartości przechodzą między kafelkami wyłącznie przez jawne wiązanie kafelek → kafelek; wartości od użytkownika
  pochodzą z kafelków `form`; kafelki nie znają się nawzajem. Bez wiązania nic nie jest przekazywane.

3.5. Test i podgląd

- Test połączenia wykonuje żądanie zgodnie z definicją (zapisaną po id lub niezapisaną przekazaną w żądaniu) i
  wartościami parametrów przekazanymi w żądaniu testu. Sukces
  zwraca status HTTP odpowiedzi, czas odpowiedzi oraz pełną treść JSON (bez obcinania); niepowodzenie zwraca odpowiedź
  błędu z kodem (source_unavailable, unexpected_status, invalid_json, response_too_large).
- Podgląd kafelka (zapisanego i niezapisanego) zwraca JSON kafelka wygenerowany zgodnie z definicją: `display`, pola
  (etykieta i przetransformowana wartość albo kod błędu) albo elementy (`itemKey`, `items` z kluczem, polami i dziećmi
  `scope: item`, albo `itemsError`) oraz dzieci `scope: tile`. Żądanie podglądu kafelka przyjmuje opcjonalne `forms`
  (wartości formularzy według adresu osadzenia formularza), `parameters` (wartości testowe albo doręczone dla
  wejść-parametrów kontraktu), `inputs` (wejścia danych) i `descendants` (wartości doręczane potomkom według ścieżki).
  Wartości testowe nie są źródłem wiązań; wiązanie w definicji wygrywa. Kafelek nie przechowuje wartości.
- Kolejność wykonania wynika z wiązań: węzły (źródła i dzieci) są dzielone na fale (algorytm Kahna), węzły jednej fali
  wykonują się równolegle. Mapa wartości węzła to wartości doręczone uzupełnione wartościami z wiązań (wiązanie wygrywa,
  najbliższe wiązanie wygrywa z wiązaniem przodka). Przekazywana jest wartość pola po transformacjach; `null` oznacza
  brak wartości; liczby i wartości logiczne dla parametrów ścieżki i zapytania są zamieniane na tekst.
- Wiązania zależne od działania użytkownika (`selected`) i dzieci `lazy` wykonuje front; pozostałe wykonuje serwer.
  Każde osadzenie w odpowiedzi ma `address` (ścieżka instancji od korzenia, z kluczami elementów, np.
  `orders[17]/shipping`) i stan: `data` (z zagnieżdżonym podglądem), `awaiting_input` (czeka na wybór albo na wymagane
  pole formularza; `inputs` i `pending`), `awaiting_load` (`lazy` albo przekroczony limit; `inputs`) albo `error`
  (`dependency_failed` z przyczyną albo `invalid_binding`). `inputs` ma kształt żądania podglądu kafelka, więc front
  doładowuje osadzenie bez znajomości definicji rodzica.
- Osadzenie z `label` zwraca go w odpowiedzi (tytuł zakładki). Zakładki kafelka `tabs` są wykonywane jak inne dzieci;
  front pokazuje aktywną zakładkę i wysyła wartości jej formularza.
- `pagination_table` wywołuje źródło elementów ze stroną z wartości parametru numeru strony (domyślnie 1) i rozmiarem
  z wartości parametru rozmiaru (domyślnie `pageSize`); wartość musi być liczbą całkowitą (numer ≥ 1, rozmiar 1–100),
  inaczej elementy mają błąd `invalid_parameters`. Odpowiedź zawiera `pagination`: `page`, `pageSize`, `total`
  i `pages` (gdy pod `totalPath` jest liczba całkowita), `previous` (gdy `page > 1`) i `next` (gdy `page < pages`,
  a bez `pages` — gdy strona jest pełna). `previous` i `next` mają kształt żądania podglądu kafelka (wartości doręczone
  instancji z ustawioną stroną), więc front zmienia stronę bez znajomości definicji rodzica.
- Akcje są zwracane przy elementach (`scope: item`) albo w kafelku (`scope: tile`): `key`, `label`, `type`, `tileId`
  i `inputs` celu (wartości pól po transformacjach, w kształcie żądania podglądu) albo `error` (`dependency_failed`
  z przyczyną przy błędzie pola, `invalid_binding`, gdy cel nie deklaruje już parametru lub wejścia). Front otwiera cel
  jako nowy korzeń podglądem kafelka `tileId` z `inputs`.
- Parametry są walidowane osobno dla każdego połączenia, tuż przed jego wywołaniem. Brak wymaganego parametru, zły typ
  albo brak wejścia danych daje błąd pól tego źródła `invalid_parameters` ze szczegółami (parametr, komunikat, czy
  wartość pochodzi z wiązania); pozostałe pola zwracają dane. Podgląd kafelka zwraca zawsze status 200 (zasada
  częściowych wyników); walidacja kształtu samego żądania pozostaje.
- Pola złożone są wyliczane po polach ze źródeł (w kafelku z `items` osobno dla każdego elementu), a w odpowiedzi
  zachowują kolejność z definicji. Błąd pola wejściowego daje polu złożonemu `dependency_failed` z przyczyną (źródło,
  pole, kod), a wartość, której kombinator nie umie złożyć (np. obiekt w `join`), daje `combination_failed`
  z kluczem kombinatora i powodem.
- Błąd źródła, brak wartości pod ścieżką lub błąd transformacji nie przerywa podglądu: zwracany jest częściowy wynik z
  błędem per pole (source_unavailable, path_not_found, transformation_failed, combination_failed, invalid_parameters,
  dependency_failed, invalid_binding).
- Każde połączenie użyte w instancji kafelka jest wywoływane najwyżej raz, a wywołanie tego samego połączenia z tymi
  samymi wartościami parametrów jest wykonywane raz na całe żądanie podglądu (bez przechowywania między żądaniami).
- Limity podglądu (stałe w kodzie): 50 elementów na osadzenie `eager` ze `scope: item`, 200 wywołań zewnętrznych na
  żądanie (po deduplikacji), 10 s na całe żądanie i 8 równoległych wywołań instancji dzieci. Osadzenia niewykonane
  z powodu limitu dostają `awaiting_load`; źródła samego korzenia wykonują się zawsze. Czas podglądu rośnie z długością
  łańcucha wiązań i głębokością drzewa.
- Wyniki testów i podglądów nie są nigdzie zapisywane.
- Żądania do zewnętrznych API mają globalny timeout i limit rozmiaru odpowiedzi.

3.6. Utrwalanie i bezpieczeństwo

- Definicje są przechowywane w PostgreSQL zgodnie z planem bazy danych modułu (db-plan.md).
- Logi nie zawierają nagłówków, body żądań ani treści odpowiedzi zewnętrznych API.

## 4. Granice produktu

- Konta użytkowników, uwierzytelnianie oraz przypisywanie użytkowników lub grup (GUID) do konfiguracji; w MVP wszystkie
  definicje są wspólne. W przyszłości użytkownicy będą przypisywani wyłącznie do kafelków-korzeni pełniących rolę widoku,
  transformacji ani połączeń.
- Uwierzytelnianie do zewnętrznych API (klucze API, tokeny, Basic auth).
- Docelowe pobieranie danych na potrzeby wyświetlania (należy do innych modułów); podgląd kafelka służy sprawdzeniu
  konfiguracji.
- Frontend renderujący kafelki.
- Źródła danych inne niż REST (SQL, GraphQL, pliki).
- Harmonogramy pobierania, cache wyników połączeń i historia wyników testów (wyjątek: słowniki transformacji
  `mapper` trzymane w pamięci aplikacji).
- Słowniki stałe (enumy) bez endpointu i źródło kafelka `static`, okresowe odświeżanie słowników i ich przeładowanie
  po zmianie połączenia.
- Definiowanie, edycja i usuwanie transformacji i kombinatorów przez użytkownika; nowe dodaje się wyłącznie w kodzie.
- Test transformacji na przykładowej wartości; efekt transformacji jest widoczny w podglądzie kafelka.
- Operacje arytmetyczne i agregaty na tablicach w transformacjach.
- Edytor wizualny typu drag & drop.
- Blokada adresów wewnętrznych (localhost, sieci prywatne) w testach połączeń; w MVP świadomie akceptowane ryzyko SSRF,
  ograniczone jedynie timeoutem i limitem rozmiaru odpowiedzi.
- Agregaty, także w kafelku `summary`.
- Wielokrotny wybór elementów, wybór zapisany w URL i symulacja wyboru elementu na serwerze.
- Dziecko wstawione między pola kafelka (`region`) i kafelki prywatne (widoczne tylko w rodzicu).
- Składanie parametru z kilku pól w wiązaniu źródło → źródło (pole złożone `template` zasila tylko wiązania z `parent`
  i z dziecka) i różne wartości dla parametrów o tej samej nazwie w różnych połączeniach jednego kafelka.
- Pole złożone z innych pól złożonych, kombinatory z wejściem spoza kafelka i grupy pól jako sekcje prezentacji.
- Limit długości łańcucha wiązań.
- Wiązanie jako samodzielny, współdzielony zasób.
- Renderowanie kafelków, obsługa wyboru elementów i formularzy we froncie; moduł dostarcza tylko kontrakt i odpowiedzi.
- Formularz: listy wyboru, wiele wartości w jednym polu, walidacja wzorcem, wartości startowe z wiązań i formularz
  zależny od innego kafelka.
- Wartości przekazywane do kafelków poza wiązaniami (płaska mapa parametrów rozsyłana po nazwie).

## 5. Historyjki użytkowników

Historyjki są uporządkowane w kolejności realizacji, od najmniejszych funkcjonalności: katalog transformacji pól,
połączenia, kafelki, widoki i podgląd widoku (TEMP-001 … TEMP-029; widoki wycofane w TEMP-038). Kolejna grupa, kafelki
złożone i wiązania (TEMP-030 … TEMP-039), rozbudowuje kafelki etapami: unikalna etykieta i ukryte pole, walidacja
parametrów per połączenie, typ wyświetlania i elementy, źródła z wiązaniami źródło → źródło, wejścia danych, formularze,
dzieci `scope: tile`, dzieci `scope: item` z limitami, usunięcie widoków na rzecz kafelków złożonych i wybór elementu. TEMP-040 dodaje filtr w ścieżce JSONPath, a TEMP-041 …
TEMP-043 zakładki, tabelę ze stronicowaniem i akcje przejścia do kafelka (wyszukiwarka z filtrami). TEMP-044 dodaje
kombinatory pól (pola złożone z kilku pól kafelka), a TEMP-045 transformację `mapper` ze słownikami z kafelków.
Każda grupa korzysta wyłącznie z grup wcześniejszych; blokada usuwania połączeń używanych przez kafelki należy do grupy
kafelków. Historyjki i scenariusze leżą w `docs/scenarios/scenario_TEMP-*`.

## 6. Metryki sukcesu

- Użytkownik konfiguruje połączenie, kafelek z gotowymi transformacjami i kafelek złożony bez zmian w kodzie aplikacji.
- Podgląd kafelka korzystającego z szybkich źródeł zwraca wynik w czasie poniżej 2 s (dotyczy kafelków bez łańcuchów
  wiązań i bez dzieci z własnymi połączeniami).
- Awaria jednego źródła nie blokuje podglądu pól z pozostałych źródeł (100% podglądów z częściowym wynikiem zamiast
  błędu serwera).
- Konfiguracja odtwarza się w całości po restarcie aplikacji.
- 100% prób usunięcia używanych połączeń jest blokowanych z listą zależnych kafelków.
- 100% prób usunięcia kafelka używanego jako źródło wiązania albo dziecko jest blokowanych z listą zależności.
