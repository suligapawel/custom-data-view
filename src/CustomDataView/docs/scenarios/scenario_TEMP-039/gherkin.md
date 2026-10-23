## Feature

Wybór elementu w kafelku z kolekcją zasila inne kafelki (master–detail)

### Background

Given użytkownik konfiguruje kafelki dla widoków danych
And   istnieje połączenie "Miasta", które odpowiada danymi {"cities": [{"id": 1, "name": "Kraków"}, {"id": 2, "name": "Gdańsk"}]}
And   istnieje połączenie "Pogoda" z wymaganymi parametrami zapytania "cityId" i "units"
And   kafelek "Miasta" typu table ma elementy pod "$.cities[*]" z kluczem "ID" i pola "ID" oraz "Miasto"
And   kafelek "Pogoda" typu details ma źródło "weather" z polem "Temperatura"
And   kafelek "Jednostki" typu form ma pole "Jednostki" (text, domyślnie "metric")

---

### Role: Użytkownik konfigurujący widoki danych

#### Scenario #1

Cel wiązania selected czeka na wybór z kompletem pozostałych wejść

```
Given kafelek "Miasta i pogoda" ma dzieci "cities" (Miasta), "units" (Jednostki) i "weather" (Pogoda)
And   wiązanie "cities" → "weather" mapuje "ID" na "cityId" z zakresem selected
And   wiązanie "units" → "weather" mapuje "Jednostki" na "units"
When  użytkownik ogląda podgląd kafelka "Miasta i pogoda"
Then  dziecko "weather" ma stan awaiting_input
And   pending wskazuje węzeł "cities", pole "ID", zakres selected i parametr "cityId"
And   wejścia zawierają parametr "units" = "metric"
And   źródło "Pogoda" nie zostaje odpytane
```

#### Scenario #2

Front doładowuje cel po wyborze elementu

```
When  front ogląda podgląd kafelka "Pogoda" z wejściami z odpowiedzi i wybranym "cityId" = 2
Then  źródło "Pogoda" zostaje odpytane o miasto "2" w jednostkach "metric"
```

#### Scenario #3

Brak zakresu w polu kafelka z elementami oznacza selected

```
Given wiązanie "cities" → "weather" nie podaje zakresu
When  użytkownik ogląda podgląd kafelka "Miasta i pogoda"
Then  dziecko "weather" ma stan awaiting_input z pending o zakresie selected
```

#### Scenario #4

Zakres selected jest dozwolony tylko dla węzła z elementami

```
When  użytkownik zapisuje wiązanie z zakresem selected z dziecka "units" bez elementów
Then  zapis zostaje odrzucony z błędem walidacji zakresu
```
