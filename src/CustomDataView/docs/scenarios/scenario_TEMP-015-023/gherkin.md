## Feature

Kafelki grupujące wybrane wartości z odpowiedzi połączeń, przekształcone łańcuchem transformacji

### Background

Given użytkownik konfiguruje kafelki dla widoków danych
And   istnieje połączenie "Klient" z wymaganym parametrem adresu "customerId"
And   dla klienta "42" źródło "Klient" odpowiada danymi:
      {"name": "acme", "balance": 1234.567, "temp": 21.456, "note": null, "unit-name": "Sprzedaż",
       "address": {"city": "Kraków"},
       "sellers": [{"name": "acme"}, {"name": "globex"}],
       "orders": [{"items": [{"price": 10}, {"price": 20}]}, {"items": [{"price": 30}]}],
       "tags": ["a", "b", "c"]}
And   istnieje połączenie "Kurs walut" z opcjonalnym parametrem zapytania "currency"
And   dla waluty "EUR" źródło "Kurs walut" odpowiada danymi {"rate": 4.31789, "date": "2026-10-13"}
And   katalog transformacji zawiera round, date_format, prefix, suffix, uppercase, lowercase i truncate

---

### Role: Użytkownik konfigurujący widoki danych

#### Scenario #1

Dodanie kafelka z polami z dwóch połączeń

```
Given użytkownik nie zdefiniował jeszcze kafelka "Klient i kurs"
When  użytkownik zapisuje kafelek "Klient i kurs" z polami:
      | połączenie | ścieżka    | etykieta | transformacje |
      | Klient     | $.name     | Nazwa    | uppercase     |
      | Kurs walut | $.rate     | Kurs EUR | round(2)      |
Then  kafelek zostaje zapisany
And   użytkownik otrzymuje identyfikator nowego kafelka
```

#### Scenario #2

Wiele pól korzysta z tego samego połączenia

```
When  użytkownik zapisuje kafelek "Klient" z polami:
      | połączenie | ścieżka         | etykieta |
      | Klient     | $.name          | Nazwa    |
      | Klient     | $.balance       | Saldo    |
      | Klient     | $.address.city  | Miasto   |
Then  kafelek zostaje zapisany z trzema polami korzystającymi z połączenia "Klient"
```

#### Scenario #3

Łańcuch transformacji pola jest zapamiętany z kolejnością i parametrami

```
Given użytkownik zapisał kafelek z polem "Saldo" z transformacjami round(decimals: 2), a następnie suffix(text: " zł")
When  użytkownik przegląda szczegóły kafelka
Then  pole "Saldo" ma najpierw transformację round z parametrem decimals równym liczbie 2
And   potem transformację suffix z parametrem text równym tekstowi " zł"
```

#### Scenario #4

Pole bez transformacji jest dozwolone

```
When  użytkownik zapisuje kafelek z polem "Miasto" ze ścieżką "$.address.city" bez transformacji
Then  kafelek zostaje zapisany
And   szczegóły kafelka pokazują pole "Miasto" z pustą listą transformacji
```

#### Scenario #5

Kolejność pól jest zachowana w szczegółach

```
Given użytkownik zapisał kafelek z polami w kolejności "Kurs EUR", "Nazwa", "Miasto"
When  użytkownik przegląda szczegóły kafelka
Then  pola są w kolejności "Kurs EUR", "Nazwa", "Miasto"
```

#### Scenario #6

Nazwy kafelków nie muszą być unikalne, a etykieta może się powtarzać w różnych kafelkach (TEMP-030)

```
Given istnieje kafelek o nazwie "Klient" z polem "Nazwa"
When  użytkownik zapisuje kolejny kafelek "Klient" z polem "Nazwa"
Then  kafelek zostaje zapisany
And   lista zawiera dwa kafelki o nazwie "Klient" z różnymi identyfikatorami
```

#### Scenario #7

Niepoprawna nazwa kafelka

