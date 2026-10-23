## Feature

Wyszukiwarka z zakładkami filtrów, stronicowaną tabelą wyników i przejściem do kafelka szczegółów

### Background

Given użytkownik konfiguruje kafelki dla widoków danych
And   istnieje połączenie "Szukaj miast" z opcjonalnymi parametrami zapytania "name" i "country" oraz parametrami
      "pageNumber" i "pageSize", które dla nazwy "Kra" odpowiada stroną
      {"total": 12, "cities": [{"id": 1, "name": "Kraków"}, … 10 miast]} na stronie 1 i 2 miastami na stronie 2
And   istnieje połączenie "Miasto" z wymaganym parametrem ścieżki "cityId", które odpowiada danymi
      {"id": 1, "name": "Kraków", "population": 800000}
And   kafelek "Nazwa miasta" typu form ma pole "Miasto" (text)
And   kafelek "Kraj" typu form ma pole "Kraj" (text)
And   kafelek "Miasto" typu details ma źródło "city" z połączenia "Miasto" i pola "Nazwa" oraz "Ludność"
And   kafelek "Wyniki" typu pagination_table ma elementy ze źródła "Szukaj miast" pod "$.cities[*]" z kluczem "ID",
      pola "ID" i "Miasto", stronicowanie po 10 elementów z łączną liczbą pod "$.total" oraz przycisk "Szczegóły"
      przy każdym elemencie, który otwiera kafelek "Miasto" z polem "ID" jako "cityId"

---

### Role: Użytkownik konfigurujący widoki danych

#### Scenario #1

Zakładki są zwracane z tytułami w kolejności, a zakładka ładowana na żądanie czeka

```
Given kafelek "Filtry" typu tabs ma zakładki "Po nazwie" (Nazwa miasta) i "Po kraju" (Kraj, ładowana na żądanie)
When  użytkownik ogląda podgląd kafelka "Filtry"
Then  kafelek ma zakładki "Po nazwie" i "Po kraju" w tej kolejności
And   zakładka "Po nazwie" pokazuje formularz z polem "Miasto"
And   zakładka "Po kraju" czeka na doładowanie
```

#### Scenario #2

Pole formularza z zakładki zasila kafelek obok zakładek

```
Given kafelek "Wyszukiwarka miast" ma dzieci "filters" (Filtry) i "results" (Wyniki)
And   wiązanie z formularza zakładki "filters/byName" przekazuje pole "Miasto" do parametru "name" wyników
When  użytkownik wpisuje "Kra" w polu "Miasto" zakładki "Po nazwie" i ogląda podgląd kafelka "Wyszukiwarka miast"
Then  źródło "Szukaj miast" zostaje odpytane o nazwę "Kra"
And   wyniki pokazują pierwszą stronę miast
```

#### Scenario #3

Wymagane pole formularza w zakładce wstrzymuje kafelek obok zakładek

```
Given pole "Miasto" formularza "Nazwa miasta" jest wymagane
When  użytkownik ogląda podgląd kafelka "Wyszukiwarka miast" bez wpisanego miasta
Then  wyniki czekają na wartość pola "Miasto" formularza zakładki "filters/byName"
And   źródło "Szukaj miast" nie zostaje odpytane
```

#### Scenario #4

Zakładki dostają wartości od rodzica i od innych zakładek

```
Given zakładka "Pogoda" kafelka "Filtry" pokazuje kafelek z wymaganym parametrem "cityId"
And   kafelek "Wyszukiwarka miast" przekazuje swoje pole "Domyślne miasto" do zakładki "filters/weather"
When  użytkownik ogląda podgląd kafelka "Wyszukiwarka miast"
Then  zakładka "Pogoda" zostaje wczytana dla domyślnego miasta
And   zakładka zasilana wiązaniem z innej zakładki kafelka "Filtry" dostaje jej wartość
```

#### Scenario #5

Niepoprawny kafelek z zakładkami odrzuca zapis

```
Scenario Outline: odrzucenie zakładek
Given użytkownik definiuje kafelek typu tabs, który <problem>
When  użytkownik zapisuje kafelek
Then  zapis zostaje odrzucony z błędem walidacji <miejsce>

Examples:
  | problem                                       | miejsce                |
  | nie ma zakładek                               | zakładek               |
  | ma zakładkę bez tytułu                        | tytułu zakładki        |
  | ma zakładkę osadzoną w każdym elemencie       | zakresu zakładki       |
  | ma własne pola                                | pól                    |
  | ma własne źródła danych                       | źródeł                 |
```

