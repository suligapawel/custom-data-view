## Feature

Kombinatory pól: pole kafelka złożone z kilku innych pól

### Background

Given użytkownik konfiguruje kafelki dla widoków danych
And   istnieje połączenie "Miejsce", które odpowiada danymi
      {"street": "Długa", "number": "5", "city": "Kraków", "lat": 50.0647, "lon": 19.9461,
       "email": null, "phone": "123 456 789", "tags": ["zabytki", null, "rynek"]}
And   kafelek "Adres" typu details ma źródło "api" z połączenia "Miejsce" i ukryte pola
      "Ulica" ($.street), "Nr" ($.number), "Miasto" ($.city), "Lat" ($.lat zaokrąglone do 2 miejsc)
      i "Lon" ($.lon zaokrąglone do 2 miejsc)
And   istnieje połączenie "Miasta", które odpowiada danymi
      {"cities": [{"id": 1, "name": "Kraków", "country": "PL"}, {"id": 2, "name": "Praga", "country": "CZ"}]}

---

### Role: Użytkownik konfigurujący widoki danych

#### Scenario #1

Katalog kombinatorów opisuje dostępne kombinatory

```
When  użytkownik odczytuje katalog kombinatorów
Then  katalog zawiera kombinatory "join", "template", "object" i "coalesce" z nazwą i opisem
And   "join" ma opcjonalny parametr "separator" i wymaga co najmniej 1 pola
And   "template" ma wymagany parametr "pattern" i nie przyjmuje listy pól
And   "object" nie ma parametru i wymaga co najmniej 1 pola
And   "coalesce" nie ma parametru i wymaga co najmniej 2 pól
```

#### Scenario #2

Szablon składa adres z ukrytych pól, a wynik przechodzi przez transformacje

```
Given kafelek "Adres" ma pole "Pełny adres" ze wzorca "{Ulica} {Nr}, {Miasto}"
And   kafelek "Adres" ma pole "Adres wielkimi" ze wzorca "{ulica} {Nr}, {Miasto}" z transformacją na wielkie litery
When  użytkownik ogląda podgląd kafelka "Adres"
Then  pole "Pełny adres" ma wartość "Długa 5, Kraków"
And   pole "Adres wielkimi" ma wartość "DŁUGA 5, KRAKÓW"
And   pola "Ulica", "Nr" i "Miasto" są zwracane jako ukryte
```

#### Scenario #3

Brak wartości w szablonie daje pusty tekst, a podwójne klamry są dosłowne

```
Given połączenie "Miejsce" nie zwraca numeru domu (wartość null)
And   kafelek "Adres" ma pole "Etykieta" ze wzorca "{{{Ulica} {Nr}}}"
When  użytkownik ogląda podgląd kafelka "Adres"
Then  pole "Etykieta" ma wartość "{Długa }"
```

#### Scenario #4

Złączenie pomija brakujące wartości i rozkłada listy

```
Scenario Outline: złączenie pól
Given kafelek "Adres" ma pole "Tagi" ze ścieżką "$.tags[*]"
And   kafelek "Adres" ma pole "Złączone" łączące pola <pola> separatorem ", "
When  użytkownik ogląda podgląd kafelka "Adres"
Then  pole "Złączone" ma wartość <wartość>

Examples:
  | pola                      | wartość                        |
  | "Ulica", "Nr", "Miasto"   | "Długa, 5, Kraków"             |
  | "Miasto", "Tagi"          | "Kraków, zabytki, rynek"       |
  | "Lat", "Lon"              | "50.06, 19.95"                 |
```

#### Scenario #5

Złączenie samych brakujących wartości nie ma wartości, a obiekt nie da się złączyć

```
Given kafelek "Adres" ma pole "Email" ($.email) i pole "Bez wartości" łączące tylko pole "Email"
And   kafelek "Adres" ma pole "Całość" ($) i pole "Błędne" łączące pola "Miasto" i "Całość"
When  użytkownik ogląda podgląd kafelka "Adres"
Then  pole "Bez wartości" nie ma wartości
And   pole "Błędne" ma błąd złożenia kombinatora "join" z powodem "nieobsługiwana wartość"
And   pozostałe pola kafelka "Adres" mają wartości
```