```
Scenario Outline: odrzucenie nazwy
Given użytkownik definiuje kafelek z nazwą <name>
When  użytkownik zapisuje kafelek
Then  zapis zostaje odrzucony z informacją o błędzie nazwy

Examples:
  | name                |
  | brak nazwy          |
  | "   "               |
  | tekst o 101 znakach |
```

#### Scenario #8

Kafelek bez pól jest odrzucany

```
Given użytkownik definiuje kafelek "Pusty" bez żadnego pola
When  użytkownik zapisuje kafelek
Then  zapis zostaje odrzucony z informacją, że kafelek musi mieć co najmniej jedno pole
```

#### Scenario #9

Niepoprawna etykieta pola

```
Scenario Outline: odrzucenie etykiety
Given użytkownik definiuje kafelek, którego drugie pole ma etykietę <label>
When  użytkownik zapisuje kafelek
Then  zapis zostaje odrzucony z informacją o błędzie etykiety drugiego pola

Examples:
  | label               |
  | brak etykiety       |
  | "   "               |
  | tekst o 101 znakach |
```

#### Scenario #10

Niepoprawna ścieżka pola

```
Scenario Outline: odrzucenie ścieżki
Given użytkownik definiuje kafelek, którego pierwsze pole ma ścieżkę <path>
When  użytkownik zapisuje kafelek
Then  zapis zostaje odrzucony z informacją o błędzie ścieżki pierwszego pola

Examples:
  | path              |
  | brak ścieżki      |
  | "sellers[0]"      |
  | "$..name"         |
  | "$.items[?(@.x)]" |
  | "$.items[0:2]"    |
  | "$.a["            |
```

#### Scenario #11

Pole bez połączenia lub z nieistniejącym połączeniem

```
Scenario Outline: odrzucenie połączenia pola
Given użytkownik definiuje kafelek, którego pierwsze pole <connection>
When  użytkownik zapisuje kafelek
Then  zapis zostaje odrzucony z informacją o błędzie połączenia pierwszego pola <details>

Examples:
  | connection                                  | details                                  |
  | nie wskazuje połączenia                     |                                          |
  | wskazuje połączenie, które nie istnieje     | zawierającą identyfikator tego połączenia |
```

#### Scenario #12

Transformacja spoza katalogu

```
Given użytkownik definiuje kafelek, którego pierwsze pole ma transformację "reverse"
When  użytkownik zapisuje kafelek
Then  zapis zostaje odrzucony z informacją o błędzie transformacji pierwszego pola zawierającą klucz "reverse"
```

#### Scenario #13

Parametr transformacji musi być tekstem lub liczbą

```
Given użytkownik definiuje kafelek, którego pierwsze pole ma transformację suffix z parametrem text będącym obiektem
When  użytkownik zapisuje kafelek
Then  zapis zostaje odrzucony z informacją o błędzie parametru text transformacji pierwszego pola
```

#### Scenario #14

Brakujący lub spoza zakresu parametr transformacji ujawnia się dopiero w podglądzie

```
Scenario Outline: parametr transformacji sprawdzany w podglądzie
Given użytkownik zapisuje kafelek z polem "Saldo" ze ścieżką "$.balance" i transformacją <transformation>
Then  kafelek zostaje zapisany
When  użytkownik ogląda podgląd kafelka dla klienta "42"
Then  pole "Saldo" ma błąd transformation_failed transformacji round z powodem invalid_parameter

Examples:
  | transformation          |
  | round bez parametrów    |
  | round(decimals: 20)     |
```

#### Scenario #15

Przy błędzie walidacji nic nie zostaje zapisane

```
Given lista kafelków zawiera kafelek "Klient"
When  użytkownik próbuje zapisać kafelek z pustą nazwą
Then  zapis zostaje odrzucony
And   lista kafelków nadal zawiera tylko kafelek "Klient"
```

#### Scenario #16

Lista kafelków w kolejności dodania

```
Given użytkownik zapisał kafelek "Klient" z trzema polami
And   następnie zapisał kafelek "Kurs" z jednym polem
When  użytkownik przegląda listę kafelków
Then  lista zawiera najpierw "Klient" z liczbą pól 3, a potem "Kurs" z liczbą pól 1
And   dla każdego kafelka widać jego identyfikator
```

