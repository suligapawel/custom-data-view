# Dokument wymagań produktu (PRD) - CustomDataView

## 1. Przegląd produktu

Moduł CustomDataView pozwala zdefiniować, skąd i w jaki sposób mają być pobierane dane z zewnętrznych REST API oraz jak
mają zostać zaprezentowane użytkownikowi. Przechowuje definicje połączeń, kafelków i widoków użytkownika,
udostępnia katalog gotowych transformacji pól, a także umożliwia sprawdzenie, czy połączenie i definicja kafelka generują dane zgodne z oczekiwaniami.
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
- Transformacja ma klucz, nazwę, opis oraz opcjonalne parametry; wartości parametrów podaje pole kafelka.
- Katalog MVP:
    - round(decimals) - zaokrąglenie liczby do 0-10 miejsc po przecinku,
    - date_format(pattern) - formatowanie daty według wzorca w kulturze niezmiennej,
    - prefix(text) - doklejenie tekstu przed wartością,
    - suffix(text) - doklejenie tekstu po wartości,
    - uppercase - zamiana tekstu na wielkie litery,
    - lowercase - zamiana tekstu na małe litery,
    - truncate(max_length) - przycięcie tekstu do maksymalnej liczby znaków.
- Użytkownik może odczytać katalog dostępnych transformacji wraz z opisem parametrów.

3.2. Definicje połączeń

- Dodawanie, odczyt listy, odczyt szczegółów, edycja i usuwanie definicji połączenia.
- Definicja połączenia zawiera: nazwę (wymagana, 1-100 znaków, bez wymogu unikalności), URL (http lub https), metodę
  GET lub POST, parametry query, nagłówki oraz body JSON (tylko dla POST).
- Brak obsługi uwierzytelniania do zewnętrznych API.
- Usunięcie połączenia używanego przez pole dowolnego kafelka jest blokowane.

3.3. Definicje kafelków

- Dodawanie, odczyt listy, odczyt szczegółów, edycja i usuwanie definicji kafelka.
- Kafelek zawiera nazwę oraz uporządkowaną listę pól (co najmniej jedno).
- Pole kafelka wiąże dane ze źródłem: odwołanie do połączenia, ścieżka JSONPath, etykieta oraz uporządkowana lista
  transformacji z katalogu wraz z wartościami ich parametrów (może być pusta). Pola jednego kafelka mogą korzystać z
  różnych połączeń.
- Usunięcie kafelka usuwa go ze wszystkich widoków; połączenia pozostają.

3.4. Definicje widoków

- Dodawanie, odczyt listy, odczyt szczegółów, edycja i usuwanie definicji widoku.
- Widok zawiera nazwę oraz siatkę: dla każdego kafelka wiersz, kolumnę, szerokość i wysokość.
- Walidacja siatki: wszystkie wartości >= 1, kafelek mieści się w 12 kolumnach, kafelki nie nachodzą na siebie, dany
  kafelek występuje w widoku najwyżej raz.

3.5. Test i podgląd

- Test połączenia wykonuje żądanie zgodnie z definicją (zapisaną po id lub niezapisaną przekazaną w żądaniu). Sukces
  zwraca status HTTP odpowiedzi, czas odpowiedzi oraz obciętą treść JSON; niepowodzenie zwraca odpowiedź błędu z kodem
  (source_unavailable, unexpected_status, invalid_json, response_too_large).
- Podgląd kafelka zwraca JSON kafelka wygenerowany zgodnie z definicją (zapisaną lub niezapisaną): dla każdego pola
  etykietę i przetransformowaną wartość albo kod błędu.
- Błąd źródła, brak wartości pod ścieżką lub błąd transformacji nie przerywa podglądu: zwracany jest częściowy wynik z
  błędem per pole (source_unavailable, path_not_found, transformation_failed) i status HTTP 200.
- Każde połączenie użyte w podglądzie kafelka jest wywoływane najwyżej raz.
- Wyniki testów i podglądów nie są nigdzie zapisywane.
- Żądania do zewnętrznych API mają globalny timeout i limit rozmiaru odpowiedzi.

3.6. Utrwalanie i bezpieczeństwo

- Definicje są przechowywane w PostgreSQL zgodnie z planem bazy danych modułu (db-plan.md).
- Logi nie zawierają nagłówków, body żądań ani treści odpowiedzi zewnętrznych API.

