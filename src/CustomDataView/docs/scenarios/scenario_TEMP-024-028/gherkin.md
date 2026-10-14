## Feature

Widoki układające kafelki w kolejności pozycji, z opcjonalną klasą CSS opisującą sposób wyświetlenia kafelka

### Background

Given użytkownik konfiguruje widoki danych
And   istnieją kafelki "Klient", "Kurs walut" i "Zamówienia"

---

### Role: Użytkownik konfigurujący widoki danych

#### Scenario #1

Dodanie widoku z dwoma kafelkami

```
When  użytkownik zapisuje widok "Pulpit klienta" z kafelkami:
      | kafelek    | pozycja | klasa CSS       |
      | Klient     | 0       | col-6 highlight |
      | Kurs walut | 1       | col-6           |
Then  widok zostaje zapisany
And   użytkownik otrzymuje identyfikator nowego widoku
And   szczegóły widoku pokazują kafelki "Klient" i "Kurs walut" z podanymi pozycjami i klasami CSS
```

#### Scenario #2

Widok bez kafelków jest dozwolony

```
When  użytkownik zapisuje widok "Pusty" bez kafelków
Then  widok zostaje zapisany
And   szczegóły widoku pokazują pustą listę kafelków
```

#### Scenario #3

Klasa CSS jest opcjonalna, a spacje na jej brzegach są pomijane

```
When  użytkownik zapisuje widok "Pulpit" z kafelkami:
      | kafelek    | pozycja | klasa CSS     |
      | Klient     | 0       |               |
      | Kurs walut | 1       | "   "         |
      | Zamówienia | 2       | "  col-12  "  |
Then  widok zostaje zapisany
And   kafelki "Klient" i "Kurs walut" nie mają klasy CSS
And   kafelek "Zamówienia" ma klasę CSS "col-12"
```

#### Scenario #4

Kafelki są pokazywane w kolejności pozycji, a luki w pozycjach są zachowane

```
When  użytkownik zapisuje widok "Pulpit" z kafelkami w kolejności:
      | kafelek    | pozycja |
      | Zamówienia | 10      |
      | Klient     | 0       |
      | Kurs walut | 5       |
Then  szczegóły widoku pokazują kafelki w kolejności "Klient", "Kurs walut", "Zamówienia"
And   ich pozycje to 0, 5 i 10
```

#### Scenario #5

Ten sam kafelek może występować w różnych widokach

```
Given użytkownik zapisał widok "Sprzedaż" z kafelkiem "Klient"
When  użytkownik zapisuje widok "Obsługa" z kafelkiem "Klient"
Then  widok "Obsługa" zostaje zapisany
And   oba widoki zawierają kafelek "Klient"
```

#### Scenario #6

Nazwa widoku nie musi być unikalna

```
Given użytkownik zapisał widok "Pulpit"
When  użytkownik zapisuje kolejny widok "Pulpit"
Then  widok zostaje zapisany
And   lista widoków zawiera dwa widoki "Pulpit"
```

#### Scenario #7

Edycja kafelka zachowuje jego miejsce w widokach

```
Given użytkownik zapisał widok "Pulpit" z kafelkiem "Klient" na pozycji 3 z klasą CSS "col-6"
When  użytkownik zmienia nazwę kafelka "Klient" na "Klient VIP" i jego pola
Then  widok "Pulpit" nadal zawiera ten kafelek na pozycji 3 z klasą CSS "col-6"
And   szczegóły widoku pokazują kafelek pod nazwą "Klient VIP"
```

#### Scenario #8

Usunięcie kafelka zdejmuje go ze wszystkich widoków

```
Given użytkownik zapisał widok "Sprzedaż" z kafelkami "Klient" i "Kurs walut"
And   użytkownik zapisał widok "Obsługa" z kafelkami "Klient" i "Zamówienia"
When  użytkownik usuwa kafelek "Klient"
Then  widok "Sprzedaż" zawiera tylko kafelek "Kurs walut"
And   widok "Obsługa" zawiera tylko kafelek "Zamówienia"
And   lista widoków pokazuje oba widoki, każdy z jednym kafelkiem
```