#### Scenario #17

Pusta lista kafelków

```
Given użytkownik nie zdefiniował żadnego kafelka
When  użytkownik przegląda listę kafelków
Then  lista jest pusta
```

#### Scenario #18

Szczegóły kafelka

```
Given użytkownik zapisał kafelek "Klient i kurs" z polem "Kurs EUR" ze źródła "Kurs walut", ścieżką "$.rate" i transformacją round(decimals: 2)
When  użytkownik przegląda szczegóły kafelka
Then  widzi nazwę "Klient i kurs"
And   pole "Kurs EUR" z połączeniem "Kurs walut", ścieżką "$.rate" i transformacją round z parametrem decimals równym 2
```

#### Scenario #19

Szczegóły nieistniejącego kafelka

```
Scenario Outline: brak kafelka
Given kafelek <tile> nie istnieje
When  użytkownik przegląda szczegóły kafelka
Then  użytkownik otrzymuje informację, że kafelek nie został znaleziony

Examples:
  | tile                              |
  | o nieznanym identyfikatorze       |
  | o niepoprawnym identyfikatorze    |
```

#### Scenario #20

Edycja zastępuje nazwę i pola wraz z kolejnością

```
Given użytkownik zapisał kafelek "Klient" z polami "Nazwa", "Saldo"
And   następnie zapisał kafelek "Kurs"
When  użytkownik edytuje kafelek "Klient", zmieniając nazwę na "Klient VIP" i ustawiając pola "Miasto", "Nazwa"
Then  szczegóły kafelka pokazują nazwę "Klient VIP" i pola w kolejności "Miasto", "Nazwa"
And   kafelek zachowuje swój identyfikator
And   lista kafelków nadal zawiera najpierw "Klient VIP", a potem "Kurs"
```

#### Scenario #21

Edycja podlega tym samym regułom walidacji

```
Given istnieje kafelek "Klient" z polem "Nazwa"
When  użytkownik edytuje kafelek, usuwając wszystkie pola
Then  edycja zostaje odrzucona z informacją, że kafelek musi mieć co najmniej jedno pole
And   kafelek "Klient" nadal ma pole "Nazwa"
```

#### Scenario #22

Edycja nieistniejącego kafelka

```
Given kafelek o podanym identyfikatorze nie istnieje
When  użytkownik edytuje kafelek
Then  użytkownik otrzymuje informację, że kafelek nie został znaleziony
```

#### Scenario #23

Usunięcie kafelka zachowuje połączenia

```
Given istnieje kafelek "Klient" z polami korzystającymi z połączenia "Klient"
When  użytkownik usuwa kafelek "Klient"
Then  kafelek znika z listy kafelków
And   szczegóły kafelka nie są już dostępne
And   połączenie "Klient" pozostaje bez zmian
```

#### Scenario #24

Usunięcie nieistniejącego kafelka

```
Given kafelek o podanym identyfikatorze nie istnieje
When  użytkownik usuwa kafelek
Then  użytkownik otrzymuje informację, że kafelek nie został znaleziony
```

#### Scenario #25

Nie można usunąć połączenia używanego przez kafelki

```
Given kafelki "Klient" i "Klient i kurs" mają pola korzystające z połączenia "Klient"
When  użytkownik usuwa połączenie "Klient"
Then  usunięcie zostaje odrzucone jako konflikt
And   odpowiedź wymienia identyfikatory i nazwy kafelków "Klient" oraz "Klient i kurs"
And   połączenie "Klient" pozostaje bez zmian
```

#### Scenario #26

Usunięcie połączenia po usunięciu ostatniego korzystającego z niego kafelka

```
Given kafelek "Klient" jest jedynym kafelkiem korzystającym z połączenia "Klient"
And   użytkownik usunął kafelek "Klient"
When  użytkownik usuwa połączenie "Klient"
Then  połączenie zostaje usunięte
```

#### Scenario #27

Połączenie nieużywane przez kafelki usuwa się bez przeszkód

