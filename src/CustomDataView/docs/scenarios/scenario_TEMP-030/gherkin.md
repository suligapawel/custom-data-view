## Feature

Unikalne etykiety pól kafelka i pola ukryte

### Background

Given użytkownik konfiguruje kafelki dla widoków danych
And   istnieje połączenie "Klient" z wymaganym parametrem adresu "customer_id"
And   dla klienta "42" źródło "Klient" odpowiada danymi {"name": "acme", "balance": 1234.567}

---

### Role: Użytkownik konfigurujący widoki danych

#### Scenario #1

Etykieta powtórzona z inną wielkością liter lub białymi znakami jest odrzucana

```
Given użytkownik przygotował kafelek "Klient" z polami "Nazwa" i " nazwa "
When  użytkownik zapisuje kafelek albo edytuje nim istniejący kafelek
Then  zapis zostaje odrzucony z błędem walidacji etykiety drugiego pola
And   nic nie zostaje zapisane
```

#### Scenario #2

Flaga ukrycia jest zapisywana i zwracana w szczegółach

```
Given użytkownik zapisał kafelek "Klient" z polem "Nazwa" i ukrytym polem "Saldo"
When  użytkownik ogląda szczegóły kafelka
Then  pole "Nazwa" ma hidden równe false
And   pole "Saldo" ma hidden równe true
```

#### Scenario #3

Podgląd kafelka zwraca pole ukryte po transformacjach

```
Given użytkownik zapisał kafelek "Klient" z polem "Nazwa" i ukrytym polem "Saldo" z transformacją round(1)
When  użytkownik ogląda podgląd kafelka dla klienta "42"
Then  pole "Nazwa" ma wartość "acme" bez oznaczenia hidden
And   pole "Saldo" ma wartość 1234.6 i oznaczenie hidden równe true
```

#### Scenario #4

Podgląd widoku pomija pola ukryte (wycofany: widoki usunięte, TEMP-038)

```
Given użytkownik zapisał kafelek "Klient" z polem "Nazwa" i ukrytym polem "Saldo"
And   użytkownik zapisał widok "Pulpit" z kafelkiem "Klient"
When  użytkownik ogląda podgląd widoku
Then  kafelek "Klient" ma tylko pole "Nazwa"
```
