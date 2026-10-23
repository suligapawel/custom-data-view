# #TEMP-041 … TEMP-043 — Scenariusz implementacji

## Cel

Wyszukiwarka zbudowana z kafelków: użytkownik wybiera zakładkę z formularzem filtra, wpisuje nazwę miasta, kafelek
pod zakładkami pokazuje stronę wyników z przyciskiem przy każdym wierszu, a przycisk otwiera inny kafelek-korzeń
z wartościami wiersza. Moduł dostarcza tylko definicje i odpowiedzi podglądu; zakładkę aktywną, numer strony
i nawigację obsługuje front na podstawie gotowych `inputs` z odpowiedzi.

```text
Wyszukiwarka miast (korzeń)
├── filters: Filtry (tabs)
│   ├── byName  „Po nazwie”  → Nazwa miasta (form: Miasto)
│   └── byCountry „Po kraju” → Kraj (form: Kraj), lazy
└── results: Wyniki (pagination_table, akcja „Szczegóły” → Miasto)
wiązania korzenia: filters/byName → results (Miasto → name), filters/byCountry → results (Kraj → country)
```

## Decyzje dewelopera

- Zakładki to dzieci kafelka `tabs` (nie kafelki-korzenie po id), więc zakładki są zwykłymi osadzeniami z adresami,
  formularzami, wiązaniami i ładowaniem `eager`/`lazy`. Aktywna zakładka to stan frontu: front wysyła wartości
  formularza aktywnej zakładki, a pola opcjonalne pozostałych zakładek nie mają wartości.
- Kafelek wyników leży obok zakładek, nie w zakładce; pole formularza z zakładki przechodzi do niego wiązaniem
  ze ścieżką `from` (`filters/byName`).
- Usunięty limit 3 poziomów zagnieżdżenia i limit 2 segmentów ścieżek osadzeń w wiązaniach.
- Stronicuje API: moduł przekazuje numer i rozmiar strony parametrami połączenia o nazwach z definicji; strony
  numerowane od 1; łączna liczba elementów opcjonalna (`totalPath`).
- Przycisk to akcja kafelka (`actions`), nie transformacja ani typ pola formularza: transformacja przekształca wartość,
  a pole formularza zbiera wartość od użytkownika. Akcja ma `type` `button` albo `link`.

## Domena

### TEMP-041 Zakładki

- `TileDisplays.Tabs = "tabs"`. Kafelek `tabs`: brak `fields`, `sources`, `items`, `pagination`, `actions`; co
  najmniej jedno dziecko; każde dziecko `scope: tile` z `label` (1–100 znaków, tytuł zakładki). Wiązania tylko
  dziecko → dziecko lub potomek (kafelek nie ma pól, więc nie ma `parent`). Kontrakt: brak parametrów, wejść i pól.
- `TileChild.Label` — opcjonalny dla innych rodziców (do 100 znaków), zwracany w szczegółach i w osadzeniu podglądu.
- Ścieżka `from` wiązania (`a/b/...`): każdy segment to dziecko `scope: tile` kafelka z poprzedniego segmentu; pole
  pochodzi z kontraktu ostatniego kafelka, a zakres (`selected`, `all`) z jego elementów. Dozwolone cele jak dla
  dziecka `scope: tile`: dziecko, potomek, źródło `connection`. Krawędź grafu biegnie od pierwszego segmentu; pierwszy
  segment `from` i `to` muszą być różne.
- Ścieżki `from`/`to` bez limitu segmentów; dla celu w potomku żaden kafelek pośredni nie może już wiązać tego wejścia.
- `TileReferencesValidator`: bez limitu poziomów; cykle zawierania zostają.
- Blokada `field_in_use` uwzględnia wiązania przodków ze ścieżką `from` kończącą się na osadzeniu kafelka.
- Podgląd: rozwiązanie `from` przechodzi przez osadzenia kolejnych segmentów; stan pośredni inny niż `data` działa jak
  stan dziecka (`awaiting_input` → `pending`, `awaiting_load` → cel `awaiting_load`, `error` → `dependency_failed`),
  brak osadzenia w odpowiedzi kafelka (np. po zmianie klucza) → `dependency_failed` z przyczyną `invalid_binding`.

### TEMP-042 Tabela ze stronicowaniem

- `TileDisplays.PaginationTable = "pagination_table"`; `HasItems` obejmuje `table`, `list`, `pagination_table`;
  role pól jak `table` (brak).
- `TilePagination(PageNumberParameter, PageSizeParameter, PageSize, TotalPath)`; `pagination` wymagane tylko i
  wyłącznie dla `pagination_table`. Domyślne nazwy `pageNumber` i `pageSize`; nazwy różne; `pageSize` 1–100;
  `totalPath` — poprawna ścieżka JSONPath bez wildcardu i filtra.
- Źródło elementów `pagination_table` jest źródłem `connection`; oba parametry są zadeklarowane w jego połączeniu
  (walidacja referencji). Parametry stronicowania zostają w kontrakcie kafelka.
- Podgląd: numer strony = wartość parametru numeru (doręczona albo z wiązania), domyślnie 1; rozmiar = wartość
  parametru rozmiaru, domyślnie `pageSize`. Wartość musi być liczbą całkowitą (liczba albo tekst cyfr): numer ≥ 1,
  rozmiar 1–100; inaczej źródło elementów nie jest wywoływane, a elementy mają błąd `invalid_parameters`.