```
Given żaden kafelek nie korzysta z połączenia "Kurs walut"
And   istnieje kafelek korzystający z połączenia "Klient"
When  użytkownik usuwa połączenie "Kurs walut"
Then  połączenie zostaje usunięte
```

#### Scenario #28

Podgląd zapisanego kafelka

```
Given użytkownik zapisał kafelek "Klient i kurs" z polami:
      | połączenie | ścieżka   | etykieta    | transformacje                     |
      | Klient     | $.temp    | Temperatura | round(decimals: 1), suffix(" °C") |
      | Klient     | $.name    | Nazwa       | uppercase                         |
      | Kurs walut | $.rate    | Kurs EUR    | round(decimals: 2)                |
When  użytkownik ogląda podgląd kafelka dla klienta "42" i waluty "EUR"
Then  podgląd zawiera identyfikator i nazwę kafelka "Klient i kurs"
And   pola w kolejności definicji:
      | etykieta    | wartość   |
      | Temperatura | "21.5 °C" |
      | Nazwa       | "ACME"    |
      | Kurs EUR    | 4.32      |
```

#### Scenario #29

Pole bez transformacji zwraca wartość z odpowiedzi bez zmian

```
Given kafelek ma pola bez transformacji: "Saldo" ze ścieżką "$.balance" i "Adres" ze ścieżką "$.address"
When  użytkownik ogląda podgląd kafelka dla klienta "42"
Then  pole "Saldo" ma liczbę 1234.567
And   pole "Adres" ma obiekt {"city": "Kraków"}
```

#### Scenario #30

Ścieżka wskazująca wiele wartości zwraca listę, a każda wartość przechodzi przez transformacje

```
Given kafelek ma pole "Sprzedawcy" ze ścieżką "$.sellers[*].name" i transformacją uppercase
When  użytkownik ogląda podgląd kafelka dla klienta "42"
Then  pole "Sprzedawcy" ma listę ["ACME", "GLOBEX"]
```

#### Scenario #31

Ścieżka wskazująca wiele wartości bez dopasowań zwraca pustą listę

```
Given kafelek ma pole "Faktury" ze ścieżką "$.invoices[*].number"
When  użytkownik ogląda podgląd kafelka dla klienta "42"
Then  pole "Faktury" ma pustą listę
```

#### Scenario #32

Zagnieżdżone wskazanie wielu wartości daje jedną płaską listę

```
Given kafelek ma pole "Ceny" ze ścieżką "$.orders[*].items[*].price" i transformacją suffix(" zł")
When  użytkownik ogląda podgląd kafelka dla klienta "42"
Then  pole "Ceny" ma listę ["10 zł", "20 zł", "30 zł"]
```

#### Scenario #33

Obsługiwane formy ścieżki

```
Scenario Outline: wskazanie wartości ścieżką
Given kafelek ma pole "Wartość" ze ścieżką <path>
When  użytkownik ogląda podgląd kafelka dla klienta "42"
Then  pole "Wartość" ma <value>

Examples:
  | path                 | value                                 |
  | "$['unit-name']"     | tekst "Sprzedaż"                      |
  | "$.tags[0]"          | tekst "a"                             |
  | "$.tags[-1]"         | tekst "c"                             |
  | "$.address.*"        | listę ["Kraków"]                      |
```

#### Scenario #34

Wartość pusta w odpowiedzi nie jest błędem

```
Given kafelek ma pole "Notatka" ze ścieżką "$.note" i transformacją uppercase
When  użytkownik ogląda podgląd kafelka dla klienta "42"
Then  pole "Notatka" ma wartość pustą bez błędu
```

#### Scenario #35

Połączenie używane przez wiele pól jest wywoływane raz

```
Given kafelek ma trzy pola ze źródła "Klient" i jedno pole ze źródła "Kurs walut"
When  użytkownik ogląda podgląd kafelka dla klienta "42" i waluty "EUR"
Then  źródło "Klient" zostaje odpytane dokładnie raz
And   źródło "Kurs walut" zostaje odpytane dokładnie raz
And   wszystkie cztery pola mają wartości
```