## 4. Granice produktu

- Konta użytkowników, uwierzytelnianie oraz przypisywanie użytkowników lub grup (GUID) do konfiguracji; w MVP wszystkie
  definicje są wspólne. W przyszłości użytkownicy będą przypisywani wyłącznie do pełnych widoków, nie do kafelków,
  transformacji ani połączeń.
- Uwierzytelnianie do zewnętrznych API (klucze API, tokeny, Basic auth).
- Docelowe pobieranie danych na potrzeby wyświetlania (należy do innych modułów).
- Frontend renderujący widoki.
- Podgląd całego widoku z danymi.
- Źródła danych inne niż REST (SQL, GraphQL, pliki).
- Harmonogramy pobierania, cache i historia wyników testów.
- Definiowanie, edycja i usuwanie transformacji przez użytkownika; nowe transformacje dodaje się wyłącznie w kodzie.
- Test transformacji na przykładowej wartości; efekt transformacji jest widoczny w podglądzie kafelka.
- Operacje arytmetyczne i agregaty na tablicach w transformacjach.
- Edytor wizualny typu drag & drop.
- Blokada adresów wewnętrznych (localhost, sieci prywatne) w testach połączeń; w MVP świadomie akceptowane ryzyko SSRF,
  ograniczone jedynie timeoutem i limitem rozmiaru odpowiedzi.

## 5. Historyjki użytkowników

Historyjki są uporządkowane w kolejności realizacji, od najmniejszych funkcjonalności: katalog transformacji pól,
połączenia, kafelki, widoki. Każda grupa korzysta wyłącznie z grup wcześniejszych; blokada usuwania połączeń używanych
przez kafelki należy do grupy kafelków.

- ID: TEMP-007  
  Tytuł: Dodanie połączenia GET  
  Opis: Jako użytkownik chcę zdefiniować połączenie GET do zewnętrznego REST API, aby móc wskazywać dane z jego
  odpowiedzi.  
  Kryteria akceptacji:
    - Można zapisać połączenie z nazwą, URL, metodą GET, opcjonalnymi parametrami query i nagłówkami.
    - Odpowiedź zawiera identyfikator nowego połączenia.
    - Zapisane połączenie jest widoczne na liście połączeń.

- ID: TEMP-008  
  Tytuł: Dodanie połączenia POST z body  
  Opis: Jako użytkownik chcę zdefiniować połączenie POST z body JSON, aby korzystać z API wymagających zapytań w treści
  żądania.  
  Kryteria akceptacji:
    - Można zapisać połączenie z metodą POST i body JSON.
    - Body jest przechowywane i zwracane w szczegółach połączenia bez zmian.
    - Połączenie POST bez body jest dozwolone.

- ID: TEMP-009  
  Tytuł: Walidacja definicji połączenia  
  Opis: Jako użytkownik chcę otrzymać informację o błędach w definicji połączenia, aby nie zapisać niedziałającej
  konfiguracji.  
  Kryteria akceptacji:
    - Brak nazwy lub nazwa dłuższa niż 100 znaków zwraca błąd walidacji.
    - URL pusty, niebezwzględny lub o schemacie innym niż http/https zwraca błąd walidacji.
    - Metoda inna niż GET lub POST zwraca błąd walidacji.
    - Body podane dla metody GET zwraca błąd walidacji.
    - Body, które nie jest poprawnym JSON, zwraca błąd walidacji.
    - Nagłówek lub parametr query z pustą nazwą zwraca błąd walidacji.
    - Nazwy nie muszą być unikalne; dwa połączenia o tej samej nazwie zapisują się poprawnie.
    - Przy błędzie walidacji nic nie zostaje zapisane.

- ID: TEMP-010  
  Tytuł: Przeglądanie połączeń  
  Opis: Jako użytkownik chcę zobaczyć listę i szczegóły połączeń, aby wiedzieć, jakie źródła danych są
  skonfigurowane.  
  Kryteria akceptacji:
    - Lista zwraca identyfikator, nazwę, metodę i URL każdego połączenia.
    - Lista jest pusta, gdy nie zdefiniowano żadnego połączenia.
    - Szczegóły zwracają pełną definicję: nazwę, URL, metodę, parametry query, nagłówki i body.
    - Szczegóły nieistniejącego połączenia zwracają 404.

