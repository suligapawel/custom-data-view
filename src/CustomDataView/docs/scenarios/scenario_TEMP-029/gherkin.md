## Feature

Podgląd widoku z danymi wszystkich jego kafelków dla jednej listy parametrów

### Background

Given użytkownik konfiguruje widoki danych
And   istnieje połączenie "Klient" z wymaganym parametrem adresu "customer_id"
And   dla klienta "42" źródło "Klient" odpowiada danymi {"name": "acme", "balance": 1234.567}
And   istnieje połączenie "Kurs walut" z opcjonalnym parametrem zapytania "currency"
And   dla waluty "EUR" źródło "Kurs walut" odpowiada danymi {"rate": 4.31789}
And   istnieje kafelek "Klient" z polem "Nazwa" ze ścieżką "$.name" z połączenia "Klient"
And   istnieje kafelek "Kurs walut" z polem "Kurs" ze ścieżką "$.rate" z połączenia "Kurs walut" z transformacją round(2)

---

### Role: Użytkownik konfigurujący widoki danych

#### Scenario #1

Podgląd widoku zwraca układ i dane kafelków w kolejności pozycji

```
Given użytkownik zapisał widok "Pulpit klienta" z kafelkami "Kurs walut" na pozycji 1
      i "Klient" na pozycji 0 z klasą CSS "col-6"
When  użytkownik ogląda podgląd widoku z parametrami customer_id "42" i currency "EUR"
Then  podgląd pokazuje widok "Pulpit klienta"
And   pierwszy jest kafelek "Klient" na pozycji 0 z klasą CSS "col-6" i polem "Nazwa" o wartości "acme"
And   drugi jest kafelek "Kurs walut" na pozycji 1 bez klasy CSS i polem "Kurs" o wartości 4.32
```

#### Scenario #2

Podgląd z parametrami w adresie daje ten sam wynik

```
Given użytkownik zapisał widok "Pulpit klienta" z kafelkami "Klient" i "Kurs walut"
When  użytkownik ogląda podgląd widoku, podając customer_id "42" i currency "EUR" w adresie
Then  podgląd jest taki sam jak przy podaniu tych parametrów w treści żądania
```

#### Scenario #3

Brak wymaganego parametru dotyczy tylko kafelka, który go potrzebuje

```
Given użytkownik zapisał widok "Pulpit klienta" z kafelkami "Klient" i "Kurs walut"
When  użytkownik ogląda podgląd widoku tylko z parametrem currency "EUR"
Then  podgląd zostaje zwrócony
And   kafelek "Klient" ma błąd nieprawidłowych parametrów wskazujący brakujący parametr "customer_id" i nie ma pól
And   źródło "Klient" nie zostaje odpytane
And   kafelek "Kurs walut" ma pole "Kurs" o wartości 4.32
```

#### Scenario #4

Niedostępne źródło jednego kafelka nie blokuje pozostałych

```
Given użytkownik zapisał widok "Pulpit klienta" z kafelkami "Klient" i "Kurs walut"
And   źródło "Kurs walut" jest niedostępne
When  użytkownik ogląda podgląd widoku z parametrami customer_id "42" i currency "EUR"
Then  kafelek "Kurs walut" ma pole "Kurs" z błędem niedostępnego źródła
And   kafelek "Klient" ma pole "Nazwa" o wartości "acme"
```

#### Scenario #5

Każdy kafelek pobiera dane osobno

```
Given istnieje kafelek "Saldo" z polem "Saldo" ze ścieżką "$.balance" z połączenia "Klient"
And   użytkownik zapisał widok "Pulpit" z kafelkami "Klient" i "Saldo"
When  użytkownik ogląda podgląd widoku z parametrem customer_id "42"
Then  źródło "Klient" zostaje odpytane dwa razy
And   kafelek "Saldo" ma pole "Saldo" o wartości 1234.567
```

#### Scenario #6

Podgląd widoku bez kafelków

```
Given użytkownik zapisał widok "Pusty" bez kafelków
When  użytkownik ogląda podgląd widoku
Then  podgląd pokazuje widok "Pusty" z pustą listą kafelków
```

#### Scenario #7

Podgląd nieistniejącego widoku nie jest dostępny

```
When  użytkownik ogląda podgląd widoku, który nie istnieje
Then  użytkownik otrzymuje informację, że widok nie został znaleziony
```

#### Scenario #8

Podgląd nie zmienia widoku

```
Given użytkownik zapisał widok "Pulpit klienta" z kafelkiem "Klient"
When  użytkownik ogląda podgląd widoku z parametrem customer_id "42"
Then  szczegóły widoku pozostają bez zmian
```
