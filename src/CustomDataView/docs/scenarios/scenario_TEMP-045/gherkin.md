## Feature

Transformacja mapper: zamiana wartości pola na wartość ze słownika z kafelka

### Background

Given użytkownik konfiguruje kafelki dla widoków danych
And   istnieje połączenie "Statusy", które odpowiada danymi
      [{"key": "NEW", "translation": "Nowe", "weight": 1}, {"key": "DONE", "translation": "Zrealizowane", "weight": 3}]
And   kafelek "Statusy zamówień" typu table ma elementy ze źródła "Statusy" pod "$[*]" z kluczem "Klucz"
      oraz pola elementu "Klucz" ($.key), "Tłumaczenie" ($.translation) i "Waga" ($.weight)
And   istnieje połączenie "Zamówienia", które odpowiada danymi
      {"status": "NEW", "statuses": ["NEW", "DONE"], "level": 6, "note": null}
And   kafelek "Zamówienie" typu details ma źródło "Zamówienia"

---

### Role: Użytkownik konfigurujący widoki danych

#### Scenario #1

Katalog transformacji opisuje mapper

```
When  użytkownik odczytuje katalog transformacji
Then  katalog zawiera transformację "mapper" z nazwą i opisem
And   "mapper" ma parametr "dictionary" typu tekst
```

#### Scenario #2

Mapper zamienia kod na wartość ze słownika bez pytania źródła słownika

```
Given kafelek "Zamówienie" ma pole "Status" ($.status) zamieniane słownikiem "Statusy zamówień" na pole "Tłumaczenie"
And   aplikacja została uruchomiona i załadowała słowniki
When  użytkownik ogląda podgląd kafelka "Zamówienie"
Then  pole "Status" ma wartość "Nowe"
And   połączenie "Statusy" nie zostaje odpytane podczas podglądu
```

#### Scenario #3

Wartość ze słownika zachowuje typ liczby

```
Given istnieje połączenie "Poziomy", które odpowiada danymi [{"from": 6, "to": 8}]
And   kafelek "Mapa poziomów" ma elementy ze źródła "Poziomy" z kluczem "Z" ($.from) i polem "Na" ($.to)
And   kafelek "Zamówienie" ma pole "Poziom" ($.level) zamieniane słownikiem "Mapa poziomów" na pole "Na",
      a potem z dopiskiem " pkt"
And   aplikacja została uruchomiona i załadowała słowniki
When  użytkownik ogląda podgląd kafelka "Zamówienie"
Then  pole "Poziom" ma wartość "8 pkt"
```

#### Scenario #4

Liczba w polu trafia w klucz słownika zapisany jako tekst

```
Given istnieje połączenie "Poziomy", które odpowiada danymi [{"from": "6", "to": "wysoki"}]
And   kafelek "Mapa poziomów" ma elementy ze źródła "Poziomy" z kluczem "Z" ($.from) i polem "Na" ($.to)
And   kafelek "Zamówienie" ma pole "Poziom" ($.level) zamieniane słownikiem "Mapa poziomów" na pole "Na"
And   aplikacja została uruchomiona i załadowała słowniki
When  użytkownik ogląda podgląd kafelka "Zamówienie"
Then  pole "Poziom" ma wartość "wysoki"
```

#### Scenario #5

Słownik zawiera wartości po transformacjach, także pola złożonego

```
Given pole "Tłumaczenie" kafelka "Statusy zamówień" ma transformację na wielkie litery
And   kafelek "Statusy zamówień" ma pole "Opis" ze wzorca "{Tłumaczenie} ({Waga})"
And   kafelek "Zamówienie" ma pole "Status" ($.status) zamieniane słownikiem "Statusy zamówień" na pole "Tłumaczenie"
And   kafelek "Zamówienie" ma pole "Opis statusu" ($.status) zamieniane słownikiem "Statusy zamówień" na pole "Opis"
And   aplikacja została uruchomiona i załadowała słowniki
When  użytkownik ogląda podgląd kafelka "Zamówienie"
Then  pole "Status" ma wartość "NOWE"
And   pole "Opis statusu" ma wartość "NOWE (1)"
```