- ID: TEMP-011  
  Tytuł: Edycja połączenia  
  Opis: Jako użytkownik chcę zmienić definicję połączenia, aby dostosować ją do zmian w zewnętrznym API.  
  Kryteria akceptacji:
    - Edycja zastępuje nazwę, URL, metodę, parametry query, nagłówki i body.
    - Edycja podlega tym samym regułom walidacji co dodanie.
    - Kafelki korzystające z połączenia używają po edycji nowej definicji.
    - Edycja nieistniejącego połączenia zwraca 404.

- ID: TEMP-012  
  Tytuł: Usunięcie połączenia  
  Opis: Jako użytkownik chcę usunąć połączenie, którego już nie potrzebuję, aby utrzymać porządek w konfiguracji.  
  Kryteria akceptacji:
    - Połączenie nieużywane przez żadne pole kafelka zostaje usunięte.
    - Usunięte połączenie nie występuje na liście, a jego szczegóły zwracają 404.
    - Usunięcie nieistniejącego połączenia zwraca 404.

- ID: TEMP-013  
  Tytuł: Test połączenia  
  Opis: Jako użytkownik chcę przetestować połączenie, zapisane lub jeszcze niezapisane, aby sprawdzić, czy zewnętrzne
  API odpowiada zgodnie z definicją.  
  Kryteria akceptacji:
    - Test przyjmuje identyfikator zapisanego połączenia albo pełną definicję połączenia w żądaniu; oba warianty działają
      tak samo.
    - Definicja przekazana w żądaniu podlega tym samym regułom walidacji co dodanie połączenia.
    - Test wykonuje żądanie zgodnie z definicją (URL, metoda, query, nagłówki, body).
    - Odpowiedź 2xx z poprawnym JSON kończy test sukcesem: wynik zawiera status HTTP, czas odpowiedzi i obciętą treść
      JSON.
    - Każdy inny wynik kończy test odpowiedzią błędu z kodem: source_unavailable (timeout lub brak połączenia),
      unexpected_status (status inny niż 2xx, wraz z otrzymanym statusem), invalid_json lub response_too_large.
    - Test nie tworzy ani nie zmienia połączenia, a jego wynik nie jest zapisywany.
    - Test nieistniejącego połączenia zwraca 404.

- ID: TEMP-014  
  Tytuł: Ograniczenia żądań do zewnętrznych API  
  Opis: Jako operator systemu chcę, aby testy i podglądy miały limity czasu i rozmiaru, aby pojedyncza definicja nie
  zablokowała aplikacji.  
  Kryteria akceptacji:
    - Każde żądanie do zewnętrznego API jest przerywane po przekroczeniu globalnego timeoutu.
    - Odpowiedź przekraczająca globalny limit rozmiaru nie jest wczytywana w całości i skutkuje błędem
      response_too_large lub source_unavailable dla pól podglądu.
    - Treść odpowiedzi zwracana w teście połączenia jest obcinana do ustalonego limitu.

- ID: TEMP-015  
  Tytuł: Dodanie kafelka  
  Opis: Jako użytkownik chcę zgrupować w kafelek pola wskazujące wartości z odpowiedzi połączeń, aby prezentować
  powiązane dane razem.  
  Kryteria akceptacji:
    - Można zapisać kafelek z nazwą i uporządkowaną listą pól.
    - Każde pole zawiera połączenie, ścieżkę JSONPath, etykietę i uporządkowaną listę transformacji z katalogu wraz z
      wartościami parametrów.
    - Pola w kafelku mogą korzystać z różnych połączeń; to samo połączenie może zasilać wiele pól.
    - Pole bez transformacji zwraca wartość bez zmian.
    - Kolejność pól jest zachowywana w szczegółach i podglądzie.
    - Odpowiedź zawiera identyfikator nowego kafelka.

