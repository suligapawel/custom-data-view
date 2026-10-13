# Podsumowanie analizy modułu - CustomDataView

## Moduł

- Nazwa: CustomDataView
- Typ: Simple CRUD (projekty `CustomDataView.Api` i `CustomDataView.Core`)
- Źródło: `docs/foundation/analysis/idea.md`

## Problem

Moduł odpowiada za definiowanie połączeń do zewnętrznych REST API, kafelków i widoków użytkownika oraz za
udostępnienie katalogu gotowych transformacji pól. Celem modułu nie jest docelowe pobieranie danych - tym zajmą się inne moduły, a frontend będzie je
renderował. Moduł pozwala jednak sprawdzić, czy połączenie zostało poprawnie skonfigurowane i czy dane pobierają się
zgodnie z definicją.

## Kluczowe funkcjonalności

1. Definicje połączeń REST: URL, metoda GET lub POST (z body JSON), parametry query, nagłówki. Bez uwierzytelniania.
2. Katalog predefiniowanych transformacji pól (tylko do odczytu), składanych w łańcuch dekoratorów.
3. Definicje kafelków: nazwa i lista pól; pole = połączenie + JSONPath + etykieta + uporządkowana lista transformacji z
   parametrami; kafelek może korzystać z wielu źródeł.
4. Definicje widoków: nazwa i siatka (pozycja i rozmiar każdego kafelka).
5. Test połączenia/definicji: zwraca JSON wygenerowany zgodnie z definicją; działa na zapisanej definicji (po id) oraz na
   niezapisanej (draft w body); wynik nie jest nigdzie zapisywany.
6. Podgląd pojedynczego kafelka: JSON kafelka z częściowym wynikiem i błędem per pole.

## Poza zakresem

- Konta użytkowników, uwierzytelnianie użytkowników, przypisywanie użytkowników/GUID do konfiguracji.
- Uwierzytelnianie do zewnętrznych API (klucze, tokeny, Basic auth).
- Docelowe pobieranie danych (inne moduły) i frontend renderujący widoki.
- Podgląd całego widoku.
- Źródła inne niż REST (SQL, GraphQL, pliki).
- Harmonogramy, cache, zapisywanie wyników testów i podglądów.
- Definiowanie, edycja i usuwanie transformacji przez użytkownika; test transformacji na przykładowej wartości.
- Operacje arytmetyczne i agregaty na tablicach w transformacjach.
- Edytor wizualny drag & drop.
- Blokada adresów wewnętrznych (ochrona SSRF poza limitami).

## Kluczowe decyzje

### Właściciel konfiguracji i kolejność realizacji

- Decyzja zmieniona po podsumowaniu: w MVP nie ma właściciela konfiguracji ani GUID; wszystkie definicje są wspólne.
- W przyszłości użytkownicy lub grupy użytkowników będą przypisywani wyłącznie do pełnych widoków, nie do kafelków,
  transformacji ani połączeń.
- Historyjki są uporządkowane od najmniejszych funkcjonalności: katalog transformacji pól, połączenia, kafelki, widoki.
- Blokada usuwania połączeń używanych przez kafelki jest realizowana w grupie kafelków.

### Utrwalanie

- Konfiguracja jest przechowywana w PostgreSQL.
- Schemat zostanie zaprojektowany osobnym skillem (`database-create-plan-single-module`) w `db-plan.md` przed
  implementacją.

### Połączenia

- Metody: GET oraz POST z body JSON; parametry query i dowolne nagłówki.
- Brak uwierzytelniania do zewnętrznych API w MVP.
- Ochrona: globalny timeout i limit rozmiaru odpowiedzi; brak blokady adresów wewnętrznych (świadome ryzyko SSRF w MVP).

### Transformacje pól

- Decyzja zmieniona po podsumowaniu: transformacje są predefiniowane w kodzie; użytkownik nie może ich tworzyć, edytować
  ani usuwać.
- Każda transformacja implementuje wspólny interfejs i może dekorować inne transformacje (wzorzec Decorator).
- Transformacja ma klucz, nazwę, opis i opcjonalne parametry; wartości parametrów podaje pole kafelka.
- Pole kafelka ma uporządkowaną listę transformacji (0..n), z której backend buduje łańcuch dekoratorów; pusta lista
  zwraca wartość bez zmian.
- Katalog MVP: `round(decimals)`, `date_format(pattern)`, `prefix(text)`, `suffix(text)`, `uppercase`, `lowercase`,
  `truncate(max_length)`.
- Użytkownik może jedynie odczytać katalog (klucz, nazwa, opis, parametry); brak endpointu testu transformacji - efekt
  widać w podglądzie kafelka.
- Brak operacji arytmetycznych i agregatów.

### Kafelki

- Kafelek ma nazwę i uporządkowaną listę pól (co najmniej jedno).
- Pole kafelka wiąże dane ze źródłem: połączenie, ścieżka JSONPath, etykieta oraz lista transformacji z parametrami.
- Walidacja pola: klucz transformacji musi istnieć w katalogu, parametry muszą być kompletne i poprawne.
- Pola mogą korzystać z różnych połączeń.
- Usunięcie kafelka usuwa go ze wszystkich widoków; połączenia pozostają.

### Widoki

- Widok ma nazwę i siatkę: pozycja (wiersz, kolumna) i rozmiar (szerokość, wysokość) każdego kafelka.
- Walidacja siatki: wartości >= 1, maksymalnie 12 kolumn, kafelki nie mogą na siebie nachodzić, kafelek najwyżej raz w
  widoku.

### Usuwanie

- Usunięcie połączenia używanego przez pole dowolnego kafelka jest blokowane (konflikt z listą kafelków).
- Usunięcie kafelka usuwa go z widoków.

### Walidacja

- Nazwy wymagane (1-100 znaków), bez wymogu unikalności.

### Test i podgląd

- Test połączenia to jedna funkcjonalność dla połączenia zapisanego (po id) i niezapisanego (definicja w żądaniu): sukces
  zwraca status, czas i obciętą treść JSON, a niepowodzenie odpowiedź błędu z kodem (`source_unavailable`,
  `unexpected_status`, `invalid_json`, `response_too_large`); wynik nie jest zapisywany.
- Podgląd dostępny wyłącznie dla pojedynczego kafelka.
- Błąd źródła, brak ścieżki lub błąd transformacji nie przerywa podglądu: zwracany jest częściowy wynik z błędem per pole
  (`source_unavailable`, `path_not_found`, `transformation_failed`) i status HTTP 200.

### Bezpieczeństwo i logowanie

- Nie logujemy nagłówków, body żądań ani pełnych odpowiedzi zewnętrznych API.
