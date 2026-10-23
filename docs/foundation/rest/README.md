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

W odpowiedzi podglądu panel zwraca swoje pola i dwa osadzenia (`days`, `station`) ze stanem `data`; osadzenie
`station` zawiera zagnieżdżony podgląd etykiety z osadzeniem `readings` pod adresem `station/readings`. Prognoza
korzenia i tabeli `days` ma te same wartości parametrów, więc połączenie jest wywoływane raz.