- ID: TEMP-016  
  Tytuł: Walidacja definicji kafelka  
  Opis: Jako użytkownik chcę otrzymać informację o błędach w definicji kafelka, aby nie zapisać pustego lub
  niespójnego kafelka.  
  Kryteria akceptacji:
    - Brak nazwy lub nazwa dłuższa niż 100 znaków zwraca błąd walidacji.
    - Pusta lista pól zwraca błąd walidacji.
    - Brak etykiety pola lub etykieta dłuższa niż 100 znaków zwraca błąd walidacji.
    - Pusta lub składniowo niepoprawna ścieżka JSONPath zwraca błąd walidacji wskazujący pole.
    - Odwołanie do nieistniejącego połączenia zwraca błąd walidacji wskazujący pole i identyfikator.
    - Klucz transformacji spoza katalogu zwraca błąd walidacji wskazujący pole i klucz.
    - Brakujący, nadmiarowy lub niepoprawny parametr transformacji zwraca błąd walidacji wskazujący pole, transformację
      i parametr.
    - Przy błędzie walidacji nic nie zostaje zapisane.

- ID: TEMP-017  
  Tytuł: Przeglądanie kafelków  
  Opis: Jako użytkownik chcę zobaczyć listę i szczegóły kafelków, aby wybrać je do widoków.  
  Kryteria akceptacji:
    - Lista zwraca identyfikator, nazwę i liczbę pól każdego kafelka.
    - Szczegóły zwracają nazwę i uporządkowaną listę pól (połączenie, ścieżka, etykieta, transformacje z
      parametrami).
    - Szczegóły nieistniejącego kafelka zwracają 404.

- ID: TEMP-018  
  Tytuł: Edycja kafelka  
  Opis: Jako użytkownik chcę zmienić nazwę lub zestaw pól kafelka, aby dopasować go do potrzeb.  
  Kryteria akceptacji:
    - Edycja zastępuje nazwę i listę pól wraz z kolejnością.
    - Edycja podlega tym samym regułom walidacji co dodanie.
    - Widoki zawierające kafelek zachowują jego pozycję w siatce.
    - Edycja nieistniejącego kafelka zwraca 404.

- ID: TEMP-019  
  Tytuł: Usunięcie kafelka  
  Opis: Jako użytkownik chcę usunąć kafelek, zachowując połączenia, aby móc wykorzystać je w innych kafelkach.  
  Kryteria akceptacji:
    - Kafelek zostaje usunięty niezależnie od tego, czy jest użyty w widokach.
    - Kafelek znika z siatki wszystkich widoków, które go zawierały; same widoki pozostają.
    - Połączenia używane przez pola kafelka pozostają niezmienione.
    - Usunięcie nieistniejącego kafelka zwraca 404.

- ID: TEMP-020  
  Tytuł: Blokada usunięcia połączenia używanego przez kafelki  
  Opis: Jako użytkownik chcę, aby nie dało się usunąć połączenia używanego przez pola kafelków, aby nie zepsuć
  istniejących kafelków.  
  Kryteria akceptacji:
    - Usunięcie połączenia używanego przez pole co najmniej jednego kafelka zwraca błąd konfliktu (409).
    - Odpowiedź zawiera listę identyfikatorów i nazw kafelków korzystających z połączenia.
    - Przy konflikcie połączenie pozostaje niezmienione.
    - Po usunięciu ostatniego kafelka korzystającego z połączenia jego usunięcie się powiedzie.

- ID: TEMP-021  
  Tytuł: Podgląd zapisanego kafelka  
  Opis: Jako użytkownik chcę zobaczyć JSON kafelka wygenerowany zgodnie z definicją, aby sprawdzić, czy dane pobierają
  się poprawnie.  
  Kryteria akceptacji:
    - Podgląd zwraca identyfikator i nazwę kafelka oraz listę pól w kolejności definicji.
    - Każde pole zawiera etykietę i wartość po zastosowaniu łańcucha transformacji.
    - Każde połączenie użyte w kafelku jest wywoływane najwyżej raz, nawet gdy korzysta z niego wiele pól.
    - Wynik podglądu nie jest zapisywany.
    - Podgląd nieistniejącego kafelka zwraca 404.

- ID: TEMP-022  
  Tytuł: Podgląd niezapisanego kafelka  
  Opis: Jako użytkownik chcę zobaczyć podgląd kafelka przed jego zapisaniem, aby dopracować definicję.  
  Kryteria akceptacji:
    - Podgląd przyjmuje definicję kafelka w żądaniu i zwraca wynik w tym samym formacie co podgląd zapisanego kafelka.
    - Definicja podlega tym samym regułom walidacji co dodanie kafelka.
    - Podgląd nie tworzy kafelka.