#### Scenario #6

Obiekt grupuje pola i zasila wejście danych dziecka

```
Given kafelek "Mapa" typu details ma wejście danych "point" i pola "Szerokość" ($.Lat) i "Długość" ($.Lon)
And   kafelek "Adres" ma ukryte pole "Punkt" grupujące pola "Lat" i "Lon" w obiekt
And   kafelek "Adres" osadza kafelek "Mapa" jako "map"
And   wiązanie z pól kafelka "Adres" przekazuje pole "Punkt" do wejścia "point" dziecka "map"
When  użytkownik ogląda podgląd kafelka "Adres"
Then  pole "Punkt" ma wartość {"Lat": 50.06, "Lon": 19.95}
And   dziecko "map" pokazuje pole "Szerokość" o wartości 50.06 i pole "Długość" o wartości 19.95
```

#### Scenario #7

Pierwsza dostępna wartość

```
Scenario Outline: pierwsza wartość
Given kafelek "Adres" ma pola "Email" ($.email) i "Telefon" ($.phone)
And   kafelek "Adres" ma pole "Kontakt" z pierwszą dostępną wartością z pól <pola>
When  użytkownik ogląda podgląd kafelka "Adres"
Then  pole "Kontakt" ma wartość <wartość>

Examples:
  | pola                 | wartość         |
  | "Email", "Telefon"   | "123 456 789"   |
  | "Telefon", "Email"   | "123 456 789"   |
  | "Email", "Email2"    | brak wartości   |
```

> W ostatnim przykładzie kafelek ma dodatkowo pole "Email2" ($.email).

#### Scenario #8

W kafelku z elementami pole złożone jest wyliczane dla każdego elementu

```
Given kafelek "Lista miast" typu list ma elementy ze źródła "Miasta" pod "$.cities[*]" z kluczem "ID"
And   kafelek "Lista miast" ma pola "ID", ukryte "Nazwa" i "Kraj" oraz pole "Tytuł" ze wzorca "{Nazwa} ({Kraj})"
      z rolą tytułu
And   kafelek "Lista miast" ma akcję "Szczegóły" przy każdym elemencie, która przekazuje pole "Tytuł" jako parametr
      "title" kafelka "Miasto", który przyjmuje parametr "title"
When  użytkownik ogląda podgląd kafelka "Lista miast"
Then  element 1 ma tytuł "Kraków (PL)", a element 2 ma tytuł "Praga (CZ)"
And   akcja "Szczegóły" elementu 2 przekazuje "Praga (CZ)" jako parametr "title"
```

#### Scenario #9

Pole złożone zasila parametr połączenia dziecka

```
Given istnieje połączenie "Pogoda" z wymaganym parametrem zapytania "latlon"
And   kafelek "Pogoda" typu details ma źródło z połączenia "Pogoda"
And   kafelek "Adres" ma pole "Współrzędne" ze wzorca "{Lat},{Lon}"
And   kafelek "Adres" osadza kafelek "Pogoda" jako "weather"
And   wiązanie z pól kafelka "Adres" przekazuje pole "Współrzędne" do parametru "latlon" dziecka "weather"
When  użytkownik ogląda podgląd kafelka "Adres"
Then  połączenie "Pogoda" zostaje odpytane z parametrem "latlon" równym "50.06,19.95"
```

#### Scenario #10

Błąd składnika nie przerywa podglądu

