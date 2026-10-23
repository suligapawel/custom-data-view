# Żądania REST: drzewo „Panel pogodowy”

Pliki `.http` (klient HTTP Ridera/IntelliJ) tworzą przykładowe drzewo kafelków na deweloperskim API pogody
(`FakeWeatherEndpoints`, dostępne w środowisku `Development`):

```text
Panel pogodowy (details, korzeń)        pola: Miasto, Jednostki, [Kod miasta]
├── days: Prognoza dni (table)          elementy: $.forecast[*], klucz „Dzień”
└── station: Stacja (details)           pola: Nazwa, Wysokość, Operator, [ID stacji]
    └── readings: Odczyty stacji (table) elementy: $.readings[*], klucz „Godzina”
```

`[…]` to pola ukryte, które zasilają wiązania: „Kod miasta” → `city` obu dzieci panelu, „ID stacji” → `station_id`
tabeli odczytów.

## Kolejność

1. Uruchom aplikację (`dotnet run --project src/CustomDataView.Bootstrapper`, profil `http`, port 5080) i wybierz
   środowisko `dev` z `http-client.env.json`.
2. `01-connections.http` — trzy połączenia: prognoza, stacja, odczyty (oraz test połączenia stacji).
3. `02-tiles.http` — kafelki od liści do korzenia: tabela prognozy, tabela odczytów, etykieta „Stacja” z dzieckiem,
   panel z dwoma dziećmi. Każde żądanie zapisuje identyfikator w zmiennej globalnej (`client.global.set`).
4. `03-preview.http` — podgląd całego panelu, podgląd etykiety z wartościami dla wnuka (`descendants`) i podgląd
   samej tabeli z wartością testową.
5. `04-filters.http` (opcjonalnie, wymaga tylko połączenia prognozy) — filtr w ścieżce JSONPath: tabela „Dni bez
   słońca” z elementami `$.forecast[?(@.summary != 'Słonecznie')]`, etykieta z polami
   `$.forecast[?@.summary == 'Deszczowo'].date` i `$.forecast[?@.humidity == 99].date`, ich podglądy oraz odrzucony
   filtr z operatorem `>`.
6. `05-city-search.http` (niezależny od 01–04, korzysta z `FakeCityEndpoints`) — wyszukiwarka miast: zakładki
   `tabs` z formularzami „Po nazwie” i „Po kraju”, tabela `pagination_table` stronicowana przez API (`pageNumber`,
   `pageSize`, `$.total`) z przyciskiem „Szczegóły” przy każdym wierszu oraz kafelek „Miasto” otwierany jako nowy
   korzeń. Pole formularza z zakładki zasila wyniki wiązaniem `from: "filters/byName"`. Kolejne żądania pokazują
   podgląd korzenia z wpisaną nazwą, następną stronę (wartości `pagination.next`) i otwarcie celu przycisku
   (wartości `actions[].inputs`).
7. `06-combinations.http` (niezależny od 01–05, korzysta z `FakeCityEndpoints`) — kombinatory pól (TEMP-044):
   katalog `GET /combinations`, „Wizytówka miasta” z ukrytymi składnikami i polami złożonymi `template`, `join`
   (z transformacją `uppercase`), `coalesce` i `object`, przy czym obiekt „Dane” zasila wejście danych dziecka
   „Karta miasta”. Do tego lista miast z tytułem elementu złożonym z pól elementu, podgląd szkicu z błędem
   składnika (`dependency_failed`) i dwa odrzucone zapisy (błędny wzorzec, nieznane pole i transformacja przy
   `object`).

W odpowiedzi podglądu panel zwraca swoje pola i dwa osadzenia (`days`, `station`) ze stanem `data`; osadzenie
`station` zawiera zagnieżdżony podgląd etykiety z osadzeniem `readings` pod adresem `station/readings`. Prognoza
korzenia i tabeli `days` ma te same wartości parametrów, więc połączenie jest wywoływane raz.