- ID: TEMP-023  
  Tytuł: Częściowy wynik podglądu kafelka  
  Opis: Jako użytkownik chcę zobaczyć dane z działających źródeł, nawet gdy część źródeł lub pól zawiedzie, aby łatwo
  zlokalizować problem.  
  Kryteria akceptacji:
    - Gdy jedno ze źródeł jest niedostępne, pola z tego źródła mają błąd source_unavailable, a pozostałe pola mają
      wartości.
    - Pole, dla którego ścieżka nie zwraca wartości, ma błąd path_not_found.
    - Pole, którego łańcuch transformacji zawiódł, ma błąd transformation_failed z kluczem transformacji.
    - Podgląd z błędami pól zwraca HTTP 200.

- ID: TEMP-024  
  Tytuł: Dodanie widoku z siatką kafelków  
  Opis: Jako użytkownik chcę ułożyć kafelki w siatce widoku, aby zapisać docelowy układ prezentacji danych.  
  Kryteria akceptacji:
    - Można zapisać widok z nazwą i listą pozycji kafelków (kafelek, wiersz, kolumna, szerokość, wysokość).
    - Widok bez kafelków jest dozwolony.
    - Odpowiedź zawiera identyfikator nowego widoku.

- ID: TEMP-025  
  Tytuł: Walidacja siatki widoku  
  Opis: Jako użytkownik chcę, aby układ widoku był spójny, aby frontend mógł go wyrenderować bez konfliktów.  
  Kryteria akceptacji:
    - Brak nazwy lub nazwa dłuższa niż 100 znaków zwraca błąd walidacji.
    - Wiersz, kolumna, szerokość lub wysokość mniejsze niż 1 zwracają błąd walidacji.
    - Kafelek wychodzący poza 12 kolumn (kolumna + szerokość - 1 > 12) zwraca błąd walidacji.
    - Nachodzące na siebie kafelki zwracają błąd walidacji wskazujący kolidujące kafelki.
    - Ten sam kafelek użyty w widoku więcej niż raz zwraca błąd walidacji.
    - Odwołanie do nieistniejącego kafelka zwraca błąd walidacji.
    - Przy błędzie walidacji nic nie zostaje zapisane.

- ID: TEMP-026  
  Tytuł: Przeglądanie widoków  
  Opis: Jako użytkownik chcę zobaczyć listę i szczegóły widoków, aby wiedzieć, jakie układy mam skonfigurowane.  
  Kryteria akceptacji:
    - Lista zwraca identyfikator, nazwę i liczbę kafelków każdego widoku.
    - Szczegóły zwracają nazwę i siatkę z identyfikatorem i nazwą każdego kafelka.
    - Szczegóły nieistniejącego widoku zwracają 404.

- ID: TEMP-027  
  Tytuł: Edycja widoku  
  Opis: Jako użytkownik chcę zmienić nazwę i układ widoku, aby dopasować go do potrzeb.  
  Kryteria akceptacji:
    - Edycja zastępuje nazwę i całą siatkę.
    - Edycja podlega tym samym regułom walidacji co dodanie.
    - Edycja nieistniejącego widoku zwraca 404.

- ID: TEMP-028  
  Tytuł: Usunięcie widoku  
  Opis: Jako użytkownik chcę usunąć niepotrzebny widok bez utraty kafelków, aby móc użyć ich w innych widokach.  
  Kryteria akceptacji:
    - Widok zostaje usunięty.
    - Kafelki i połączenia użyte w widoku pozostają niezmienione.
    - Usunięcie nieistniejącego widoku zwraca 404.

## 6. Metryki sukcesu

- Użytkownik konfiguruje połączenie, kafelek z gotowymi transformacjami i widok bez zmian w kodzie aplikacji.
- Podgląd kafelka korzystającego z szybkich źródeł zwraca wynik w czasie poniżej 2 s.
- Awaria jednego źródła nie blokuje podglądu pól z pozostałych źródeł (100% podglądów z częściowym wynikiem zamiast
  błędu serwera).
- Konfiguracja odtwarza się w całości po restarcie aplikacji.
- 100% prób usunięcia używanych połączeń jest blokowanych z listą zależnych kafelków.