#### Scenario #36

Wartości parametrów trafiają do połączeń, które je deklarują

```
Given kafelek ma pole "Nazwa" ze źródła "Klient" i pole "Kurs EUR" ze źródła "Kurs walut"
When  użytkownik ogląda podgląd kafelka, podając tylko wartości parametrów: klienta "42" i walutę "EUR"
Then  źródło "Klient" zostaje odpytane o klienta "42"
And   źródło "Kurs walut" zostaje odpytane o walutę "EUR"
```

#### Scenario #37

Brak wymaganego parametru połączenia

```
Given kafelek ma pola ze źródeł "Klient" i "Kurs walut"
When  użytkownik ogląda podgląd kafelka, podając tylko walutę "EUR"
Then  podgląd zostaje odrzucony z informacją o brakującym parametrze "customerId"
And   żadne źródło nie zostaje odpytane
```

#### Scenario #38

Parametr o tej samej nazwie trafia do każdego połączenia, które go deklaruje

```
Given istnieje też połączenie "Kurs archiwalny" z opcjonalnym parametrem zapytania "currency"
And   kafelek ma pole "Kurs EUR" ze źródła "Kurs walut" i pole "Kurs archiwalny" ze źródła "Kurs archiwalny"
When  użytkownik ogląda podgląd kafelka, podając walutę "EUR"
Then  źródło "Kurs walut" zostaje odpytane o walutę "EUR"
And   źródło "Kurs archiwalny" zostaje odpytane o walutę "EUR"
```

#### Scenario #39

Parametr niezadeklarowany przez żadne połączenie kafelka jest pomijany

```
Given kafelek ma tylko pole ze źródła "Klient"
When  użytkownik ogląda podgląd kafelka dla klienta "42", podając też walutę "EUR"
Then  podgląd zawiera wartość pola ze źródła "Klient"
And   źródło "Klient" zostaje odpytane bez waluty
And   źródło "Kurs walut" nie zostaje odpytane
```

#### Scenario #40

Podgląd nie zmienia kafelka i nie jest zapisywany

```
Given istnieje kafelek "Klient" z polem "Nazwa"
When  użytkownik dwukrotnie ogląda podgląd kafelka dla klienta "42"
Then  szczegóły kafelka "Klient" pozostają bez zmian
And   lista kafelków pozostaje bez zmian
```

#### Scenario #41

Podgląd nieistniejącego kafelka

```
Given kafelek o podanym identyfikatorze nie istnieje
When  użytkownik ogląda podgląd kafelka
Then  użytkownik otrzymuje informację, że kafelek nie został znaleziony
And   żadne źródło nie zostaje odpytane
```

#### Scenario #42

Podgląd niezapisanego kafelka daje ten sam wynik co zapisanego

```
Given użytkownik przygotował definicję kafelka "Klient i kurs" z polami jak w scenariuszu #28, bez jej zapisywania
When  użytkownik ogląda podgląd tej definicji dla klienta "42" i waluty "EUR"
Then  podgląd zawiera nazwę "Klient i kurs" bez identyfikatora kafelka
And   pola mają te same etykiety i wartości co w podglądzie zapisanego kafelka o tej definicji
And   lista kafelków pozostaje bez zmian
```

#### Scenario #43

Podgląd niepoprawnej definicji niezapisanego kafelka

```
Given użytkownik przygotował definicję kafelka bez nazwy, z polem korzystającym z nieistniejącego połączenia
When  użytkownik ogląda podgląd tej definicji
Then  podgląd zostaje odrzucony z informacją o błędach nazwy i połączenia pola
And   żadne źródło nie zostaje odpytane
```

#### Scenario #44

Niedostępne źródło nie blokuje pól z innych źródeł

```
Given kafelek ma pole "Nazwa" ze źródła "Klient" i pole "Kurs EUR" ze źródła "Kurs walut"
And   źródło "Kurs walut" nie odpowiada
When  użytkownik ogląda podgląd kafelka dla klienta "42" i waluty "EUR"
Then  podgląd zostaje zwrócony poprawnie
And   pole "Nazwa" ma wartość "acme"
And   pole "Kurs EUR" ma błąd source_unavailable
```