#### Scenario #6

Lista wartości jest zamieniana element po elemencie

```
Given kafelek "Zamówienie" ma pole "Historia" ($.statuses[*]) zamieniane słownikiem "Statusy zamówień"
      na pole "Tłumaczenie"
And   aplikacja została uruchomiona i załadowała słowniki
When  użytkownik ogląda podgląd kafelka "Zamówienie"
Then  pole "Historia" ma wartość ["Nowe", "Zrealizowane"]
```

#### Scenario #7

Wartość spoza słownika daje błąd pola, który blokuje zależne dziecko

```
Given połączenie "Zamówienia" zwraca status "LOST"
And   kafelek "Zamówienie" ma pole "Status" ($.status) zamieniane słownikiem "Statusy zamówień" na pole "Tłumaczenie"
And   kafelek "Zamówienie" ma pole "Poziom" ($.level)
And   kafelek "Zamówienie" osadza kafelek "Opis statusu" jako "info", któremu przekazuje pole "Status" jako parametr
And   aplikacja została uruchomiona i załadowała słowniki
When  użytkownik ogląda podgląd kafelka "Zamówienie"
Then  pole "Status" ma błąd transformacji "mapper" z powodem "brak klucza w słowniku"
And   dziecko "info" ma błąd zależności od pola "Status"
And   pole "Poziom" ma wartość 6
```

#### Scenario #8

Brak wartości na wejściu nie jest błędem

```
Given kafelek "Zamówienie" ma pole "Notatka" ($.note) zamieniane słownikiem "Statusy zamówień" na pole "Tłumaczenie"
And   aplikacja została uruchomiona i załadowała słowniki
When  użytkownik ogląda podgląd kafelka "Zamówienie"
Then  pole "Notatka" nie ma wartości ani błędu
```

#### Scenario #9

Słownik niedostępny przy starcie wraca po ponownym zapisie

```
Given kafelek "Zamówienie" ma pole "Status" ($.status) zamieniane słownikiem "Statusy zamówień" na pole "Tłumaczenie"
And   kafelek "Zamówienie" ma pole "Poziom" ($.level) zamieniane słownikiem "Mapa poziomów" na pole "Na"
And   połączenie "Statusy" nie odpowiadało, gdy aplikacja ładowała słowniki po uruchomieniu
When  użytkownik ogląda podgląd kafelka "Zamówienie"
Then  pole "Status" ma błąd transformacji "mapper" z powodem "słownik niedostępny"
And   pole "Poziom" ma wartość ze słownika "Mapa poziomów"
When  połączenie "Statusy" znów odpowiada
And   użytkownik ponownie zapisuje kafelek "Statusy zamówień" i słownik zostaje załadowany
And   użytkownik ogląda podgląd kafelka "Zamówienie"
Then  pole "Status" ma wartość "Nowe"
```

#### Scenario #10

Błędne dane słownika powodują, że słownik się nie ładuje

```
Scenario Outline: niepoprawne dane słownika
Given połączenie "Statusy" odpowiada danymi <dane>
And   kafelek "Zamówienie" ma pole "Status" ($.status) zamieniane słownikiem "Statusy zamówień" na pole "Tłumaczenie"
And   aplikacja została uruchomiona i spróbowała załadować słowniki
When  użytkownik ogląda podgląd kafelka "Zamówienie"
Then  pole "Status" ma <wynik>

Examples:
  | dane                                                                                  | wynik                                             |
  | [{"key": "NEW", "translation": "Nowe"}, {"key": "NEW", "translation": "Inne"}]        | błąd transformacji z powodem "słownik niedostępny" |
  | [{"key": "NEW", "translation": "Nowe"}, {"translation": "Bez klucza"}]                | błąd transformacji z powodem "słownik niedostępny" |
  | [{"key": "NEW"}]                                                                      | błąd transformacji z powodem "słownik niedostępny" |
  | [{"key": null, "translation": "Pusty"}, {"key": null, "translation": "Inny"},
     {"key": "NEW", "translation": "Nowe"}]                                                | wartość "Nowe"                                    |
```

