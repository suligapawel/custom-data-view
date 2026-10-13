## Feature

Połączenia do zewnętrznych REST API jako szablony z parametrami podawanymi przy wywołaniu

### Background

Given użytkownik konfiguruje źródła danych dla widoków
And   zewnętrzne API "Kontrakty" jest dostępne pod adresem "https://api.example.com"
And   globalny limit czasu odpowiedzi wynosi 10 sekund
And   globalny limit rozmiaru odpowiedzi wynosi 1 MB

---

### Role: Użytkownik konfigurujący widoki danych

#### Scenario #1

Dodanie połączenia GET z parametrem w adresie

```
Given użytkownik nie zdefiniował jeszcze połączenia "Sprzedawcy kontraktu"
When  użytkownik zapisuje połączenie GET "Sprzedawcy kontraktu" z adresem "https://api.example.com/contracts/{id}/sellers?units=metric"
And   połączenie ma nagłówek "Accept-Language" o wartości "pl"
And   połączenie deklaruje wymagany parametr adresu "id"
And   połączenie deklaruje opcjonalny parametr zapytania "pageSize" z wartością domyślną "20"
Then  połączenie zostaje zapisane
And   użytkownik otrzymuje identyfikator nowego połączenia
```

#### Scenario #2

Dodanie połączenia POST z szablonem treści

```
Given użytkownik definiuje połączenie POST "Wyszukiwanie sprzedawców" z adresem "https://api.example.com/sellers/search"
When  użytkownik zapisuje je z szablonem treści {"contractId": "{id}", "limit": "{limit}"}
And   połączenie deklaruje wymagany parametr treści "id"
And   połączenie deklaruje opcjonalny parametr treści "limit" z wartością domyślną 10
Then  połączenie zostaje zapisane
And   szczegóły połączenia pokazują szablon treści {"contractId": "{id}", "limit": "{limit}"} bez zmian
```

#### Scenario #3

Dodanie połączenia POST bez treści

```
Given użytkownik definiuje połączenie POST "Odświeżenie danych" bez szablonu treści
When  użytkownik zapisuje połączenie
Then  połączenie zostaje zapisane
```

#### Scenario #4

Zapisane połączenia są widoczne na liście w kolejności dodania

```
Given użytkownik zapisał połączenie "Sprzedawcy kontraktu"
And   następnie zapisał połączenie "Wyszukiwanie sprzedawców"
When  użytkownik przegląda listę połączeń
Then  lista zawiera najpierw "Sprzedawcy kontraktu", a potem "Wyszukiwanie sprzedawców"
And   dla każdego połączenia widać identyfikator, nazwę, rodzaj GET lub POST oraz szablon adresu
```

#### Scenario #5

Nazwy połączeń nie muszą być unikalne

```
Given istnieje połączenie o nazwie "Pogoda"
When  użytkownik zapisuje kolejne połączenie o nazwie "Pogoda"
Then  połączenie zostaje zapisane
And   lista zawiera dwa połączenia o nazwie "Pogoda" z różnymi identyfikatorami
```

#### Scenario #6

Rodzaj połączenia nie zależy od wielkości liter

```
Given użytkownik podaje rodzaj połączenia jako "get"
When  użytkownik zapisuje połączenie
Then  połączenie zostaje zapisane jako połączenie GET
```

#### Scenario #7

Niepoprawna nazwa połączenia

```
Scenario Outline: odrzucenie nazwy
Given użytkownik definiuje połączenie z nazwą <name>
When  użytkownik zapisuje połączenie
Then  zapis zostaje odrzucony z informacją o błędzie nazwy

Examples:
  | name                  |
  | brak nazwy            |
  | "   "                 |
  | tekst o 101 znakach   |
```

#### Scenario #8

Niepoprawny adres połączenia

```
Scenario Outline: odrzucenie adresu
Given użytkownik definiuje połączenie z adresem <url>
When  użytkownik zapisuje połączenie
Then  zapis zostaje odrzucony z informacją o błędzie adresu

Examples:
  | url                              |
  | brak adresu                      |
  | "/contracts/42"                  |
  | "ftp://api.example.com/contracts" |
```

