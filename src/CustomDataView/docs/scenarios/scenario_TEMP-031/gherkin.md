## Feature

Walidacja parametrów osobno dla każdego połączenia, tuż przed jego wywołaniem

### Background

Given użytkownik konfiguruje kafelki dla widoków danych
And   istnieje połączenie "Klient" z wymaganym parametrem adresu "customer_id"
And   dla klienta "42" źródło "Klient" odpowiada danymi {"name": "acme"}
And   istnieje połączenie "Kurs walut" z opcjonalnym parametrem zapytania "currency"
And   dla waluty "EUR" źródło "Kurs walut" odpowiada danymi {"rate": 4.31789}
And   kafelek "Klient i kurs" ma pole "Nazwa" ze źródła "Klient" i pole "Kurs EUR" ze źródła "Kurs walut"

---

### Role: Użytkownik konfigurujący widoki danych

#### Scenario #1

Brak wymaganego parametru daje błąd tylko polom połączenia, które go wymaga

```
When  użytkownik ogląda podgląd kafelka "Klient i kurs", podając tylko walutę "EUR"
Then  podgląd zostaje zwrócony ze statusem 200
And   pole "Nazwa" ma błąd invalid_parameters wskazujący brakujący parametr "customer_id" spoza wiązania
And   pole "Kurs EUR" ma wartość 4.31789
And   odpytane zostaje tylko źródło "Kurs walut"
```

#### Scenario #2

Niepoprawna wartość parametru daje błąd pól połączenia bez jego wywołania

```
When  użytkownik ogląda podgląd kafelka "Klient i kurs", podając klienta jako obiekt i walutę "EUR"
Then  podgląd zostaje zwrócony ze statusem 200
And   pole "Nazwa" ma błąd invalid_parameters wskazujący niepoprawny parametr "customer_id"
And   źródło "Klient" nie zostaje odpytane
```

#### Scenario #3

Podgląd niezapisanego kafelka i podgląd metodą GET stosują tę samą zasadę

```
When  użytkownik ogląda podgląd niezapisanego kafelka "Klient i kurs" albo podgląd metodą GET tylko z walutą "EUR"
Then  podgląd zostaje zwrócony ze statusem 200
And   pole "Nazwa" ma błąd invalid_parameters, a pole "Kurs EUR" ma wartość
```

#### Scenario #4

Podgląd widoku zwraca błąd parametrów w polach, a nie w całym kafelku (wycofany: widoki usunięte, TEMP-038)

```
Given użytkownik zapisał widok "Pulpit klienta" z kafelkiem "Klient i kurs"
When  użytkownik ogląda podgląd widoku
Then  podgląd zostaje zwrócony ze statusem 200
And   kafelek nie ma błędu całego kafelka
And   pole "Nazwa" ma błąd invalid_parameters
```