#### Scenario #11

Nowe odwołanie do słownika ładuje słownik w tle

```
Given aplikacja została uruchomiona, gdy żaden kafelek nie używał słownika "Statusy zamówień"
When  użytkownik zapisuje kafelek "Zamówienie" z polem "Status" ($.status) zamienianym słownikiem
      "Statusy zamówień" na pole "Tłumaczenie"
Then  zapis kończy się bez czekania na połączenie "Statusy"
When  słownik "Statusy zamówień" zostaje załadowany
And   użytkownik ogląda podgląd kafelka "Zamówienie"
Then  pole "Status" ma wartość "Nowe"
```

#### Scenario #12

Zmiana kafelka-słownika przeładowuje słownik, a do tego czasu działa poprzednia wersja

```
Given kafelek "Zamówienie" ma pole "Status" ($.status) zamieniane słownikiem "Statusy zamówień" na pole "Tłumaczenie"
And   aplikacja została uruchomiona i załadowała słowniki
When  użytkownik dodaje do pola "Tłumaczenie" kafelka "Statusy zamówień" transformację na wielkie litery i zapisuje go
Then  dopóki słownik się przeładowuje, pole "Status" kafelka "Zamówienie" ma wartość "Nowe"
And   po przeładowaniu słownika pole "Status" ma wartość "NOWE"
When  połączenie "Statusy" przestaje odpowiadać
And   użytkownik ponownie zapisuje kafelek "Statusy zamówień" i przeładowanie słownika się nie udaje
Then  pole "Status" kafelka "Zamówienie" ma błąd transformacji "mapper" z powodem "słownik niedostępny"
```

#### Scenario #13

Ładowanie słownika pobiera tylko elementy słownika

```
Given połączenie "Statusy" deklaruje opcjonalny parametr "lang" z wartością domyślną "pl"
And   kafelek "Statusy zamówień" ma drugie źródło z połączenia "Kategorie" i osadza kafelek "Szczegóły statusu"
      z własnym połączeniem
And   kafelek "Zamówienie" ma pole "Status" ($.status) zamieniane słownikiem "Statusy zamówień" na pole "Tłumaczenie"
When  aplikacja zostaje uruchomiona i ładuje słowniki
Then  połączenie "Statusy" zostaje odpytane raz, z parametrem "lang" równym "pl"
And   połączenie "Kategorie" i połączenie kafelka "Szczegóły statusu" nie zostają odpytane
```

#### Scenario #14

Niepoprawne odwołanie do słownika jest odrzucane przy zapisie

```
Scenario Outline: odrzucenie odwołania do słownika
Given kafelek "Zamówienie" ma pole "Status" ($.status) zamieniane <odwołanie>
When  użytkownik zapisuje kafelek "Zamówienie"
Then  zapis zostaje odrzucony z błędem walidacji parametru transformacji "mapper" pola "Status"

Examples:
  | odwołanie                                                                                        |
  | słownikiem opisanym tekstem "Statusy zamówień" zamiast identyfikatora kafelka i pola              |
  | słownikiem z nieistniejącego kafelka                                                             |
  | słownikiem z samego kafelka "Zamówienie"                                                         |
  | słownikiem z kafelka "Zamówienie szczegóły" typu details bez elementów                           |
  | słownikiem "Statusy zamówień" na pole "Kolor", którego kafelek nie ma                            |
  | słownikiem "Statusy zamówień", którego połączenie wymaga parametru "lang" bez wartości domyślnej |
  | słownikiem "Statusy zamówień", którego kafelek wymaga wejścia danych                             |
  | słownikiem "Statusy zamówień" ze stronicowaniem                                                  |
  | słownikiem "Statusy zamówień", którego pole "Tłumaczenie" samo używa słownika                    |
```

#### Scenario #15

Zmiana kafelka-słownika, która psuje odwołania, jest blokowana