#### Scenario #9

Parametr dozwolony tylko w ścieżce adresu

```
Scenario Outline: odrzucenie miejsca na parametr poza ścieżką
Given użytkownik definiuje połączenie z adresem <url>
When  użytkownik zapisuje połączenie
Then  zapis zostaje odrzucony z informacją o błędzie adresu

Examples:
  | url                                      |
  | "https://{host}/contracts"               |
  | "https://api.example.com/contracts?q={q}" |
  | "https://api.example.com/contracts/{id"  |
```

#### Scenario #10

Nieobsługiwany rodzaj połączenia

```
Scenario Outline: odrzucenie rodzaju
Given użytkownik definiuje połączenie rodzaju <method>
When  użytkownik zapisuje połączenie
Then  zapis zostaje odrzucony z informacją, że dozwolone są tylko GET i POST

Examples:
  | method       |
  | "PUT"        |
  | brak rodzaju |
```

#### Scenario #11

Treść dozwolona tylko dla połączenia POST

```
Scenario Outline: odrzucenie treści dla GET
Given użytkownik definiuje połączenie GET
And   połączenie ma <element>
When  użytkownik zapisuje połączenie
Then  zapis zostaje odrzucony z informacją, że treść jest dozwolona tylko dla POST

Examples:
  | element                      |
  | szablon treści {"q": 1}      |
  | parametr treści "limit"      |
```

#### Scenario #12

Definicja, która nie jest poprawnym JSON

```
Given użytkownik przesyła definicję połączenia, która nie jest poprawnym dokumentem JSON
When  użytkownik zapisuje połączenie
Then  zapis zostaje odrzucony
```

#### Scenario #13

Parametry adresu muszą odpowiadać miejscom w adresie

```
Scenario Outline: niezgodność parametrów adresu
Given użytkownik definiuje połączenie z adresem <url>
And   połączenie deklaruje parametry adresu <declared>
When  użytkownik zapisuje połączenie
Then  zapis zostaje odrzucony z informacją o <error>

Examples:
  | url                                           | declared | error                                  |
  | "https://api.example.com/contracts/{id}"      | brak     | niezadeklarowanym parametrze "id"      |
  | "https://api.example.com/contracts"           | "id"     | nieużytym parametrze adresu "id"       |
```

#### Scenario #14

Parametry treści muszą odpowiadać miejscom w treści

```
Scenario Outline: niezgodność parametrów treści
Given użytkownik definiuje połączenie POST z szablonem treści <body>
And   połączenie deklaruje parametry treści <declared>
When  użytkownik zapisuje połączenie
Then  zapis zostaje odrzucony z informacją o <error>

Examples:
  | body                     | declared | error                                 |
  | {"contractId": "{id}"}   | brak     | niezadeklarowanym parametrze "id"     |
  | {"contractId": 42}       | "id"     | nieużytym parametrze treści "id"      |
```

#### Scenario #15

Nazwy parametrów adresu i treści w formacie snake_case

```
Scenario Outline: odrzucenie nazwy parametru
Given użytkownik definiuje połączenie z parametrem <section> o nazwie <name>
When  użytkownik zapisuje połączenie
Then  zapis zostaje odrzucony z informacją o błędzie nazwy parametru

Examples:
  | section | name          |
  | adresu  | "Id"          |
  | treści  | "contract-id" |
```

#### Scenario #16

Nazwy parametrów zapytania i nagłówków

```
Scenario Outline: nazwy parametrów zapytania i nagłówków
Given użytkownik definiuje połączenie z <element> o nazwie <name>
When  użytkownik zapisuje połączenie
Then  <outcome>

Examples:
  | element               | name       | outcome                                                |
  | parametrem zapytania  | ""         | zapis zostaje odrzucony z informacją o pustej nazwie    |
  | nagłówkiem            | ""         | zapis zostaje odrzucony z informacją o pustej nazwie    |
  | parametrem zapytania  | "pageSize" | połączenie zostaje zapisane                             |
```

#### Scenario #17

Powtórzenia nazw parametrów

