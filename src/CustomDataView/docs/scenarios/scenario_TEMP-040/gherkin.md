## Feature

Filtrowanie elementów tablicy w ścieżce JSONPath

### Background

Given użytkownik konfiguruje kafelki dla widoków danych
And   istnieje połączenie "Miasta", które odpowiada danymi
      {"cities": [{"id": 1, "name": "Kraków", "example": {"_type": "capital"}},
                  {"id": 2, "name": "Gdańsk", "example": {"_type": "port"}},
                  {"id": 3, "name": "Sopot"}]}

---

### Role: Użytkownik konfigurujący widoki danych

#### Scenario #1

Pole z filtrem zwraca wartości pasujących elementów

```
Given kafelek "Porty" ma pole "Miasto" ze ścieżką "$.cities[?@.example._type == 'port'].name"
When  użytkownik ogląda podgląd kafelka "Porty"
Then  pole "Miasto" ma wartość ["Gdańsk"]
```

#### Scenario #2

Elementy kafelka table są filtrowane, a brak wartości spełnia warunek !=

```
Given kafelek "Miasta" typu table ma elementy pod "$.cities[?(@.example._type != 'capital')]" z kluczem "ID"
      i pola "ID" oraz "Miasto"
When  użytkownik ogląda podgląd kafelka "Miasta"
Then  kafelek ma elementy "2" (Gdańsk) i "3" (Sopot)
```

#### Scenario #3

Filtr porównuje liczby, wartości logiczne i null według typu

```
Scenario Outline: porównanie literału
Given kafelek "Miasto" ma pole "Miasto" ze ścieżką <path>
When  użytkownik ogląda podgląd kafelka "Miasto"
Then  pole "Miasto" ma wartość <value>

Examples:
  | path                                  | value             |
  | "$.cities[?@.id == 2].name"           | ["Gdańsk"]        |
  | "$.cities[?@.id == 2.0].name"         | ["Gdańsk"]        |
  | "$.cities[?@.id == '2'].name"         | []                |
  | "$.cities[?@.id != 1].name"           | ["Gdańsk","Sopot"]|
  | "$.cities[?@.example == null].name"   | []                |
  | "$.cities[?@.missing == true].name"   | []                |
```

#### Scenario #4

Niepoprawny filtr odrzuca zapis kafelka

```
Scenario Outline: odrzucenie filtra
Given użytkownik definiuje kafelek, którego pierwsze pole ma ścieżkę <path>
When  użytkownik zapisuje kafelek
Then  zapis zostaje odrzucony z informacją o błędzie ścieżki pierwszego pola

Examples:
  | path                          |
  | "$.cities[?@.id]"             |
  | "$.cities[?@.id > 1]"         |
  | "$.cities[?@.name == 'Gda]"   |
  | "$.cities[?@.name == Gdańsk]" |
  | "$.cities[?@.* == 1]"         |
  | "$.cities[?(@.id == 1]"       |
  | "$.cities[?$.id == 1]"        |
```
