## Feature

Wejścia danych kafelka i wartości testowe w żądaniu podglądu

### Background

Given użytkownik konfiguruje kafelki dla widoków danych
And   istnieje połączenie "Klient" z wymaganym parametrem adresu "customer_id"
And   dla klienta "42" źródło "Klient" odpowiada danymi {"name": "acme"}
And   kafelek "Pozycje zamówienia" typu table ma wejście danych "rows", elementy z "rows" pod ścieżką "$[*]"
      z kluczem "SKU" oraz pola "SKU" ($.sku) i "Ilość" ($.qty)

---

### Role: Użytkownik konfigurujący widoki danych

#### Scenario #1

Podgląd kafelka z wartością wejścia danych

```
When  użytkownik ogląda podgląd kafelka "Pozycje zamówienia" z wejściem "rows"
      [{"sku": "A-1", "qty": 2}, {"sku": "B-7", "qty": 1}]
Then  podgląd zwraca elementy "A-1" z ilością 2 i "B-7" z ilością 1
And   żadne zewnętrzne źródło nie zostaje odpytane
```

#### Scenario #2

Brak wartości wejścia danych

```
When  użytkownik ogląda podgląd kafelka "Pozycje zamówienia" bez wejścia "rows"
Then  podgląd ma błąd elementów invalid_parameters wskazujący wejście "rows"
```

#### Scenario #3

Kontrakt zawiera wejścia danych

```
When  użytkownik ogląda szczegóły kafelka "Pozycje zamówienia"
Then  kontrakt zawiera wejście danych "rows", nie ma wejść-parametrów i ma wyjścia "SKU" i "Ilość" z elementami
```

#### Scenario #4

Walidacja źródła input

```
When  użytkownik zapisuje kafelek ze źródłem input wskazującym połączenie albo ze źródłem nieznanego rodzaju
Then  zapis zostaje odrzucony z błędem walidacji źródła
```

#### Scenario #5

Kafelek miesza wejście danych i połączenie

```
Given kafelek "Klient z notatką" typu details ma źródło "customer" (połączenie "Klient") z polem "Nazwa" i wejście
      danych "note" z polem "Notatka" ($.text)
When  użytkownik ogląda podgląd kafelka z parametrem customer_id "42" i wejściem "note" {"text": "VIP"}
Then  pole "Nazwa" ma wartość "acme", a pole "Notatka" wartość "VIP"
```