```
Scenario Outline: powtórzona nazwa parametru
Given użytkownik definiuje połączenie z <declarations>
When  użytkownik zapisuje połączenie
Then  <outcome>

Examples:
  | declarations                                        | outcome                                                     |
  | dwoma parametrami adresu "id"                       | zapis zostaje odrzucony z informacją o powtórzonej nazwie   |
  | dwoma parametrami treści "limit"                    | zapis zostaje odrzucony z informacją o powtórzonej nazwie   |
  | dwoma parametrami zapytania "tag"                   | połączenie zostaje zapisane                                 |
  | parametrem adresu "id" i parametrem treści "id"     | połączenie zostaje zapisane                                 |
```

#### Scenario #18

Wymagalność i wartość domyślna parametru

```
Scenario Outline: sprzeczna deklaracja parametru
Given użytkownik definiuje połączenie z parametrem <declaration>
When  użytkownik zapisuje połączenie
Then  zapis zostaje odrzucony z informacją o błędzie deklaracji parametru

Examples:
  | declaration                                                   |
  | zapytania "pageSize", wymaganym i z wartością domyślną "20"    |
  | adresu "id", opcjonalnym i bez wartości domyślnej              |
```

#### Scenario #19

Niepoprawne nagłówki

```
Scenario Outline: nagłówki połączenia
Given użytkownik definiuje połączenie z nagłówkiem <name> o wartości <value>
When  użytkownik zapisuje połączenie
Then  <outcome>

Examples:
  | name             | value        | outcome                                                  |
  | "Accept Language" | "pl"        | zapis zostaje odrzucony z informacją o błędzie nagłówka  |
  | "Accept:Language" | "pl"        | zapis zostaje odrzucony z informacją o błędzie nagłówka  |
  | "X-Trace"        | brak wartości | zapis zostaje odrzucony z informacją o błędzie nagłówka  |
  | "X-Trace"        | ""           | połączenie zostaje zapisane                              |
```

#### Scenario #20

Błędna definicja niczego nie zapisuje

```
Given lista połączeń zawiera 2 połączenia
When  użytkownik próbuje zapisać połączenie z błędną definicją
Then  zapis zostaje odrzucony
And   lista połączeń nadal zawiera 2 połączenia
```

#### Scenario #21

Pusta lista połączeń

```
Given nie zdefiniowano żadnego połączenia
When  użytkownik przegląda listę połączeń
Then  lista połączeń jest pusta
```

#### Scenario #22

Szczegóły połączenia

```
Given istnieje połączenie POST "Wyszukiwanie sprzedawców" z nagłówkami, parametrami i szablonem treści
When  użytkownik przegląda szczegóły tego połączenia
Then  widzi nazwę, rodzaj POST, szablon adresu i nagłówki
And   widzi deklaracje parametrów adresu, zapytania i treści z wymagalnością i wartościami domyślnymi
And   widzi szablon treści
```

#### Scenario #23

Nieistniejące połączenie

```
Scenario Outline: operacja na nieistniejącym połączeniu
Given połączenie <reference> nie istnieje
When  użytkownik próbuje <action> tego połączenia
Then  użytkownik otrzymuje informację, że połączenie nie zostało znalezione

Examples:
  | reference                       | action                  |
  | o nieznanym identyfikatorze     | wyświetlić szczegóły    |
  | o nieznanym identyfikatorze     | zmienić definicję       |
  | o nieznanym identyfikatorze     | usunąć                  |
  | o nieznanym identyfikatorze     | przetestować            |
  | o identyfikatorze "abc"         | wyświetlić szczegóły    |
```

#### Scenario #24

Edycja połączenia zastępuje całą definicję

```
Given istnieje połączenie GET "Sprzedawcy kontraktu" na drugiej pozycji listy
When  użytkownik zmienia je na połączenie POST "Wyszukiwanie sprzedawców" z szablonem treści i parametrami treści
Then  szczegóły połączenia pokazują wyłącznie nową definicję
And   identyfikator połączenia się nie zmienia
And   połączenie pozostaje na drugiej pozycji listy
```

#### Scenario #25

Edycja podlega tym samym regułom co dodanie

