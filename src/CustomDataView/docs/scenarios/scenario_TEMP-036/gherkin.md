## Feature

Kafelki złożone: osadzone dzieci scope tile i wiązania z rodzicem

### Background

Given użytkownik konfiguruje kafelki dla widoków danych
And   istnieje połączenie "Klient" z wymaganym parametrem adresu "customer_id"
And   dla klienta "42" źródło "Klient" odpowiada danymi {"id": 7, "name": "acme", "country": "PL"}
And   istnieje połączenie "Zamówienia" z wymaganym parametrem zapytania "customer" i opcjonalnym parametrem
      zapytania "status"
And   istnieje połączenie "Przewoźnik" z wymaganym parametrem zapytania "country"
And   kafelek "Zamówienia" typu details ma źródło "orders" z polem "Liczba" ($.count)
And   kafelek "Przewoźnik" typu details ma źródło "carrier" z polem "Nazwa przewoźnika" ($.name)
And   kafelek "Dostawa" typu details ma źródło "orders" z polem "Status" ($.status) i dziecko "carrier"
      z kafelkiem "Przewoźnik"
And   kafelek "Filtr" typu form ma pole "Status" (text)

---

### Role: Użytkownik konfigurujący widoki danych

#### Scenario #1

Kafelek z polami i dziećmi zwraca osadzenia z adresem i stanem

```
Given kafelek "Klient 360" typu details ma źródło "customer" z polem "Nazwa" i ukrytym polem "ID" ($.id)
And   dziecko "orders" z kafelkiem "Zamówienia" na pozycji 1 z klasą "col-6" oraz dziecko "lazy-orders" z kafelkiem
      "Zamówienia" na pozycji 0 z ładowaniem lazy
And   wiązania "parent" → "orders" i "parent" → "lazy-orders" mapują pole "ID" na parametr "customer"
When  użytkownik ogląda podgląd kafelka "Klient 360" dla klienta "42"
Then  podgląd zawiera pole "Nazwa" i dzieci w kolejności pozycji
And   dziecko "lazy-orders" ma stan awaiting_load z wejściami {"parameters": {"customer": 7}}
And   dziecko "orders" ma adres "orders", klasę "col-6", stan data i zagnieżdżony podgląd kafelka "Zamówienia"
      z polem "Liczba"
And   źródło "Zamówienia" zostaje odpytane o klienta "7" jeden raz
```

#### Scenario #2

Formularz-dziecko filtruje połączenie rodzica

```
Given kafelek "Zamówienia z filtrem" typu details ma źródło "orders" z polem "Liczba", dziecko "filter" z kafelkiem
      "Filtr" i wiązanie "filter" → "orders" mapujące "Status" na parametr "status"
When  użytkownik ogląda podgląd z parametrem customer "7" i formularzem "filter" ze statusem "new"
Then  źródło "Zamówienia" zostaje odpytane o klienta "7" i status "new"
And   dziecko "filter" ma stan data z polem formularza "Status" o wartości "new"
```

#### Scenario #3

Wiązanie dziecko → dziecko i cel wskazany ścieżką do wnuka

```
Given kafelek "Klient i dostawa" ma dziecko "customer" z kafelkiem "Klient" i dziecko "shipping" z kafelkiem "Dostawa"
And   wiązanie "customer" → "shipping/carrier" mapuje pole "Kraj" na parametr "country"
And   wiązanie "customer" → "shipping" mapuje pole "ID" na parametr "customer"
When  użytkownik ogląda podgląd kafelka dla klienta "42"
Then  źródło "Przewoźnik" zostaje odpytane o kraj "PL"
And   wnuk ma adres "shipping/carrier" i stan data
```

#### Scenario #4

Wartości doręczane potomkom w żądaniu podglądu

```
When  użytkownik ogląda podgląd kafelka "Dostawa" z parametrem customer "7" i wartościami potomka "carrier"
      {"parameters": {"country": "DE"}}
Then  źródło "Przewoźnik" zostaje odpytane o kraj "DE"
```

#### Scenario #5

Walidacja osadzeń

```
When  użytkownik zapisuje kafelek z powtórzonym kluczem dziecka, kluczem równym kluczowi źródła, powtórzoną pozycją,
      nieistniejącym kafelkiem dziecka, zakresem item u rodzica bez elementów, nieznanym ładowaniem
      albo klasą CSS dłuższą niż 200 znaków
Then  zapis zostaje odrzucony z błędem walidacji wskazującym dziecko
```

#### Scenario #6

Cykle zawierania i limit trzech poziomów

```
Given kafelek "Dostawa" zawiera kafelek "Przewoźnik"
When  użytkownik edytuje kafelek "Przewoźnik", dodając dziecko "Dostawa"
Then  zapis zostaje odrzucony z błędem cyklu zawierania
When  użytkownik zapisuje kafelek "Pulpit" z dzieckiem "Klient i dostawa" (razem 4 poziomy)
Then  zapis zostaje odrzucony z błędem przekroczenia 3 poziomów
```

#### Scenario #7

Walidacja wiązań z dziećmi

```
When  użytkownik zapisuje kafelek z wiązaniem do parametru spoza kontraktu dziecka, z polem spoza wyjść dziecka,
      ze ścieżką do nieistniejącego wnuka, do parametru wnuka związanego już przez dziecko, z parą
      parent → źródło albo ze zbyt długą ścieżką
Then  zapis zostaje odrzucony z błędem walidacji wiązania
```

#### Scenario #8

Błąd i brak wartości u źródła wiązania dziecka

```
Given źródło "Klient" jest niedostępne
When  użytkownik ogląda podgląd kafelka "Klient 360"
Then  dziecko "orders" ma stan error dependency_failed z przyczyną: węzeł "customer", pole "ID", kod source_unavailable
When  formularz "filter" w kafelku "Zamówienia z filtrem" ma wymagane pole bez wartości
Then  pola źródła "orders" mają błąd awaiting_input wskazujący pole "Status" formularza "filter"
```

#### Scenario #9

Usunięcie kafelka osadzonego jako dziecko jest blokowane

```
When  użytkownik usuwa kafelek "Przewoźnik" osadzony w kafelku "Dostawa"
Then  usunięcie zostaje odrzucone (409 tile_in_use) z listą kafelków, w tym "Dostawa"
```

#### Scenario #10

Edycja pola używanego w wiązaniu rodzica jest blokowana

```
Given kafelek "Klient i dostawa" wiąże pole "Kraj" dziecka "customer"
When  użytkownik edytuje kafelek "Klient", zmieniając etykietę "Kraj" na "Państwo" albo usuwając to pole
Then  edycja zostaje odrzucona (409 field_in_use) z listą kafelków, w tym "Klient i dostawa"
When  użytkownik zmienia tylko ścieżkę pola "Kraj"
Then  edycja zostaje zapisana
```

#### Scenario #11

Zmiana kafelka dziecka unieważnia wiązanie w podglądzie

```
Given kafelek "Zamówienia" po edycji używa połączenia bez parametru "customer"
When  użytkownik ogląda podgląd kafelka "Klient 360"
Then  dziecko "orders" ma stan error invalid_binding wskazujący parametr "customer"
```