```
Scenario Outline: blokada edycji używanego słownika
Given kafelek "Zamówienie" ma pole "Status" ($.status) zamieniane słownikiem "Statusy zamówień" na pole "Tłumaczenie"
When  użytkownik <zmiana> i zapisuje kafelek "Statusy zamówień"
Then  edycja zostaje zablokowana z listą pól "Tłumaczenie" i kafelków "Zamówienie"

Examples:
  | zmiana                                                          |
  | usuwa pole "Tłumaczenie"                                        |
  | zmienia etykietę pola "Tłumaczenie" na "Nazwa"                  |
  | zmienia klucz elementów na pole "Waga"                          |
  | zmienia typ kafelka na details bez elementów                    |
  | dodaje stronicowanie                                            |
  | dodaje źródło z połączenia wymagającego parametru "lang"        |
  | dodaje źródło będące wejściem danych                            |
  | dodaje do pola "Waga" zamianę słownikiem z innego kafelka       |
```

```
Given kafelek "Zamówienie" ma pole "Status" ($.status) zamieniane słownikiem "Statusy zamówień" na pole "Tłumaczenie"
When  użytkownik zmienia etykietę pola "Waga" na "Kolejność" i zapisuje kafelek "Statusy zamówień"
Then  zmiana zostaje zapisana
```

#### Scenario #16

Używanego słownika nie można usunąć

```
Given kafelek "Zamówienie" ma pole "Status" ($.status) zamieniane słownikiem "Statusy zamówień" na pole "Tłumaczenie"
When  użytkownik usuwa kafelek "Statusy zamówień"
Then  usunięcie zostaje zablokowane z listą kafelków "Zamówienie"
```

#### Scenario #17

Kafelek-słownik można wyświetlić jak każdy kafelek

```
Given kafelek "Zamówienie" ma pole "Status" ($.status) zamieniane słownikiem "Statusy zamówień" na pole "Tłumaczenie"
When  użytkownik ogląda podgląd kafelka "Statusy zamówień"
Then  połączenie "Statusy" zostaje odpytane
And   wiersz 1 ma pola "Klucz" = "NEW" i "Tłumaczenie" = "Nowe", a wiersz 2 pola "Klucz" = "DONE"
      i "Tłumaczenie" = "Zrealizowane"
```

#### Scenario #18

Podgląd niezapisanego kafelka korzysta tylko z załadowanych słowników

```
Given aplikacja została uruchomiona i załadowała słownik "Statusy zamówień" na pole "Tłumaczenie"
And   użytkownik nie zapisał jeszcze kafelka "Zamówienie" z polami "Status" ($.status) zamienianym słownikiem
      "Statusy zamówień" na pole "Tłumaczenie" i "Waga statusu" ($.status) zamienianym tym słownikiem na pole "Waga"
When  użytkownik ogląda podgląd niezapisanej definicji kafelka "Zamówienie"
Then  pole "Status" ma wartość "Nowe"
And   pole "Waga statusu" ma błąd transformacji "mapper" z powodem "słownik niedostępny"
And   słownik "Statusy zamówień" na pole "Waga" nie zostaje załadowany
```

```
Given użytkownik nie zapisał jeszcze kafelka "Zamówienie" z polem "Status" ($.status) zamienianym słownikiem
      opisanym tekstem "Statusy zamówień" zamiast identyfikatora kafelka i pola
When  użytkownik ogląda podgląd niezapisanej definicji kafelka "Zamówienie"
Then  podgląd zostaje odrzucony z błędem walidacji parametru transformacji "mapper" pola "Status"
```

---

### General

#### Scenario #19

Uruchomienie aplikacji nie czeka na słowniki

```
Given kafelek "Zamówienie" ma pole "Status" ($.status) zamieniane słownikiem "Statusy zamówień" na pole "Tłumaczenie"
And   połączenie "Statusy" nie odpowiada
When  aplikacja zostaje uruchomiona
Then  aplikacja przyjmuje żądania, zanim słowniki zostaną załadowane
And   użytkownik może oglądać podgląd kafelków bez słowników
```