```
Scenario Outline: błąd składnika
Given kafelek "Adres" ma pole "Pełny adres" ze wzorca "{Ulica} {Nr}, {Miasto}"
And   <sytuacja>
When  użytkownik ogląda podgląd kafelka "Adres"
Then  pole "Pełny adres" ma błąd zależności od pola <pole> źródła <źródło> z przyczyną <przyczyna>
And   podgląd kafelka "Adres" zostaje zwrócony z wynikami pozostałych pól

Examples:
  | sytuacja                                                     | pole     | źródło | przyczyna                    |
  | połączenie "Miejsce" jest niedostępne                        | "Ulica"  | "api"  | niedostępne źródło           |
  | pole "Miasto" wskazuje ścieżkę "$.town", której nie ma w danych | "Miasto" | "api"  | brak wartości pod ścieżką |
```

#### Scenario #11

Niepoprawne pole złożone odrzuca zapis

```
Scenario Outline: odrzucenie pola złożonego
Given użytkownik definiuje w kafelku "Adres" pole złożone, które <problem>
When  użytkownik zapisuje kafelek
Then  zapis zostaje odrzucony z błędem walidacji pola złożonego

Examples:
  | problem                                                                 |
  | używa kombinatora "concat", którego nie ma w katalogu                   |
  | ma jednocześnie źródło, ścieżkę i kombinator                            |
  | łączy pole "Kod pocztowy", którego nie ma w kafelku                     |
  | łączy samo siebie                                                       |
  | łączy inne pole złożone                                                 |
  | wybiera pierwszą wartość tylko z pola "Email"                           |
  | ma wzorzec "{Ulica} {Nr" z niezamkniętą klamrą                          |
  | ma wzorzec "Adres" bez żadnego pola                                     |
  | ma wzorzec i dodatkowo listę pól                                        |
  | grupuje pola w obiekt i ma parametr                                     |
  | grupuje pola w obiekt i ma transformację na wielkie litery              |
  | ma wzorzec używający pola "Tagi" ze ścieżką "$.tags[*]"                 |
```

#### Scenario #12

Pole złożone w kafelku z elementami korzysta tylko z pól elementu

```
Scenario Outline: odrzucenie pola złożonego w kafelku z elementami
Given kafelek "Lista miast" <problem>
When  użytkownik zapisuje kafelek
Then  zapis zostaje odrzucony z błędem walidacji pola złożonego

Examples:
  | problem                                                                              |
  | ma pole złożone łączące ukryte pole innego źródła                                    |
  | identyfikuje elementy polem złożonym "Tytuł"                                         |
  | ma wiązanie źródło → źródło przekazujące pole złożone "Tytuł"                        |
```

#### Scenario #13

Pole użyte w kombinatorze nie może zniknąć z kafelka

```
Given kafelek "Adres" ma pole "Pełny adres" ze wzorca "{Ulica} {Nr}, {Miasto}"
When  użytkownik zmienia etykietę pola "Miasto" na "Miejscowość" i zapisuje kafelek "Adres"
Then  zapis zostaje odrzucony z błędem walidacji pola złożonego "Pełny adres"
When  użytkownik usuwa pole "Ulica" i zapisuje kafelek "Adres"
Then  zapis zostaje odrzucony z błędem walidacji pola złożonego "Pełny adres"
```

#### Scenario #14

Pole złożone używane przez wiązanie rodzica nie może zniknąć

```
Given kafelek "Adres" ma pole "Współrzędne" ze wzorca "{Lat},{Lon}"
And   kafelek "Panel" osadza kafelek "Adres" jako "address"
And   wiązanie kafelka "Panel" przekazuje pole "Współrzędne" dziecka "address" do innego dziecka
When  użytkownik usuwa pole "Współrzędne" z kafelka "Adres"
Then  edycja zostaje zablokowana z listą pól "Współrzędne" i kafelków "Panel"
```

#### Scenario #15

Definicja pola złożonego jest trwała i należy do kontraktu kafelka

```
Given kafelek "Adres" ma pole "Miejsce" łączące pola "Ulica" i "Miasto" separatorem ", "
When  użytkownik odczytuje szczegóły kafelka "Adres"
Then  pole "Miejsce" ma kombinator "join" z parametrem ", " i polami "Ulica" i "Miasto", bez źródła i ścieżki
And   kontrakt kafelka "Adres" udostępnia pole "Miejsce"
And   po ponownym uruchomieniu aplikacji szczegóły kafelka "Adres" są takie same
```