```
Given istnieje połączenie POST "Wyszukiwanie sprzedawców" z szablonem treści
When  użytkownik zmienia rodzaj połączenia na GET, pozostawiając szablon treści
Then  zmiana zostaje odrzucona z informacją, że treść jest dozwolona tylko dla POST
And   połączenie zachowuje poprzednią definicję
```

#### Scenario #26

Usunięcie połączenia

```
Given istnieje połączenie "Pogoda"
When  użytkownik usuwa połączenie "Pogoda"
Then  połączenie "Pogoda" nie występuje na liście
And   próba wyświetlenia jego szczegółów kończy się informacją, że połączenie nie zostało znalezione
```

#### Scenario #27

Test połączenia GET z parametrem w adresie

```
Given istnieje połączenie GET "Sprzedawcy kontraktu" z adresem "https://api.example.com/contracts/{id}/sellers"
When  użytkownik testuje połączenie z wartością parametru adresu "id" równą "42"
Then  zewnętrzne API otrzymuje zapytanie GET na adres "https://api.example.com/contracts/42/sellers"
```

#### Scenario #28

Wartość parametru adresu jest bezpiecznie wstawiana

```
Given istnieje połączenie GET z adresem "https://api.example.com/contracts/{id}/sellers"
When  użytkownik testuje połączenie z wartością parametru adresu "id" równą "a/b"
Then  zewnętrzne API otrzymuje zapytanie na adres "https://api.example.com/contracts/a%2Fb/sellers"
```

#### Scenario #29

Parametry zapytania z wartości i wartości domyślnych

```
Scenario Outline: dopisanie parametrów zapytania
Given istnieje połączenie GET z adresem "https://api.example.com/sellers?units=metric"
And   połączenie deklaruje opcjonalny parametr zapytania "pageSize" z wartością domyślną "20"
And   połączenie deklaruje opcjonalny parametr zapytania "status" bez wartości domyślnej
When  użytkownik testuje połączenie z wartościami parametrów zapytania <values>
Then  zewnętrzne API otrzymuje zapytanie na adres <address>

Examples:
  | values             | address                                                              |
  | brak               | "https://api.example.com/sellers?units=metric&pageSize=20"           |
  | pageSize = "50"    | "https://api.example.com/sellers?units=metric&pageSize=50"           |
  | status = "active"  | "https://api.example.com/sellers?units=metric&pageSize=20&status=active" |
```

#### Scenario #30

Powtórzone parametry zapytania

```
Scenario Outline: wiele wartości parametru zapytania
Given istnieje połączenie GET z adresem "https://api.example.com/sellers"
And   połączenie deklaruje dwa opcjonalne parametry zapytania "tag" z wartościami domyślnymi "a" oraz "b"
When  użytkownik testuje połączenie z wartościami parametru "tag" <values>
Then  zewnętrzne API otrzymuje zapytanie na adres <address>

Examples:
  | values               | address                                                 |
  | brak                 | "https://api.example.com/sellers?tag=a&tag=b"           |
  | "x", "y", "z"        | "https://api.example.com/sellers?tag=x&tag=y&tag=z"     |
```

#### Scenario #31

Test połączenia POST z parametrami treści

```
Given istnieje połączenie POST "Wyszukiwanie sprzedawców" z szablonem treści {"contractId": "{id}", "limit": "{limit}"}
When  użytkownik testuje połączenie z wartością parametru treści "id" równą "C-42" i "limit" równą 5
Then  zewnętrzne API otrzymuje zapytanie POST z treścią {"contractId": "C-42", "limit": 5}
And   treść jest oznaczona jako JSON
```

#### Scenario #32

Opcjonalny parametr treści bez wartości

```
Scenario Outline: wartość parametru treści
Given istnieje połączenie POST z szablonem treści {"contractId": "{id}", "limit": "{limit}"}
And   parametr treści "limit" jest opcjonalny <default>
When  użytkownik testuje połączenie z wartością parametru "id" równą "C-42" i bez wartości "limit"
Then  zewnętrzne API otrzymuje treść <body>

Examples:
  | default                     | body                                      |
  | z wartością domyślną 10     | {"contractId": "C-42", "limit": 10}       |
  | bez wartości domyślnej      | {"contractId": "C-42", "limit": null}     |
```