#### Scenario #6

Wiązanie z pola potomka wymaga istniejącego osadzenia i pola

```
Scenario Outline: odrzucenie ścieżki źródła wiązania
Given kafelek "Wyszukiwarka miast" ma wiązanie z <źródło> z polem <pole> do wyników
When  użytkownik zapisuje kafelek
Then  zapis zostaje odrzucony z błędem walidacji <miejsce>

Examples:
  | źródło              | pole     | miejsce                     |
  | "filters/missing"   | "Miasto" | źródła wiązania             |
  | "filters/byName"    | "Ulica"  | pola mapowania              |
  | "results/byName"    | "Miasto" | źródła wiązania             |
```

#### Scenario #7

Kafelki można zagnieżdżać bez limitu poziomów, ale bez cykli

```
Given kafelki tworzą łańcuch osadzeń na 4 poziomach
When  użytkownik zapisuje kafelek najwyższego poziomu
Then  kafelek zostaje zapisany
When  użytkownik osadza kafelek najwyższego poziomu w kafelku z najniższego poziomu
Then  zapis zostaje odrzucony z błędem cyklu zawierania
```

#### Scenario #8

Pole używane przez wiązanie przodka przez ścieżkę nie może zniknąć

```
Given wiązanie kafelka "Wyszukiwarka miast" używa pola "Miasto" formularza zakładki "filters/byName"
When  użytkownik usuwa pole "Miasto" z formularza "Nazwa miasta"
Then  edycja zostaje zablokowana z listą pól "Miasto" i kafelków "Wyszukiwarka miast"
```

#### Scenario #9

Pierwsza strona wyników z łączną liczbą i następną stroną

```
When  użytkownik ogląda podgląd kafelka "Wyniki" dla nazwy "Kra"
Then  źródło "Szukaj miast" zostaje odpytane o stronę 1 po 10 elementów
And   wyniki pokazują 10 miast ze stroną 1 z 2 i łączną liczbą 12
And   wyniki mają następną stronę z nazwą "Kra" i stroną 2 i nie mają poprzedniej
```

#### Scenario #10

Front przechodzi na następną stronę

```
When  front ogląda podgląd kafelka "Wyniki" z wartościami następnej strony
Then  źródło "Szukaj miast" zostaje odpytane o nazwę "Kra" i stronę 2 po 10 elementów
And   wyniki pokazują 2 miasta ze stroną 2 z 2
And   wyniki mają poprzednią stronę 1 i nie mają następnej
```

#### Scenario #11

Bez łącznej liczby następna strona istnieje, gdy strona jest pełna

```
Scenario Outline: następna strona bez łącznej liczby
Given stronicowanie kafelka "Wyniki" nie ma łącznej liczby elementów
And   źródło "Szukaj miast" zwraca <liczba> miast na stronie 1
When  użytkownik ogląda podgląd kafelka "Wyniki"
Then  wyniki <następna> następną stronę i nie znają liczby stron

Examples:
  | liczba | następna |
  | 10     | mają     |
  | 4      | nie mają |
```

#### Scenario #12

Nazwy parametrów stronicowania i rozmiar strony są konfigurowalne

```
Given połączenie "Szukaj miast" przyjmuje stronę w parametrze "page" i rozmiar w parametrze "limit"
And   stronicowanie kafelka "Wyniki" używa parametrów "page" i "limit"
When  front ogląda podgląd kafelka "Wyniki" z rozmiarem strony 5
Then  źródło "Szukaj miast" zostaje odpytane o stronę 1 w parametrze "page" i 5 elementów w parametrze "limit"
And   wyniki mają rozmiar strony 5
```

#### Scenario #13

Niepoprawne stronicowanie odrzuca zapis

```
Scenario Outline: odrzucenie stronicowania
Given użytkownik definiuje kafelek typu pagination_table, którego <problem>
When  użytkownik zapisuje kafelek
Then  zapis zostaje odrzucony z błędem walidacji stronicowania

Examples:
  | problem                                                  |
  | stronicowanie nie zostało podane                         |
  | rozmiar strony wynosi 0                                  |
  | rozmiar strony wynosi 101                                |
  | parametr numeru strony nie jest zadeklarowany w połączeniu |
  | parametry numeru i rozmiaru strony mają tę samą nazwę    |
  | ścieżka łącznej liczby zawiera wildcard                  |
  | źródło elementów jest wejściem danych                    |
```