- Odpowiedź: `pagination` (`page`, `pageSize`, `total`, `pages`, `previous`, `next`) obok `items`, tylko gdy elementy
  się wczytały. `total` — liczba całkowita pod `totalPath` w odpowiedzi źródła elementów (brak albo nie liczba →
  `total` i `pages` pominięte); `pages = ceil(total / pageSize)`. `next` istnieje, gdy `page < pages`, a bez `pages`,
  gdy liczba elementów strony ≥ `pageSize`; `previous`, gdy `page > 1`. `previous`/`next` to wartości doręczone
  instancji (z wiązań rodzica) z ustawionym numerem i rozmiarem strony, w kształcie żądania podglądu.

### TEMP-043 Akcje

- `TileAction(Key, Label, Type, TileId, Scope, Map)`, `TileActionMapping(Field, Parameter, Input)`;
  `TileActionTypes`: `button` (domyślny), `link`; `TileActionScopes`: `item` (domyślny u kafelka z elementami),
  `tile` (domyślny w pozostałych).
- `actions` dla `table`, `list`, `details`, `summary`, `pagination_table`; nie dla `form` i `tabs`. Klucz jak klucz węzła,
  unikalny wśród akcji; `label` 1–100 znaków; `scope: item` tylko u kafelka z elementami; `map` może być puste;
  mapowanie: `field` wymagane, dokładnie jedno z `parameter` i `input`, każde wejście celu najwyżej raz w akcji.
- Pole mapowania: dla `item` pole źródła elementów; dla `tile` pole spoza źródła elementów (u kafelka bez elementów
  dowolne pole).
- Referencje: kafelek docelowy istnieje (może to być ten sam kafelek); `parameter` należy do parametrów, a `input` do
  wejść kontraktu celu.
- Podgląd: akcje `item` w każdym elemencie, akcje `tile` w kafelku: `key`, `label`, `type`, `tileId` oraz `inputs`
  (wartości pól po transformacjach; `null` pomijany) albo `error`: `dependency_failed` z przyczyną (źródło, pole, kod)
  przy błędzie pola, `invalid_binding` z listą parametrów, gdy cel nie deklaruje już parametru lub wejścia.
- Usunięcie kafelka wskazanego przez akcję jest blokowane: `tile_in_use` z listą kafelków (rodzice i kafelki z akcjami).

## Baza danych

Migracja `AddTabsPaginationAndActions` (tworzona, nie uruchamiana):

- `tiles.display` `varchar(20)`, CHECK z `pagination_table` i `tabs`; `ck_tiles_items` obejmuje `pagination_table`.
- `tiles`: `pagination_page_number_parameter varchar(50)`, `pagination_page_size_parameter varchar(50)`,
  `pagination_page_size integer`, `pagination_total_path text`; CHECK `ck_tiles_pagination`: trzy pierwsze ustawione
  wtedy i tylko wtedy, gdy `display = 'pagination_table'`, `total_path` tylko wtedy, `page_size` 1–100.
- `tile_children.label varchar(100) NULL`.
- `tile_bindings.target_node` i `source_node` jako `text` (ścieżki bez limitu).
- `tile_actions` (`tile_id`, `key` PK; `position` unikalna; `label`, `type`, `target_tile_id` FK `RESTRICT`, `scope`)
  i `tile_action_mappings` (`tile_id`, `action_key`, `target_kind`, `target_name` PK; `source_field`, `position`;
  FK do akcji `CASCADE`).

## Kryteria akceptacji

1. Podgląd kafelka `tabs` zwraca zakładki jako osadzenia z `label` w kolejności `position`; zakładka `lazy` ma stan
   `awaiting_load`, zakładka `eager` jest wykonana.
2. Pole formularza z zakładki zasila kafelek obok zakładek wiązaniem `from` ze ścieżką; wymagane pole bez wartości
   wstrzymuje cel (`awaiting_input`, `pending` z adresem formularza).
3. Rodzic przekazuje wartości do zakładki ścieżką `to`, a kafelek `tabs` wiąże zakładki między sobą.
4. Zapis kafelka `tabs` bez dzieci, z dzieckiem bez `label`, z dzieckiem `scope: item`, z polami albo ze źródłami jest
   odrzucany; ścieżka `from` do nieistniejącego osadzenia albo pola jest odrzucana.
5. Kafelki zagnieżdżone na 4 poziomach zapisują się; cykl zawierania jest odrzucany.
6. Edycja kafelka zakładki usuwająca pole użyte przez wiązanie przodka ze ścieżką daje 409 `field_in_use`.
7. Pierwszy podgląd `pagination_table` wywołuje API ze stroną 1 i rozmiarem z definicji i zwraca `pagination`
   z `total`, `pages` i `next`; podgląd z wartościami `next` wywołuje stronę 2 i zwraca `previous`.
8. Bez `totalPath` `next` zależy od pełnej strony; własne nazwy parametrów i rozmiar strony od frontu trafiają do API.
9. Niepoprawne `pagination` odrzuca zapis; numer strony spoza zakresu daje błąd elementów `invalid_parameters`.
10. Akcja `item` zwraca przy każdym elemencie `inputs` celu, a podgląd celu z tymi wartościami wczytuje dane; akcja
    `tile` typu `link` jest zwracana w kafelku.
11. Błąd mapowanego pola daje akcję z błędem `dependency_failed`; zmiana kontraktu celu daje `invalid_binding`.
12. Niepoprawna akcja odrzuca zapis; usunięcie kafelka docelowego daje 409 `tile_in_use`.
13. Przepływ end-to-end: zakładka z formularzem → strona wyników z przyciskami → podgląd nowego korzenia.

## Testy

API: `src/CustomDataView/docs/scenarios/scenario_TEMP-041-043/gherkin.md` (testy w `Tiles/TabsTileTests`,
`Tiles/PaginationTableTests`, `Tiles/TileActionTests`, `Tiles/CitySearchFlowTests`); repozytorium: zapis i odczyt
stronicowania, etykiet dzieci i akcji, użycia pól przez ścieżki i kafelki z akcjami.