#### Scenario #9

Nazwa widoku jest wymagana i ma najwyżej 100 znaków

```
When  użytkownik zapisuje widok o nazwie <nazwa>
Then  widok nie zostaje zapisany
And   użytkownik otrzymuje błąd walidacji nazwy widoku

Examples:
| nazwa                   |
| brak nazwy              |
| "   "                   |
| nazwa o długości 101 znaków |
```

#### Scenario #10

Każdy kafelek w widoku musi wskazywać kafelek

```
When  użytkownik zapisuje widok "Pulpit" z wpisem na pozycji 0 bez wskazanego kafelka
Then  widok nie zostaje zapisany
And   użytkownik otrzymuje błąd walidacji pierwszego wpisu informujący, że kafelek jest wymagany
```

#### Scenario #11

Widok nie może odwoływać się do nieistniejącego kafelka

```
Given kafelek o identyfikatorze "7c9e…" nie istnieje
When  użytkownik zapisuje widok "Pulpit" z kafelkami "Klient" na pozycji 0 i "7c9e…" na pozycji 1
Then  widok nie zostaje zapisany
And   użytkownik otrzymuje błąd walidacji drugiego wpisu z identyfikatorem "7c9e…" w komunikacie
```

#### Scenario #12

Ten sam kafelek nie może wystąpić w widoku więcej niż raz

```
When  użytkownik zapisuje widok "Pulpit" z kafelkami:
      | kafelek | pozycja |
      | Klient  | 0       |
      | Klient  | 1       |
      | Klient  | 2       |
Then  widok nie zostaje zapisany
And   użytkownik otrzymuje błędy walidacji drugiego i trzeciego wpisu informujące o powtórzonym kafelku
And   pierwszy wpis nie ma błędu
```

#### Scenario #13

Pozycja jest wymagana i nie może być ujemna

```
When  użytkownik zapisuje widok "Pulpit" z kafelkiem "Klient" <pozycja>
Then  widok nie zostaje zapisany
And   użytkownik otrzymuje błąd walidacji pozycji pierwszego wpisu

Examples:
| pozycja            |
| bez pozycji        |
| na pozycji -1      |
```

#### Scenario #14

Pozycja nie może się powtórzyć w widoku

```
When  użytkownik zapisuje widok "Pulpit" z kafelkami:
      | kafelek    | pozycja |
      | Klient     | 2       |
      | Kurs walut | 2       |
      | Zamówienia | 2       |
Then  widok nie zostaje zapisany
And   użytkownik otrzymuje błędy walidacji pozycji drugiego i trzeciego wpisu informujące o powtórzonej pozycji 2
```

#### Scenario #15

Klasa CSS ma najwyżej 200 znaków

```
When  użytkownik zapisuje widok "Pulpit" z kafelkiem "Klient" i klasą CSS o długości <długość> znaków
Then  <wynik>

Examples:
| długość | wynik                                                          |
| 200     | widok zostaje zapisany                                         |
| 201     | widok nie zostaje zapisany i pojawia się błąd walidacji klasy CSS |
```

#### Scenario #16

Przy błędzie walidacji nic nie zostaje zapisane

```
Given użytkownik ma jeden zapisany widok "Sprzedaż"
When  użytkownik zapisuje widok "Pulpit" z kafelkiem "Klient" na pozycji -1
Then  widok nie zostaje zapisany
And   lista widoków nadal zawiera tylko widok "Sprzedaż"
```

#### Scenario #17

Lista widoków pokazuje nazwę i liczbę kafelków w kolejności dodania

```
Given użytkownik zapisał kolejno widoki:
      | widok    | kafelki                        |
      | Sprzedaż | Klient, Kurs walut             |
      | Pusty    |                                |
      | Obsługa  | Klient, Kurs walut, Zamówienia |
When  użytkownik przegląda listę widoków
Then  lista zawiera w kolejności:
      | widok    | liczba kafelków |
      | Sprzedaż | 2               |
      | Pusty    | 0               |
      | Obsługa  | 3               |
And   każdy widok ma swój identyfikator
```

