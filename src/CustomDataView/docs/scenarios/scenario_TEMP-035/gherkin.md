## Feature

Kafelek formularz zbierający wartości od użytkownika

### Background

Given użytkownik konfiguruje kafelki dla widoków danych
And   kafelek "Filtry" typu form ma pola "Kraj" (text, wymagane), "Od" (date) i "Limit" (number, domyślnie 20)

---

### Role: Użytkownik konfigurujący widoki danych

#### Scenario #1

Zapis i szczegóły formularza

```
When  użytkownik zapisuje kafelek "Filtry"
Then  szczegóły zwracają typ "form" i pola z typem, wymaganiem i wartością domyślną
And   kontrakt nie ma wejść, a wyjścia to "Kraj", "Od" i "Limit"
```

#### Scenario #2

Walidacja formularza

```
When  użytkownik zapisuje formularz z polem bez typu, z nieznanym typem, z wartością domyślną niezgodną z typem,
      z polem wskazującym źródło lub ścieżkę, ze źródłami, elementami albo wiązaniami, albo z powtórzoną etykietą
Then  zapis zostaje odrzucony z błędem walidacji wskazującym pole albo sekcję formularza
```

#### Scenario #3

Podgląd formularza z wartościami

```
When  użytkownik ogląda podgląd formularza "Filtry" z wartościami "Kraj" = "PL" i "Od" = "2026-10-01"
Then  podgląd ma typ "form"
And   pole "Kraj" ma wartość "PL", pole "Od" wartość "2026-10-01", a pole "Limit" wartość domyślną 20
And   każde pole zwraca typ, wymaganie i wartość domyślną
```

#### Scenario #4

Wartość niezgodna z typem pola formularza

```
When  użytkownik ogląda podgląd formularza "Filtry" z wartością "Limit" = "abc" i "Od" = "01.10.2026"
Then  pola "Limit" i "Od" mają błąd invalid_value i zwracają podaną wartość
```

#### Scenario #5

Pola bez wartości

```
When  użytkownik ogląda podgląd formularza "Filtry" bez wartości
Then  pola "Kraj" i "Od" nie mają wartości, a pole "Limit" ma wartość domyślną 20
```
