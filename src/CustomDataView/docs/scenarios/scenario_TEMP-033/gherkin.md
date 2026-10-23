## Feature

Nazwane źródła danych kafelka i wiązania źródło → źródło

### Background

Given użytkownik konfiguruje kafelki dla widoków danych
And   istnieje połączenie "Klient" z wymaganym parametrem adresu "customer_id"
And   dla klienta "42" źródło "Klient" odpowiada danymi {"id": 7, "name": "acme", "region": "eu", "score": null}
And   istnieje połączenie "Zamówienia" z wymaganym parametrem zapytania "customer" i opcjonalnym parametrem zapytania
      "region" z wartością domyślną "all"
And   dla klienta "7" i regionu "eu" źródło "Zamówienia" odpowiada danymi {"count": 3}
And   istnieje połączenie "Kurs walut" z opcjonalnym parametrem zapytania "currency"

---

### Role: Użytkownik konfigurujący widoki danych

#### Scenario #1

Kafelek z nazwanymi źródłami

```
When  użytkownik zapisuje kafelek "Klient" ze źródłami "customer" (połączenie "Klient") i "rate"
      (połączenie "Kurs walut") oraz polami "Nazwa" ze źródła "customer" i "Kurs" ze źródła "rate"
Then  szczegóły kafelka zwracają oba źródła i źródło każdego pola
And   podgląd kafelka odpytuje każde źródło raz
```

#### Scenario #2

Walidacja źródeł

```
When  użytkownik zapisuje kafelek z powtórzonym kluczem źródła, kluczem zawierającym "/", kluczem "parent",
      źródłem bez połączenia, źródłem z nieistniejącym połączeniem albo polem wskazującym nieistniejące źródło
Then  zapis zostaje odrzucony z błędem walidacji wskazującym źródło albo pole
```

#### Scenario #3

Połączenie używane przez źródło nie może zostać usunięte

```
Given kafelek "Klient" ma źródło "customer" z połączeniem "Klient"
When  użytkownik usuwa połączenie "Klient"
Then  usunięcie zostaje odrzucone z listą kafelków, w tym "Klient"
```

#### Scenario #4

Wiązanie źródło → źródło przekazuje kilka wartości jednym wiązaniem

```
Given kafelek "Zamówienia klienta" ma źródła "customer" i "orders"
And   ukryte pola "ID klienta" ($.id) i "Region" ($.region) ze źródła "customer" oraz pole "Liczba" ($.count) ze
      źródła "orders"
And   wiązanie "customer" → "orders" mapuje "ID klienta" na parametr "customer" i "Region" na parametr "region"
When  użytkownik ogląda podgląd kafelka dla klienta "42"
Then  najpierw zostaje odpytane źródło "Klient", a potem źródło "Zamówienia" o klienta "7" i region "eu"
And   pole "Liczba" ma wartość 3
```

#### Scenario #5

Walidacja wiązań kafelka

```
When  użytkownik zapisuje kafelek z wiązaniem wskazującym nieistniejący węzeł, pole spoza źródła "from", parametr
      niezadeklarowany w połączeniu celu, ten sam węzeł jako "from" i "to", powtórzoną parę, pusty "map", parametr
      zasilany dwa razy albo cykl wiązań
Then  zapis zostaje odrzucony z błędem walidacji wskazującym wiązanie
```

#### Scenario #6

Błąd pola źródłowego wstrzymuje cel wiązania

```
Given źródło "Klient" jest niedostępne
When  użytkownik ogląda podgląd kafelka "Zamówienia klienta"
Then  pola ze źródła "customer" mają błąd source_unavailable
And   pole "Liczba" ma błąd dependency_failed wskazujący węzeł "customer", pole "ID klienta" i kod source_unavailable
And   źródło "Zamówienia" nie zostaje odpytane
```

#### Scenario #7

Wiązanie do parametru, którego połączenie już nie deklaruje

```
Given połączenie "Zamówienia" po edycji nie deklaruje parametru "region"
When  użytkownik ogląda podgląd kafelka "Zamówienia klienta"
Then  pole "Liczba" ma błąd invalid_binding wskazujący parametr "region"
And   źródło "Zamówienia" nie zostaje odpytane
```

#### Scenario #8

Wartość null z pola źródłowego oznacza brak wartości

```
Given wiązanie "customer" → "orders" mapuje "ID klienta" na "customer" i pole "Wynik" ($.score) na "region"
When  użytkownik ogląda podgląd kafelka dla klienta "42"
Then  źródło "Zamówienia" zostaje odpytane o klienta "7" i domyślny region "all"
```

#### Scenario #9

Kontrakt kafelka w szczegółach

```
When  użytkownik ogląda szczegóły kafelka "Zamówienia klienta"
Then  kontrakt zawiera wejście-parametr "customer_id" (wymagany), nie zawiera parametrów "customer" i "region"
      związanych wiązaniem, nie ma wejść danych
And   wyjścia zawierają etykiety "ID klienta", "Region" i "Liczba" bez elementów
```

#### Scenario #10

Wartość testowa nie nadpisuje wiązania

```
When  użytkownik ogląda podgląd kafelka "Zamówienia klienta" dla klienta "42", podając też region "us"
Then  źródło "Zamówienia" zostaje odpytane o region "eu" z wiązania
```
