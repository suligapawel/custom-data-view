# TODO — pola liczone (łączenie i grupowanie pól kafelka)

Status: do zaprojektowania (nowa historyjka w PRD i osobna zmiana). Notatka z 2026-10-23.

## Cel

- Połączenie dwóch lub więcej pól kafelka w jeden tekst (np. „Ulica Nr, Miasto”).
- Pogrupowanie kilku pól w obiekt.

## Wniosek: nie jako transformacja

Transformacja to dekorator jednej wartości (wartość pola → wartość). Łączenie i grupowanie biorą **kilka pól** i
tworzą **nowe pole**. Wciśnięte w transformacje wymagałyby parametru wskazującego inne pola, ukrytych zależności między
polami i mieszania wyciągania/formatowania wartości z budowaniem struktury.

## Propozycja: rodzaj pola (`kind`)

| `kind`           | Skąd wartość                              | Przykład                          |
|------------------|-------------------------------------------|-----------------------------------|
| `value` (obecne) | `source` + `path`                         | `$.address.city`                  |
| `template`       | szablon tekstu z etykiet innych pól       | `"{Ulica} {Nr}, {Miasto}"`        |
| `object`         | obiekt złożony z innych pól               | `{ "lat": "Lat", "lon": "Lon" }`  |

```json
"fields": [
  { "label": "Lat", "source": "geo", "path": "$.lat", "hidden": true },
  { "label": "Lon", "source": "geo", "path": "$.lon", "hidden": true },
  { "label": "Współrzędne", "kind": "template", "template": "{Lat}, {Lon}" },
  { "label": "Punkt", "kind": "object", "members": { "lat": "Lat", "lon": "Lon" } }
]
```

## Jak to się łączy z obecnym modelem

- **Pola ukryte** jako składniki — wyciągasz `Lat`/`Lon` jako ukryte, pokazujesz tylko wynik.
- **Transformacje** działają dalej: składniki są już przetransformowane (np. `round`), a pole `template` może mieć
  własny łańcuch (np. `uppercase`). Pole `object` nie ma transformacji (dekoratory działają na wartościach prostych).
- **Wiązania:** `object` może zasilać wejście danych (`input`) dziecka, `template` — parametr połączenia (częściowo
  pokrywa granicę PRD „składanie jednego parametru z kilku pól”).
- **Elementy (`items`):** w `table`/`list` szablon działa w obrębie jednego elementu.

## Do dodania

- **Walidacja:** odwołania do istniejących etykiet, brak cykli między polami, zgodność rodzaju pola z `display`.
- **Podgląd:** pola liczone wyliczane po polach `value`; błąd składnika → `dependency_failed`; brak wartości
  w szablonie → pusty tekst albo błąd (do decyzji).
- **Baza:** `tile_fields.kind`, opcjonalne `source_key`/`path` z CHECK zależnym od `kind`, kolumna `template text`,
  składniki obiektu w tabeli `tile_field_members (tile_id, field_label, name, member_label, position)` (zgodnie
  z decyzją „bez kolumn JSON”).

## Otwarte pytanie

Do czego ma służyć grupowanie „do obiektu”?

1. **Dane** — obiekt jako wartość przekazywana wiązaniem do wejścia innego kafelka → pole `object`.
2. **Prezentacja** — sekcje/grupy pól w kafelku (np. „Adres” z trzema polami pod jednym nagłówkiem w `details`) →
   raczej atrybut `group` pola (wskazówka dla frontu), a nie nowa wartość.

Rekomendacja: zacząć od `template` (najprostszy, od razu przydatny); `object` dodać, gdy pojawi się konkretne wiązanie,
które go potrzebuje.