#### Scenario #14

Stronicowanie jest dostępne tylko w tabeli ze stronicowaniem

```
When  użytkownik zapisuje kafelek typu table ze stronicowaniem
Then  zapis zostaje odrzucony z błędem walidacji stronicowania
```

#### Scenario #15

Numer strony spoza zakresu daje błąd elementów

```
Scenario Outline: niepoprawna strona
When  front ogląda podgląd kafelka "Wyniki" ze stroną <strona>
Then  wyniki mają błąd elementów z niepoprawnym parametrem "pageNumber"
And   źródło "Szukaj miast" nie zostaje odpytane

Examples:
  | strona |
  | 0      |
  | "abc"  |
  | 1.5    |
```

#### Scenario #16

Przycisk przy każdym elemencie niesie wartości dla kafelka docelowego

```
When  użytkownik ogląda podgląd kafelka "Wyniki" dla nazwy "Kra"
Then  element "1" ma przycisk "Szczegóły" prowadzący do kafelka "Miasto" z wartością "cityId" = 1
And   każdy element ma przycisk z własną wartością "cityId"
```

#### Scenario #17

Front otwiera kafelek docelowy z wartościami przycisku

```
When  front ogląda podgląd kafelka "Miasto" z wartościami przycisku elementu "1"
Then  źródło "Miasto" zostaje odpytane o miasto 1
And   kafelek "Miasto" pokazuje nazwę "Kraków"
```

#### Scenario #18

Link na poziomie kafelka

```
Given kafelek "Miasto" ma link "Wszystkie miasta" do kafelka "Wyniki" z polem "Nazwa" jako "name"
When  użytkownik ogląda podgląd kafelka "Miasto" dla miasta 1
Then  kafelek ma link "Wszystkie miasta" prowadzący do kafelka "Wyniki" z wartością "name" = "Kraków"
```

#### Scenario #19

Błąd pola albo zmiana kafelka docelowego dają akcję z błędem

```
Given pole "ID" elementów kafelka "Wyniki" wskazuje nieistniejącą wartość
When  użytkownik ogląda podgląd kafelka "Wyniki"
Then  przycisk "Szczegóły" każdego elementu ma błąd zależności od pola "ID"
When  kafelek "Miasto" przestaje przyjmować parametr "cityId" i użytkownik ogląda podgląd kafelka "Wyniki"
Then  przycisk "Szczegóły" ma błąd niepoprawnego wiązania parametru "cityId"
```

#### Scenario #20

Niepoprawna akcja odrzuca zapis

```
Scenario Outline: odrzucenie akcji
Given użytkownik definiuje akcję kafelka, która <problem>
When  użytkownik zapisuje kafelek
Then  zapis zostaje odrzucony z błędem walidacji akcji

Examples:
  | problem                                                  |
  | prowadzi do nieistniejącego kafelka                      |
  | mapuje nieistniejące pole                                |
  | przekazuje parametr, którego kafelek docelowy nie przyjmuje |
  | ma typ inny niż przycisk albo link                       |
  | jest przy elementach kafelka bez elementów               |
  | należy do formularza                                     |
  | ma ten sam klucz co inna akcja                           |
  | nie ma etykiety                                          |
```

#### Scenario #21

Kafelek docelowy akcji nie może zostać usunięty

```
When  użytkownik usuwa kafelek "Miasto"
Then  usunięcie zostaje zablokowane z listą kafelków "Wyniki"
```

#### Scenario #22

Przepływ wyszukiwarki od zakładki do nowego korzenia

```
Given kafelek "Wyszukiwarka miast" ma zakładki filtrów i wyniki pod zakładkami
When  użytkownik wybiera zakładkę "Po nazwie", wpisuje "Kra" i ogląda podgląd kafelka "Wyszukiwarka miast"
Then  wyniki pokazują pierwszą stronę miast z przyciskami "Szczegóły" i następną stroną
When  front ogląda następną stronę wyników
Then  wyniki pokazują drugą stronę miast
When  użytkownik klika przycisk "Szczegóły" miasta "1"
Then  front ogląda podgląd kafelka "Miasto" jako nowego korzenia z danymi miasta "Kraków"
```
