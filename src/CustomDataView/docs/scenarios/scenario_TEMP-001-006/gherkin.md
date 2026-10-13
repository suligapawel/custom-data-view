## Feature

Katalog transformacji pól i składanie transformacji w łańcuch

### Background

Given katalog zawiera predefiniowane transformacje: round, date_format, prefix, suffix, uppercase, lowercase, truncate

---

### Role: Użytkownik konfigurujący widoki danych

#### Scenario #1

Użytkownik przegląda katalog transformacji

```
Given użytkownik konfiguruje pola kafelka
When  użytkownik przegląda katalog transformacji
Then  widzi 7 transformacji w kolejności alfabetycznej klucza: date_format, lowercase, prefix, round, suffix, truncate, uppercase
And   każda transformacja ma klucz, polską nazwę, polski opis i listę parametrów
And   każdy parametr ma wyłącznie nazwę i typ
```

#### Scenario #2

Katalog opisuje parametry każdej transformacji

```
Given użytkownik przegląda katalog transformacji
When  sprawdza parametry transformacji
Then  round ma parametr "decimals" typu liczba całkowita
And   date_format ma parametr "pattern" typu tekst
And   prefix oraz suffix mają parametr "text" typu tekst
And   truncate ma parametr "max_length" typu liczba całkowita
And   uppercase oraz lowercase nie mają parametrów
```

#### Scenario #3

Katalog jest tylko do odczytu

```
Given użytkownik przegląda katalog transformacji
When  użytkownik próbuje dodać, zmienić lub usunąć transformację
Then  operacja nie jest dostępna
And   katalog nadal zawiera te same 7 transformacji
```

---

### General

#### Scenario #4

Zaokrąglenie liczby do wybranej liczby miejsc po przecinku

```
Scenario Outline: zaokrąglenie wartości
Given pole ma transformację round z liczbą miejsc po przecinku <decimals>
When  przekształcana jest wartość <value>
Then  wynikiem jest liczba <result>

Examples:
  | value       | decimals | result |
  | 21.4567     | 2        | 21.46  |
  | 21.4567     | 0        | 21     |
  | 2.5         | 0        | 3      |
  | -2.5        | 0        | -3     |
  | "21,4567"   | 2        | 21.46  |
  | " 21.4567 " | 2        | 21.46  |
```

#### Scenario #5

Zaokrąglenie wartości, która nie jest liczbą

```
Scenario Outline: wartość nieliczbowa
Given pole ma transformację round z liczbą miejsc po przecinku 2
When  przekształcana jest wartość <value>
Then  przekształcenie kończy się błędem transformation_failed dla transformacji round
And   powodem błędu jest niepoprawna wartość

Examples:
  | value     |
  | "abc"     |
  | "1,234.5" |
  | true      |
```

#### Scenario #6

Zaokrąglenie z brakującą lub niepoprawną liczbą miejsc po przecinku

```
Scenario Outline: błędna konfiguracja zaokrąglenia
Given pole ma transformację round z konfiguracją <configuration>
When  przekształcana jest wartość 21.4567
Then  przekształcenie kończy się błędem transformation_failed dla transformacji round
And   powodem błędu jest niepoprawny parametr "decimals"

Examples:
  | configuration               |
  | bez liczby miejsc           |
  | liczba miejsc równa -1      |
  | liczba miejsc równa 11      |
```

#### Scenario #7

Formatowanie daty według wzorca

```
Scenario Outline: formatowanie daty
Given pole ma transformację date_format ze wzorcem <pattern>
When  przekształcana jest wartość <value>
Then  wynikiem jest tekst <result>
And   nazwy miesięcy i dni są zawsze w kulturze niezmiennej, niezależnie od ustawień regionalnych serwera

Examples:
  | pattern            | value                       | result             |
  | "yyyy-MM-dd HH:mm" | "2026-10-13T10:00:00+02:00" | "2026-10-13 10:00" |
  | "yyyy-MM-dd HH:mm" | "2026-10-13"                | "2026-10-13 00:00" |
  | "d MMMM yyyy"      | "2026-10-13"                | "13 October 2026"  |
```

#### Scenario #8

Formatowanie wartości, która nie jest datą, lub z błędnym wzorcem

```
Scenario Outline: błąd formatowania daty
Given pole ma transformację date_format z konfiguracją <configuration>
When  przekształcana jest wartość <value>
Then  przekształcenie kończy się błędem transformation_failed dla transformacji date_format
And   powodem błędu jest <reason>

Examples:
  | configuration                 | value        | reason                         |
  | wzorzec "yyyy-MM-dd"          | "jutro"      | niepoprawna wartość            |
  | wzorzec "yyyy-MM-dd"          | 1760342400   | niepoprawna wartość            |
  | pusty wzorzec                 | "2026-10-13" | niepoprawny parametr "pattern" |
  | bez wzorca                    | "2026-10-13" | niepoprawny parametr "pattern" |
```

#### Scenario #9

Doklejanie tekstu przed i po wartości