#### Scenario #33

Niepoprawne wartości parametrów przy teście

```
Scenario Outline: odrzucenie wartości parametrów
Given istnieje połączenie GET z wymaganym parametrem adresu "id"
When  użytkownik testuje połączenie z wartościami <values>
Then  test zostaje odrzucony z informacją o błędzie parametru <parameter>
And   zewnętrzne API nie otrzymuje żadnego zapytania

Examples:
  | values                                   | parameter          |
  | bez wartości "id"                        | adresu "id"        |
  | "id" = "42" oraz nieznany parametr "foo" | adresu "foo"       |
  | "id" = ""                                | adresu "id"        |
  | "id" = 42 jako liczba                    | adresu "id"        |
```

#### Scenario #34

Test zapisanego i niezapisanego połączenia daje ten sam wynik

```
Given istnieje zapisane połączenie "Sprzedawcy kontraktu"
And   użytkownik ma niezapisaną definicję identyczną z tym połączeniem
When  użytkownik testuje zapisane połączenie oraz niezapisaną definicję z wartością parametru "id" równą "42"
Then  oba testy wysyłają takie samo zapytanie do zewnętrznego API
And   oba testy zwracają taki sam wynik
```

#### Scenario #35

Test niepoprawnej niezapisanej definicji

```
Given użytkownik ma niezapisaną definicję połączenia z adresem "ftp://api.example.com"
When  użytkownik testuje tę definicję
Then  test zostaje odrzucony z informacją o błędzie adresu
And   zewnętrzne API nie otrzymuje żadnego zapytania
```

#### Scenario #36

Udany test połączenia

```
Given zewnętrzne API odpowiada statusem 200 i poprawnym dokumentem JSON {"sellers": [{"name": "ACME"}]}
When  użytkownik testuje połączenie
Then  test kończy się sukcesem
And   wynik zawiera status 200, czas odpowiedzi w milisekundach i pełną treść {"sellers": [{"name": "ACME"}]}
```

#### Scenario #37

Źródło niedostępne

```
Scenario Outline: brak odpowiedzi źródła
Given <situation>
When  użytkownik testuje połączenie
Then  test kończy się błędem "source_unavailable"

Examples:
  | situation                                               |
  | zewnętrzne API nie przyjmuje połączeń                   |
  | zewnętrzne API nie odpowiada dłużej niż 10 sekund       |
```

#### Scenario #38

Nieoczekiwany status odpowiedzi

```
Scenario Outline: odpowiedź inna niż sukces
Given zewnętrzne API odpowiada statusem <status>
When  użytkownik testuje połączenie
Then  test kończy się błędem "unexpected_status" ze statusem <status>
And   przekierowanie nie jest wykonywane

Examples:
  | status |
  | 302    |
  | 404    |
  | 503    |
```

#### Scenario #39

Odpowiedź, która nie jest JSON

```
Scenario Outline: niepoprawna treść odpowiedzi
Given zewnętrzne API odpowiada statusem <status> z treścią <content>
When  użytkownik testuje połączenie
Then  test kończy się błędem "invalid_json"

Examples:
  | status | content            |
  | 200    | "<html>…</html>"   |
  | 204    | pustą              |
```

#### Scenario #40

Zbyt duża odpowiedź

```
Given zewnętrzne API odpowiada statusem 200 i treścią większą niż 1 MB
When  użytkownik testuje połączenie
Then  test kończy się błędem "response_too_large"
And   odpowiedź nie jest wczytywana w całości
```

#### Scenario #41

Test nie zmienia konfiguracji

```
Given istnieje połączenie "Sprzedawcy kontraktu", a lista zawiera 1 połączenie
When  użytkownik testuje to połączenie oraz niezapisaną definicję innego połączenia
Then  lista nadal zawiera 1 połączenie
And   definicja połączenia "Sprzedawcy kontraktu" pozostaje bez zmian
And   wynik testu nie jest nigdzie zapisany
```