#### Scenario #18

Lista widoków jest pusta, gdy nie zdefiniowano żadnego widoku

```
Given użytkownik nie zapisał żadnego widoku
When  użytkownik przegląda listę widoków
Then  lista widoków jest pusta
```

#### Scenario #19

Szczegóły widoku pokazują kafelki z identyfikatorem, nazwą, pozycją i klasą CSS

```
Given użytkownik zapisał widok "Pulpit klienta" z kafelkami "Kurs walut" na pozycji 1 bez klasy CSS
      i "Klient" na pozycji 0 z klasą CSS "col-6"
When  użytkownik przegląda szczegóły widoku
Then  widok ma nazwę "Pulpit klienta"
And   pierwszy jest kafelek "Klient" z identyfikatorem, pozycją 0 i klasą CSS "col-6"
And   drugi jest kafelek "Kurs walut" z identyfikatorem, pozycją 1 i bez klasy CSS
```

#### Scenario #20

Szczegóły nieistniejącego widoku nie są dostępne

```
When  użytkownik przegląda szczegóły widoku, który nie istnieje
Then  użytkownik otrzymuje informację, że widok nie został znaleziony
```

#### Scenario #21

Edycja zastępuje nazwę i całą listę kafelków

```
Given użytkownik zapisał widok "Pulpit" z kafelkami "Klient" na pozycji 0 i "Kurs walut" na pozycji 1
And   po nim widok "Obsługa"
When  użytkownik zmienia widok "Pulpit" na "Pulpit klienta" z kafelkami:
      | kafelek    | pozycja | klasa CSS |
      | Zamówienia | 0       | col-12    |
      | Klient     | 4       | col-6     |
Then  szczegóły widoku pokazują nazwę "Pulpit klienta"
And   kafelki "Zamówienia" na pozycji 0 i "Klient" na pozycji 4 z nowymi klasami CSS
And   kafelek "Kurs walut" nie należy już do widoku
And   widok ma ten sam identyfikator i nadal jest pierwszy na liście widoków
```

#### Scenario #22

Edycja podlega tym samym regułom walidacji i przy błędzie niczego nie zmienia

```
Given użytkownik zapisał widok "Pulpit" z kafelkiem "Klient" na pozycji 0
When  użytkownik zmienia widok "Pulpit" tak, że kafelek "Klient" występuje w nim dwa razy
Then  widok nie zostaje zmieniony
And   użytkownik otrzymuje błąd walidacji powtórzonego kafelka
And   szczegóły widoku nadal pokazują nazwę "Pulpit" i kafelek "Klient" na pozycji 0
```

#### Scenario #23

Edycja nieistniejącego widoku nie jest możliwa

```
When  użytkownik zmienia widok, który nie istnieje
Then  użytkownik otrzymuje informację, że widok nie został znaleziony
```

#### Scenario #24

Usunięcie widoku

```
Given użytkownik zapisał widok "Pulpit"
When  użytkownik usuwa widok "Pulpit"
Then  widok "Pulpit" nie występuje na liście widoków
And   szczegóły widoku "Pulpit" nie są dostępne
```

#### Scenario #25

Usunięcie widoku nie zmienia kafelków ani połączeń

```
Given użytkownik zapisał widoki "Sprzedaż" i "Obsługa", oba z kafelkiem "Klient"
When  użytkownik usuwa widok "Sprzedaż"
Then  kafelek "Klient" nadal istnieje z niezmienioną definicją
And   połączenia używane przez kafelek "Klient" nadal istnieją
And   widok "Obsługa" nadal zawiera kafelek "Klient"
```

#### Scenario #26

Usunięcie nieistniejącego widoku nie jest możliwe

```
When  użytkownik usuwa widok, który nie istnieje
Then  użytkownik otrzymuje informację, że widok nie został znaleziony
```