#### Scenario #45

Błąd źródła zawiera powód

```
Given kafelek ma pole "Kurs EUR" ze źródła "Kurs walut"
And   źródło "Kurs walut" odpowiada statusem 503
When  użytkownik ogląda podgląd kafelka dla waluty "EUR"
Then  pole "Kurs EUR" ma błąd source_unavailable z powodem unexpected_status i statusem 503
```

#### Scenario #46

Brak wartości pod ścieżką

```
Given kafelek ma pola "Miasto" ze ścieżką "$.address.city" i "Kod" ze ścieżką "$.address.zip"
When  użytkownik ogląda podgląd kafelka dla klienta "42"
Then  pole "Miasto" ma wartość "Kraków"
And   pole "Kod" ma błąd path_not_found
```

#### Scenario #47

Nieudana transformacja

```
Given kafelek ma pola "Nazwa jako liczba" ze ścieżką "$.name" i transformacją round(decimals: 1) oraz "Saldo" ze ścieżką "$.balance"
When  użytkownik ogląda podgląd kafelka dla klienta "42"
Then  pole "Nazwa jako liczba" ma błąd transformation_failed transformacji round z powodem invalid_value
And   pole "Saldo" ma wartość 1234.567
```

#### Scenario #48

Nieudana transformacja jednej z wielu wartości daje błąd całego pola

```
Given kafelek ma pole "Tagi" ze ścieżką "$.*" i transformacją uppercase
When  użytkownik ogląda podgląd kafelka dla klienta "42"
Then  pole "Tagi" ma błąd transformation_failed transformacji uppercase z powodem invalid_value
And   pole "Tagi" nie ma wartości
```

#### Scenario #49

Podgląd z błędami wszystkich pól jest nadal poprawną odpowiedzią

```
Given kafelek ma tylko pola ze źródła "Kurs walut"
And   źródło "Kurs walut" nie odpowiada
When  użytkownik ogląda podgląd kafelka dla waluty "EUR"
Then  podgląd zostaje zwrócony poprawnie
And   każde pole ma błąd source_unavailable
```

#### Scenario #50

Liczba jako wartość parametru adresu lub zapytania

```
Given kafelek ma pole "Nazwa" ze źródła "Klient"
When  użytkownik ogląda podgląd kafelka, podając klienta jako liczbę 42
Then  źródło "Klient" zostaje odpytane o klienta "42"
And   pole "Nazwa" ma wartość "acme"
```

#### Scenario #51

Niepoprawna wartość parametru połączenia

```
Given kafelek ma pole "Nazwa" ze źródła "Klient"
When  użytkownik ogląda podgląd kafelka, podając klienta jako obiekt
Then  podgląd zostaje odrzucony z informacją o niepoprawnej wartości parametru "customerId"
And   żadne źródło nie zostaje odpytane
```

#### Scenario #52

Podgląd zapisanego kafelka z parametrami w adresie żądania

```
Given kafelek ma pole "Nazwa" ze źródła "Klient" i pole "Kurs EUR" ze źródła "Kurs walut"
When  użytkownik ogląda podgląd kafelka metodą GET, podając klienta "42" i walutę "EUR" jako parametry zapytania
Then  źródło "Klient" zostaje odpytane o klienta "42"
And   źródło "Kurs walut" zostaje odpytane o walutę "EUR"
And   podgląd jest taki sam jak podgląd metodą POST z tymi samymi wartościami parametrów
```

#### Scenario #53

Parametr powtórzony w adresie żądania podglądu staje się listą wartości

```
Given istnieje połączenie "Kursy" z opcjonalnym parametrem zapytania "currency"
And   kafelek ma pole "Kursy" ze źródła "Kursy"
When  użytkownik ogląda podgląd kafelka metodą GET, podając walutę "EUR" i walutę "USD"
Then  źródło "Kursy" zostaje odpytane o waluty "EUR" i "USD"
```