```
Scenario Outline: prefiks i sufiks
Given pole ma transformację <transformation> z tekstem <text>
When  przekształcana jest wartość <value>
Then  wynikiem jest tekst <result>

Examples:
  | transformation | text    | value | result    |
  | suffix         | " °C"   | 21.5  | "21.5 °C" |
  | prefix         | "ID: "  | 7     | "ID: 7"   |
  | prefix         | ""      | "abc" | "abc"     |
```

#### Scenario #10

Prefiks lub sufiks bez tekstu do doklejenia

```
Scenario Outline: brak tekstu
Given pole ma transformację <transformation> bez tekstu do doklejenia
When  przekształcana jest wartość "abc"
Then  przekształcenie kończy się błędem transformation_failed dla transformacji <transformation>
And   powodem błędu jest niepoprawny parametr "text"

Examples:
  | transformation |
  | prefix         |
  | suffix         |
```

#### Scenario #11

Zmiana wielkości liter i przycinanie tekstu

```
Scenario Outline: transformacje tekstowe
Given pole ma transformację <transformation>
When  przekształcana jest wartość <value>
Then  wynikiem jest tekst <result>
And   zmiana wielkości liter jest zawsze w kulturze niezmiennej, niezależnie od ustawień regionalnych serwera

Examples:
  | transformation | value      | result     |
  | uppercase      | "abc"      | "ABC"      |
  | uppercase      | "istanbul" | "ISTANBUL" |
  | lowercase      | "ABC"      | "abc"      |
  | lowercase      | "TITLE"    | "title"    |
  | truncate(3)    | "abcdef"   | "abc"      |
  | truncate(3)    | "ab"       | "ab"       |
```

#### Scenario #12

Przycinanie z brakującą lub niepoprawną maksymalną długością

```
Scenario Outline: błędna konfiguracja przycinania
Given pole ma transformację truncate z konfiguracją <configuration>
When  przekształcana jest wartość "abcdef"
Then  przekształcenie kończy się błędem transformation_failed dla transformacji truncate
And   powodem błędu jest niepoprawny parametr "max_length"

Examples:
  | configuration                  |
  | bez maksymalnej długości       |
  | maksymalna długość równa 0     |
```

#### Scenario #13

Liczba jest zamieniana na tekst w jednolity sposób przed transformacją tekstową

```
Scenario Outline: liczba w transformacji tekstowej
Given pole ma transformację <transformation>
When  przekształcana jest liczba <value>
Then  liczba jest najpierw zapisana jako tekst w kulturze niezmiennej, bez zbędnych zer
And   wynikiem jest tekst <result>

Examples:
  | transformation | value | result    |
  | suffix(" kg")  | 21.50 | "21.5 kg" |
  | prefix("#")    | 1e3   | "#1000"   |
  | uppercase      | 21.50 | "21.5"    |
  | lowercase      | 0.1   | "0.1"     |
  | truncate(3)    | 21.50 | "21."     |
```

#### Scenario #14

Brak wartości i wartości złożone

```
Scenario Outline: wartość pusta lub złożona
Given pole ma transformację <transformation>
When  przekształcana jest <value>
Then  <outcome>

Examples:
  | transformation | value                 | outcome                                                                        |
  | uppercase      | brak wartości         | wynikiem jest brak wartości                                                    |
  | round(2)       | brak wartości         | wynikiem jest brak wartości                                                    |
  | uppercase      | obiekt z danymi       | przekształcenie kończy się błędem transformation_failed z niepoprawną wartością |
  | truncate(3)    | lista wartości        | przekształcenie kończy się błędem transformation_failed z niepoprawną wartością |
```

#### Scenario #15

Transformacje są stosowane po kolei w kolejności listy

```
Given pole ma listę transformacji: round(1), następnie suffix(" °C")
When  przekształcana jest wartość 21.46
Then  wynikiem jest tekst "21.5 °C"
```

#### Scenario #16

Pusta lista transformacji

```
Given pole nie ma żadnych transformacji
When  przekształcana jest wartość 21.4567
Then  wynikiem jest niezmieniona wartość 21.4567
```

#### Scenario #17

Błąd jednej transformacji przerywa łańcuch

```
Given pole ma listę transformacji: uppercase, następnie round(1), następnie suffix(" °C")
When  przekształcana jest wartość "abc"
Then  przekształcenie kończy się błędem transformation_failed dla transformacji round
And   transformacja suffix nie zostaje zastosowana
```

#### Scenario #18

Transformacja spoza katalogu w łańcuchu

```
Given pole ma listę transformacji: uppercase, następnie "reverse", której nie ma w katalogu
When  przekształcana jest wartość "abc"
Then  przekształcenie kończy się błędem transformation_failed dla transformacji "reverse"
And   powodem błędu jest nieznana transformacja
```

#### Scenario #19

Ta sama transformacja może wystąpić w łańcuchu kilka razy

```
Given pole ma listę transformacji: prefix("a"), następnie prefix("b")
When  przekształcana jest wartość "x"
Then  wynikiem jest tekst "bax"
```
