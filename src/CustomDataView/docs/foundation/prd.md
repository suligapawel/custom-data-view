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
- Połączenie jest szablonem żądania: nazwa (wymagana, 1-100 znaków, bez wymogu unikalności), szablon URL (http lub
  https) z parametrami ścieżki `{name}`, rodzaj GET lub POST, deklaracje parametrów ścieżki i query oraz dla POST szablon
  body JSON z deklaracjami parametrów body.
- Połączenie nie definiuje nagłówków: wszystkie połączenia wysyłają te same nagłówki predefiniowane w aplikacji (treść
  POST zawsze jako `Content-Type: application/json`).
- Deklaracja parametru zawiera nazwę, informację, czy jest wymagany, oraz opcjonalną wartość domyślną. Wartości
  parametrów są przekazywane w czasie wykonania i podmieniane w szablonie.
- Brak obsługi uwierzytelniania do zewnętrznych API.
- Usunięcie połączenia używanego przez pole dowolnego kafelka jest blokowane.

3.3. Definicje kafelków

- Dodawanie, odczyt listy, odczyt szczegółów, edycja i usuwanie definicji kafelka.
- Kafelek zawiera nazwę oraz uporządkowaną listę pól (co najmniej jedno).
- Pole kafelka wiąże dane ze źródłem: odwołanie do połączenia, ścieżka JSONPath, etykieta oraz uporządkowana lista
  transformacji z katalogu wraz z wartościami ich parametrów (może być pusta). Pola jednego kafelka mogą korzystać z
  różnych połączeń.
- Ścieżka JSONPath obsługuje podzbiór składni: `$`, `.name`, `['name']`, `[n]` (także ujemny), `[*]` i `.*`. Ścieżka
  bez wildcardu wskazuje jedną wartość; ścieżka z wildcardem zwraca płaską tablicę dopasowań, a każdy jej element
  przechodzi przez łańcuch transformacji pola.
- Usunięcie kafelka usuwa go ze wszystkich widoków; połączenia pozostają.

3.4. Definicje widoków

- Dodawanie, odczyt listy, odczyt szczegółów, edycja i usuwanie definicji widoku.
- Widok zawiera nazwę oraz siatkę: dla każdego kafelka wiersz, kolumnę, szerokość i wysokość.
- Walidacja siatki: wszystkie wartości >= 1, kafelek mieści się w 12 kolumnach, kafelki nie nachodzą na siebie, dany
  kafelek występuje w widoku najwyżej raz.

3.5. Test i podgląd

- Test połączenia wykonuje żądanie zgodnie z definicją (zapisaną po id lub niezapisaną przekazaną w żądaniu) i
  wartościami parametrów przekazanymi w żądaniu testu. Sukces
  zwraca status HTTP odpowiedzi, czas odpowiedzi oraz pełną treść JSON (bez obcinania); niepowodzenie zwraca odpowiedź
  błędu z kodem (source_unavailable, unexpected_status, invalid_json, response_too_large).
- Podgląd kafelka zwraca JSON kafelka wygenerowany zgodnie z definicją (zapisaną lub niezapisaną): dla każdego pola
  etykietę i przetransformowaną wartość albo kod błędu. Wartości parametrów połączeń są przekazywane w żądaniu podglądu
  (osobno dla każdego połączenia); kafelek ich nie przechowuje.
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

- ID: TEMP-024  
  Tytuł: Dodanie widoku z siatką kafelków  
  Opis: Jako użytkownik chcę ułożyć kafelki w siatce widoku, aby zapisać docelowy układ prezentacji danych.  
  Kryteria akceptacji:
    - Można zapisać widok z nazwą i listą pozycji kafelków (kafelek, wiersz, kolumna, szerokość, wysokość).
    - Widok bez kafelków jest dozwolony.
    - Edycja kafelka zachowuje jego pozycję w siatce widoków, które go zawierają.
    - Usunięcie kafelka usuwa go z siatki wszystkich widoków, które go zawierały; same widoki pozostają.
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
