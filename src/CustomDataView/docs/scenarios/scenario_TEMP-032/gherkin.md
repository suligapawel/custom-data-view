## Feature

Typ wyświetlania kafelka, role pól i elementy kolekcji

### Background

Given użytkownik konfiguruje kafelki dla widoków danych
And   istnieje połączenie "Miasta" z opcjonalnym parametrem zapytania "country"
And   źródło "Miasta" odpowiada danymi {"cities": [{"id": 1, "name": "Kraków", "temp": 21.456},
      {"id": 2, "name": "Gdańsk", "temp": 17.04}]}
And   istnieje połączenie "Klient" z wymaganym parametrem adresu "customer_id"
And   dla klienta "42" źródło "Klient" odpowiada danymi {"name": "acme", "balance": 1234.567}

---

### Role: Użytkownik konfigurujący widoki danych

#### Scenario #1

Typ wyświetlania jest wymagany i ma dozwoloną wartość

```
When  użytkownik zapisuje kafelek bez typu wyświetlania albo z typem "chart"
Then  zapis zostaje odrzucony z błędem walidacji "display"
```

#### Scenario #2

Kafelek details zwraca pola i swój typ wyświetlania

```
Given użytkownik zapisał kafelek "Klient" typu details z polem "Nazwa"
When  użytkownik ogląda szczegóły i podgląd kafelka dla klienta "42"
Then  szczegóły i podgląd mają typ wyświetlania "details"
And   podgląd zwraca pole "Nazwa" o wartości "acme"
```

#### Scenario #3

Kafelek table zwraca elementy z kluczem i polami względnymi wobec elementu

```
Given użytkownik zapisał kafelek "Miasta" typu table ze źródłem "cities" i elementami ze źródła "cities"
      pod ścieżką "$.cities[*]" z kluczem "ID"
And   kafelek ma pola "ID" ($.id), "Miasto" ($.name) i "Temperatura" ($.temp z round(1))
When  użytkownik ogląda podgląd kafelka
Then  podgląd ma typ "table" i klucz elementu "ID"
And   pierwszy element ma klucz 1 i pola "ID" = 1, "Miasto" = "Kraków", "Temperatura" = 21.5
And   drugi element ma klucz 2 i pola "ID" = 2, "Miasto" = "Gdańsk", "Temperatura" = 17
And   podgląd nie zawiera listy pól poza elementami
```

#### Scenario #4

Kafelek list wymaga pola z rolą title, a role muszą pasować do typu wyświetlania

```
When  użytkownik zapisuje kafelek typu list bez pola z rolą title, kafelek typu details z polem z rolą title
      albo kafelek typu list z polem z rolą primary
Then  zapis zostaje odrzucony z błędem walidacji wskazującym pola albo rolę pola
```

#### Scenario #5

Kafelek summary wymaga co najmniej jednego pola z rolą primary

```
When  użytkownik zapisuje kafelek typu summary z polami bez roli primary
Then  zapis zostaje odrzucony z błędem walidacji pól
When  użytkownik zapisuje kafelek typu summary z polem "Saldo" z rolą primary i polem "Nazwa" z rolą secondary
Then  kafelek zostaje zapisany, a szczegóły zwracają role pól
```

#### Scenario #6

Walidacja elementów

```
When  użytkownik zapisuje kafelek typu table bez elementów, kafelek typu details z elementami, elementy
      z nieistniejącym źródłem, ścieżką bez wildcardu albo kluczem spoza etykiet pól, albo widoczne pole spoza
      źródła elementów
Then  zapis zostaje odrzucony z błędem walidacji wskazującym elementy albo pole
```

#### Scenario #7

Ukryte pole spoza źródła elementów jest dozwolone

```
When  użytkownik zapisuje kafelek typu table z elementami ze źródła "cities" i ukrytym polem "Klient" ze
      źródła "customer"
Then  kafelek zostaje zapisany
```

#### Scenario #8

Błąd źródła elementów zwraca błąd elementów

```
Given źródło "Miasta" jest niedostępne
When  użytkownik ogląda podgląd kafelka "Miasta"
Then  podgląd zawiera błąd elementów source_unavailable zamiast elementów
```

#### Scenario #9

Podgląd widoku zwraca elementy kafelka z kolekcją bez pól ukrytych (wycofany: widoki usunięte, TEMP-038)

```
Given kafelek "Miasta" ma ukryte pole "Temperatura"
And   użytkownik zapisał widok "Pulpit" z kafelkiem "Miasta"
When  użytkownik ogląda podgląd widoku
Then  kafelek "Miasta" ma typ "table" i elementy z polami "ID" i "Miasto"
```
