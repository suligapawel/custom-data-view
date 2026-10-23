## Feature

Dzieci osadzone w każdym elemencie rodzica i limity podglądu

### Background

Given użytkownik konfiguruje kafelki dla widoków danych
And   istnieje połączenie "Zamówienia" bez parametrów, które odpowiada danymi
      {"orders": [{"id": 17, "lines": [{"sku": "A-1", "qty": 2}]}, {"id": 18, "lines": []}]}
And   istnieje połączenie "Dostawa" z wymaganym parametrem zapytania "order"
And   kafelek "Pozycje zamówienia" typu table ma wejście danych "rows" i elementy z kluczem "SKU"
And   kafelek "Dostawa" typu details ma źródło "shipping" z polem "Status" ($.status)
And   kafelek "Zamówienia" typu table ma elementy ze źródła "orders" pod ścieżką "$.orders[*]" z kluczem "ID",
      pola "ID" ($.id) i ukryte "Linie" ($.lines), dzieci "lines" (Pozycje zamówienia, item, eager) i "shipping"
      (Dostawa, item, eager) oraz wiązania "parent" → "lines" ("Linie" → wejście "rows", item) i "parent" →
      "shipping" ("ID" → parametr "order", item)

---

### Role: Użytkownik konfigurujący widoki danych

#### Scenario #1

Każdy element ma własne instancje dzieci z adresem zawierającym klucz elementu

```
When  użytkownik ogląda podgląd kafelka "Zamówienia"
Then  element 17 ma dziecko "lines" pod adresem "[17]/lines" z pozycją "A-1" i dziecko "shipping" pod adresem
      "[17]/shipping" z danymi dostawy zamówienia 17
And   element 18 ma dziecko "lines" bez pozycji i dziecko "shipping" z danymi dostawy zamówienia 18
```

#### Scenario #2

Dziecko lazy w elemencie czeka na doładowanie z kompletem wejść

```
Given dziecko "shipping" ma ładowanie lazy
When  użytkownik ogląda podgląd kafelka "Zamówienia"
Then  dziecko "shipping" elementu 17 ma stan awaiting_load z wejściami {"parameters": {"order": 17}}
And   źródło "Dostawa" nie zostaje odpytane
```

#### Scenario #3

Walidacja zakresu item

```
When  użytkownik zapisuje wiązanie z zakresem item do dziecka o zakresie tile, z zakresem item z pola dziecka
      albo wiązanie z dziecka o zakresie item do dziecka o zakresie tile
Then  zapis zostaje odrzucony z błędem walidacji wiązania
```

#### Scenario #4

Limit 50 elementów na osadzenie eager

```
Given źródło "Zamówienia" zwraca 51 zamówień
When  użytkownik ogląda podgląd kafelka "Zamówienia"
Then  dzieci "shipping" pierwszych 50 elementów mają stan data
And   dziecko "shipping" 51. elementu ma stan awaiting_load z wejściami
```

#### Scenario #5

Limit 200 wywołań zewnętrznych na żądanie

```
Given kafelek ma 50 elementów i 5 dzieci o zakresie item, z których każde wywołuje własne połączenie
When  użytkownik ogląda podgląd kafelka
Then  wykonanych zostaje 200 wywołań zewnętrznych
And   pozostałe osadzenia mają stan awaiting_load
```

#### Scenario #6

Limit czasu całego żądania

```
Given wywołanie źródła pierwszego dziecka trwa dłużej niż 10 s
When  użytkownik ogląda podgląd kafelka z dzieckiem zależnym od pierwszego dziecka
Then  dziecko zależne ma stan awaiting_load
```

#### Scenario #7

Ograniczona równoległość i deduplikacja wywołań

```
Given 20 elementów wywołuje dziecko z własnym połączeniem
When  użytkownik ogląda podgląd kafelka
Then  jednocześnie trwa najwyżej 8 wywołań instancji dzieci
And   dwa osadzenia tego samego kafelka z tymi samymi wartościami parametrów wywołują połączenie raz
```

#### Scenario #8

Rodzeństwo w elemencie wiąże się z instancją tego samego elementu

```
Given dziecko "lines" przekazuje pole "SKU" (all) do parametru dziecka "stock" w tym samym elemencie
When  użytkownik ogląda podgląd kafelka "Zamówienia"
Then  dziecko "stock" elementu 17 odpytuje stan dla listy ["A-1"]
And   dziecko "stock" elementu 18 dostaje pustą listę, więc jego pole ma błąd invalid_parameters
```