#### Scenario #16

Podgląd niezapisanego kafelka wylicza pola złożone

```
Given użytkownik nie zapisał jeszcze kafelka "Adres" z polem "Pełny adres" ze wzorca "{Ulica} {Nr}, {Miasto}"
When  użytkownik ogląda podgląd niezapisanej definicji kafelka "Adres"
Then  pole "Pełny adres" ma wartość "Długa 5, Kraków"
```

#### Scenario #17

Pole złożone dziecka zasila inne dziecko

```
Given kafelek "Pogoda" ma wymagany parametr "latlon"
And   kafelek "Adres" ma pole "Współrzędne" ze wzorca "{Lat},{Lon}"
And   kafelek "Panel" osadza kafelek "Adres" jako "address" i kafelek "Pogoda" jako "weather"
And   wiązanie kafelka "Panel" przekazuje pole "Współrzędne" dziecka "address" do parametru "latlon" dziecka "weather"
When  użytkownik ogląda podgląd kafelka "Panel"
Then  połączenie "Pogoda" zostaje odpytane z parametrem "latlon" równym "50.06,19.95"
And   dziecko "weather" pokazuje dane pogody
```

#### Scenario #18

Akcja kafelka przekazuje wartość pola złożonego

```
Given kafelek "Mapa miejsca" przyjmuje parametr "latlon"
And   kafelek "Adres" ma pole "Współrzędne" ze wzorca "{Lat},{Lon}"
And   kafelek "Adres" ma link "Pokaż na mapie" raz na kafelek, który przekazuje pole "Współrzędne" jako parametr
      "latlon" kafelka "Mapa miejsca"
When  użytkownik ogląda podgląd kafelka "Adres"
Then  link "Pokaż na mapie" przekazuje "50.06,19.95" jako parametr "latlon"
```

#### Scenario #19

Błąd transformacji wyniku nie przerywa podglądu

```
Given kafelek "Adres" ma pole "Pełny adres" ze wzorca "{Ulica} {Nr}, {Miasto}" z zaokrągleniem do 2 miejsc
When  użytkownik ogląda podgląd kafelka "Adres"
Then  pole "Pełny adres" ma błąd transformacji "round"
And   pozostałe pola kafelka "Adres" mają wartości
```

#### Scenario #20

Obiekt zachowuje brakujące wartości i listy

```
Given kafelek "Adres" ma pola "Email" ($.email) i "Tagi" ($.tags[*])
And   kafelek "Adres" ma pole "Szczegóły" grupujące pola "Miasto", "Email" i "Tagi" w obiekt
When  użytkownik ogląda podgląd kafelka "Adres"
Then  pole "Szczegóły" ma wartość {"Miasto": "Kraków", "Email": null, "Tagi": ["zabytki", null, "rynek"]}
```

#### Scenario #21

Pole złożone w tabelach jest wyliczane dla każdego wiersza

```
Scenario Outline: pole złożone w wierszach tabeli
Given kafelek "Tabela miast" typu <typ> ma elementy ze źródła "Miasta" pod "$.cities[*]" z kluczem "ID"
And   kafelek "Tabela miast" ma pola "ID", ukryte "Nazwa" i "Kraj" oraz pole "Miasto" ze wzorca "{Nazwa} ({Kraj})"
When  użytkownik ogląda podgląd kafelka "Tabela miast"
Then  wiersz 1 ma w polu "Miasto" wartość "Kraków (PL)", a wiersz 2 wartość "Praga (CZ)"

Examples:
  | typ              |
  | table            |
  | pagination_table |
```

> Dla `pagination_table` połączenie "Miasta" deklaruje parametry "pageNumber" i "pageSize", a kafelek ma stronicowanie
> po 10 elementów.
