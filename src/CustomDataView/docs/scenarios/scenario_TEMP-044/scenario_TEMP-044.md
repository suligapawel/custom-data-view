# #TEMP-044 — Scenariusz implementacji

## Cel

Użytkownik składa jedno pole kafelka z kilku innych pól, np. pokazuje „Długa 5, Kraków” w jednej kolumnie albo
przekazuje wiązaniem obiekt `{"Lat": 50.06, "Lon": 19.94}`. Robi to kombinatorem z katalogu w kodzie, tak jak dziś
formatuje pojedynczą wartość transformacją, bez zmian w kodzie aplikacji.

## Kontekst biznesowy

Dziś każde pole kafelka bierze jedną wartość spod jednej ścieżki JSONPath. Adres, współrzędne czy „e-mail albo
telefon” wymagają dziś kilku osobnych pól, a front musi je sam składać. Kombinatory przenoszą to do definicji kafelka.
Składniki zwykle są polami ukrytymi, a widoczny jest tylko wynik. Zastępuje to notatkę
`docs/temp/computed-fields-todo.md` (rodzaje pól `template`/`object`): deweloper wybrał model „jak transformacje”, czyli
katalog w kodzie z jednym ogólnym kształtem definicji.

```json
"fields": [
  { "label": "Ulica",   "source": "api", "path": "$.street", "hidden": true },
  { "label": "Nr",      "source": "api", "path": "$.number", "hidden": true },
  { "label": "Miasto",  "source": "api", "path": "$.city",   "hidden": true },
  { "label": "Adres",   "combine": { "key": "template", "parameter": "{Ulica} {Nr}, {Miasto}" },
    "transformations": [ { "key": "uppercase" } ] },
  { "label": "Miejsce", "combine": { "key": "join", "parameter": ", ", "fields": ["Ulica", "Miasto"] } },
  { "label": "Kontakt", "combine": { "key": "coalesce", "fields": ["Email", "Telefon"] } },
  { "label": "Punkt",   "combine": { "key": "object", "fields": ["Lat", "Lon"] }, "hidden": true }
]
```

## Decyzje dewelopera

- Kombinator działa jak transformacja wielu pól: katalog predefiniowany w kodzie, wspólny interfejs, najwyżej jeden
  parametr tekstowy. Kombinator sam interpretuje parametr i listę pól. Kształt definicji jest zawsze ten sam:
  `combine: { key, parameter, fields }`.
- Wejściami są wyłącznie etykiety pól tego samego kafelka (wartości po ich transformacjach). Składniki, których nie
  chcemy pokazywać, to pola `hidden`. Adresy właściwości (`source` + `path`) nie są wejściami kombinatora.
- Wejście nie może być polem złożonym (brak łańcuchów kombinatorów, więc nie ma cykli).
- `template` używa etykiet w klamrach we wzorcu, a lista `fields` jest dla niego pusta.
- `object`: kluczem członka jest etykieta pola wejściowego w zapisanej postaci. Pole z `object` nie ma transformacji.
- `null`: `join` pomija, `template` wstawia pusty tekst, `object` wpisuje `null`, `coalesce` szuka dalej.
- Tablice: `join` spłaszcza i łączy elementy tym samym separatorem; `template` odrzuca przy zapisie pole, którego
  ścieżka zwraca tablicę; `object` i `coalesce` traktują tablicę jak wartość.
- Błąd pola wejściowego daje `dependency_failed` z przyczyną. Błąd samego kombinatora daje nowy kod
  `combination_failed`, analogicznie do `transformation_failed`.
- Katalog jest osobnym endpointem `GET /api/custom-data-view/combinations`.
- Usunięcie albo zmiana etykiety pola-wejścia w tym samym kafelku to błąd walidacji zapisu, a nie 409. Blokada
  `field_in_use` dla wiązań przodków działa bez zmian, także dla pól złożonych.
- Kombinatory działają w `table`, `list`, `details`, `summary` i `pagination_table` (`form` i `tabs` nie mają takich
  pól). W kafelku z elementami wejścia pochodzą ze źródła elementów, a wynik jest polem elementu.
- Baza wzorowana na transformacjach: osobne tabele kombinatora pola i jego pól wejściowych, bez kolumn JSON.

## Domena

### Katalog kombinatorów (`Domain/Services/Combinations`)

- `ICombination`: `Key`, `Name`, `Description`, `CombinationParameterDescriptor Parameter` (nazwa, typ, czy wymagany;
  `null` = brak parametru), `MinFields`, `MaxFields` (`null` = bez limitu) oraz:
  - `IReadOnlyList<string> Inputs(string parameter, IReadOnlyList<string> fields)` zwraca etykiety pól, których
    kombinator potrzebuje, w kolejności użycia (`template` bierze je ze wzorca, pozostałe z `fields`);
  - `IReadOnlyList<CombinationError> Validate(string parameter, IReadOnlyList<string> fields)` to błędy definicji
    niezależne od kafelka (parametr, liczba pól, składnia wzorca);
  - `bool AcceptsCollections` mówi, czy wejście może być polem zwracającym tablicę (`false` tylko dla `template`);
  - `bool AcceptsTransformations` mówi, czy wynik może mieć łańcuch transformacji (`false` tylko dla `object`);
  - `JsonNode Combine(string parameter, IReadOnlyList<CombinationInput> inputs)`, gdzie `CombinationInput(Label,
    JsonNode Value)`; zwraca wartość albo rzuca `CombinationFailedException(key, reason)`.
- `CombinationService` (singleton, jak `TransformationService`) daje katalog po kluczu (bez rozróżniania wielkości
  liter jak transformacje), listę do odczytu i `Combine(...)` z mapowaniem wyjątku na `CombinationResult`.
- Wartości proste zamieniane na tekst (`join`, `template`) w kulturze niezmiennej: liczba jak w JSON (`50.06`, `5`),
  `true`/`false`, tekst bez cudzysłowów.
- `join(separator)`: parametr opcjonalny (brak = pusty separator), pola ≥ 1. Kolejno wartości `fields`: `null` jest
  pomijany, tablica spłaszczana (jeden poziom; `null` w tablicy pomijany), a pozostałe wartości są łączone
  separatorem. Obiekt albo tablica zagnieżdżona daje `combination_failed` z powodem `unsupported_value`. Gdy nie
  zostanie żadna wartość, wynik to `null`.
- `template(pattern)`: parametr wymagany; `fields` musi być puste. Wzorzec zawiera co najmniej jedno odwołanie
  `{Etykieta}`; etykieta w klamrach jest przycinana i porównywana jak etykiety pól (`TileLabels`). `{{` i `}}` to
  dosłowne klamry; niezamknięta klamra, `}` bez pary i pusta etykieta `{}` to błąd walidacji. Ta sama etykieta może
  wystąpić kilka razy. Za `null` wstawiany jest pusty tekst. Wartość obiekt/tablica w czasie podglądu (np. pole ze
  źródła `input` bez wildcardu wskazujące tablicę) daje `combination_failed` z powodem `unsupported_value`.
- `object`: bez parametru (podany parametr to błąd), pola ≥ 1, etykiety unikalne. Wynik to `JsonObject` z kluczami
  równymi etykietom wejść (pisownia z definicji pola wejściowego) i wartościami po transformacjach (kopie, także `null`
  i tablice). Nie przyjmuje transformacji.
- `coalesce`: bez parametru, pola ≥ 2. Wynik to pierwsza wartość różna od `null` (pusta tablica i pusty tekst to
  wartości); same `null` dają `null`.

### Model pola

- `TileField` dostaje opcjonalne `TileFieldCombination Combination` (`Key`, `Parameter`, `Fields`). Pole ma dokładnie
  jedno z: `Source` + `Path` albo `Combination` (`IsCombined => Combination is not null`).
- Pole złożone nie należy do źródła (`Source == null`), więc:
  - nie jest polem źródła w wiązaniach źródło → źródło (`from` = klucz źródła);
  - jest dostępne przez `parent`, z dziecka (kontrakt `outputs`) i w akcjach;
  - w kafelku z `items` jest polem elementu (wliczane do pól elementu w podglądzie, w `scope: item`/`selected`/`all`,
    w mapowaniach akcji `scope: item`); w kafelku bez `items` jest polem kafelka (akcje `scope: tile`).

### Walidacja (`TileDefinitionValidator`)

Dla każdego pola z `combine` (błędy jako dotychczasowe błędy walidacji pól, ścieżka `fields[i].combine...`):

1. Pole nie ma jednocześnie `source`/`path` i `combine`; pole bez `combine` ma `source` i `path` jak dotąd.
2. `combine.key` jest w katalogu kombinatorów.
3. Błędy z `ICombination.Validate` (parametr, liczba pól, wzorzec, unikalność w `object`).
4. Każda etykieta z `Inputs(...)` istnieje w kafelku, nie jest samym polem i nie jest polem złożonym.
5. Gdy `AcceptsCollections == false`, wejście nie ma ścieżki z wildcardem albo filtrem (`JsonPath.IsCollection`).
6. Gdy `AcceptsTransformations == false`, pole nie ma transformacji.
7. Kafelek z `items`: każde wejście pochodzi ze źródła `items.source`; pole złożone nie jest `items.key`.
8. Pole złożone nie występuje jako `field` w wiązaniu, którego `from` jest kluczem źródła.
9. `display` `form` i `tabs`: zachowane obecne reguły (formularz ma pola formularza, `tabs` nie ma pól), więc
   `combine` w nich nie występuje.

Etykiety, role, `hidden`, unikalność etykiet i blokada `field_in_use` działają bez zmian.

### Podgląd (`TilePreviewer`)

- Pola ze źródłami są wyliczane jak dotąd. Pola złożone liczone są po nich: dla kafelka z pól kafelka, dla elementu
  z pól elementu. Lista pól w odpowiedzi zachowuje kolejność definicji.
- Dla pola złożonego:
  1. Wejście z błędem: `dependency_failed` z `cause` (`node` = źródło pola wejściowego, `field` = etykieta wejścia,
     `code` = kod błędu wejścia); bierzemy pierwsze błędne wejście w kolejności `Inputs`.
  2. `Combine(...)`; `CombinationFailedException` daje `combination_failed` z `combination` (klucz) i `reason`.
  3. Łańcuch transformacji pola; błąd daje `transformation_failed` jak dotąd.
- Wynik ma `hidden` z definicji i trafia do wiązań, akcji, `scope: item` dzieci i `outputs` jak każde pole.
- `TileFieldErrorCodes.CombinationFailed = "combination_failed"`; `TileFieldError` dostaje opcjonalne `Combination`.

## Kryteria akceptacji

1. Katalog kombinatorów zwraca `join`, `template`, `object`, `coalesce` z kluczem, nazwą, opisem, parametrem
   (nazwa, typ, czy wymagany) oraz `minFields`/`maxFields`.
2. `template` „{Ulica} {Nr}, {Miasto}” w kafelku `details` daje „Długa 5, Kraków”; składniki ukryte są zwracane
   z `hidden: true`; transformacja `uppercase` na polu złożonym daje „DŁUGA 5, KRAKÓW”.
3. Składnik `null` w `template` daje pusty tekst w jego miejscu; `{{`/`}}` dają dosłowne klamry.
4. `join` z separatorem „, ” pomija `null` i spłaszcza tablicę (pole `$.tags[*]`); same `null` dają `null`; obiekt
   w wejściu daje `combination_failed` (`unsupported_value`).
5. `object` z pól „Lat” i „Lon” zwraca `{"Lat": 50.06, "Lon": 19.94}` (wartości po transformacjach, np. `round`)
   i zasila wiązaniem `parent` → dziecko wejście danych dziecka, które wczytuje dane.
6. `coalesce` zwraca pierwszą wartość różną od `null`; same `null` dają `null`.
7. Pole złożone z roli `title` w `list` i w `table`/`pagination_table` jest wyliczane dla każdego elementu z pól
   elementu; akcja `scope: item` przekazuje wartość pola złożonego elementu.
8. Pole złożone `template` „{Lat},{Lon}” zasila parametr połączenia dziecka (`parent` → dziecko).
9. Błąd źródła składnika (`source_unavailable`) albo brak wartości pod ścieżką (`path_not_found`) daje polu złożonemu
   `dependency_failed` z przyczyną; pozostałe pola kafelka zwracają dane (status 200).
10. Zapis jest odrzucany (400) przy: nieznanym kluczu, `source`/`path` razem z `combine`, nieznanej etykiecie,
    odwołaniu do samego pola, wejściu będącym polem złożonym, za małej liczbie pól (`coalesce` z jednym polem), `fields`
    przy `template`, błędnym wzorcu (niezamknięta klamra, brak odwołania), parametrze przy `object`/`coalesce`,
    polu-tablicy w `template`, transformacjach przy `object`, wejściu spoza źródła elementów, polu złożonym jako
    `items.key` i polu złożonym w wiązaniu ze źródła.
11. Zmiana etykiety albo usunięcie pola będącego wejściem kombinatora w tym samym kafelku jest odrzucane przy zapisie
    (400); usunięcie pola złożonego użytego przez wiązanie rodzica daje 409 `field_in_use`.
12. Szczegóły kafelka zwracają `combine` (`key`, `parameter`, `fields`) i `null` dla `source`/`path`; kontrakt
    zawiera pole złożone w `outputs`. Definicja odtwarza się po ponownym odczycie (zapis w bazie).
13. Podgląd niezapisanego kafelka z kombinatorami działa tak samo jak zapisanego.

## Endpoint

`GET /api/custom-data-view/combinations` (200):

```json
[
  { "key": "join", "name": "Złączenie", "description": "…",
    "parameter": { "name": "separator", "type": "text", "required": false }, "minFields": 1, "maxFields": null },
  { "key": "template", "name": "Szablon tekstu", "description": "…",
    "parameter": { "name": "pattern", "type": "text", "required": true }, "minFields": 0, "maxFields": 0 },
  { "key": "object", "name": "Obiekt", "description": "…", "parameter": null, "minFields": 1, "maxFields": null },
  { "key": "coalesce", "name": "Pierwsza wartość", "description": "…", "parameter": null, "minFields": 2,
    "maxFields": null }
]
```

`POST`/`PUT /api/custom-data-view/tiles`, szczegóły i podgląd: pole przyjmuje i zwraca
`"combine": { "key": "...", "parameter": "...", "fields": ["..."] }`. Podgląd pola z błędem kombinatora:

```json
{ "label": "Miejsce", "error": { "code": "combination_failed", "combination": "join", "reason": "unsupported_value" } }
```

Błąd wejścia:

```json
{ "label": "Adres", "error": { "code": "dependency_failed",
  "cause": { "node": "api", "field": "Miasto", "code": "path_not_found" } } }
```

## Walidacja wejścia

- `combine.key`: wymagany, 1–50 znaków, klucz z katalogu.
- `combine.parameter`: opcjonalny tekst; liczba też dozwolona i zapisywana jako tekst, jak parametr transformacji.
- `combine.fields`: opcjonalna lista etykiet (`null` = pusta); każda etykieta 1–100 znaków.
- Pole z `combine`: `source` i `path` puste; pozostałe reguły w sekcji „Walidacja”.

## Handler i sekwencja wykonania

- `GetCombinations` (query) zwraca katalog z `CombinationService` (bez bazy).
- `AddTile`/`UpdateTile`: mapowanie `TileFieldInput.Combine` → `TileFieldCombination`, walidacja definicji
  (`TileDefinitionValidator` z katalogiem kombinatorów), referencje i `field_in_use` jak dotąd, a potem zapis.
- `GetTile`: mapowanie `combine` do `TileFieldDto`; kontrakt bez zmian logiki (`outputs` = etykiety pól).
- `PreviewTile`/`PreviewTileDraft`: `TilePreviewer` wylicza pola złożone po polach ze źródeł.

## Baza danych

Migracja `AddFieldCombinations` (tworzona, nie uruchamiana); `db-plan.md` jest aktualizowany w tej zmianie:

- `tile_fields.source_key` i `tile_fields.path` stają się `NULL`; CHECK `ck_tile_fields_source`:
  `(source_key IS NULL) = (path IS NULL)`. FK `(tile_id, source_key)` → `tile_sources` zostaje (MATCH SIMPLE pomija
  `NULL`).
- `custom_data_view.tile_field_combinations` (wzorowana na `tile_field_transformations`):
  `tile_id uuid`, `field_label varchar(100)`, `key varchar(50) NOT NULL`, `parameter text NULL`;
  PK `pk_tile_field_combinations (tile_id, field_label)`; FK `fk_tile_field_combinations_tile_fields`
  `(tile_id, field_label)` → `tile_fields (tile_id, label)` `ON DELETE CASCADE`.
- `custom_data_view.tile_field_combination_fields`: `tile_id uuid`, `field_label varchar(100)`,
  `position integer CHECK (position >= 0)`, `input_label varchar(100) NOT NULL`;
  PK `pk_tile_field_combination_fields (tile_id, field_label, position)`; FK
  `fk_tile_field_combination_fields_tile_field_combinations` `(tile_id, field_label)` →
  `tile_field_combinations` `ON DELETE CASCADE`.
- Zgodność „źródło i ścieżka albo kombinator” sprawdza aplikacja (jak `display` a pola formularza). Wejścia nie mają
  FK do `tile_fields`, bo walidacja i tak blokuje nieznane etykiety, a zmiana etykiety wejścia jest odrzucana przy
  zapisie.

## Repozytoria

- `TileRepository` (zapis): dla pola złożonego wiersz `tile_fields` z `source_key`/`path` = `NULL`, wiersz
  `tile_field_combinations` i wiersze `tile_field_combination_fields` w kolejności `fields`. Przy edycji kafelka
  wiersze są zastępowane jak transformacje.
- Odczyt: `Include` kombinatora i jego pól (uporządkowanych po `position`) obok transformacji; mapowanie na
  `TileFieldCombination`. Nowe konfiguracje `TileFieldCombinationRowConfiguration`
  i `TileFieldCombinationFieldRowConfiguration` w `Persistence/Configurations/`, wiersze w `Persistence/Rows/`.
- Odczyty użycia pól (`TileFieldUse`) bez zmian.

## Frontend

Bez zmian (frontend nie jest częścią MVP; pole złożone wygląda w podglądzie jak każde pole).

## Testy

### Testy jednostkowe

- **Domena (kombinatory)**, bez mocków:
  - `join`: separator, brak separatora, pomijanie `null`, spłaszczanie tablicy, same `null` → `null`, obiekt →
    `CombinationFailedException`, liczby i bool w kulturze niezmiennej;
  - `template`: `Inputs` ze wzorca (z powtórzeniami, przycinanie, bez rozróżniania wielkości liter), `{{`/`}}`,
    walidacja (niezamknięta klamra, `}` bez pary, `{}`, brak odwołań, niepuste `fields`), `null` → pusty tekst,
    tablica → błąd;
  - `object`: klucze z etykiet, `null` i tablice zachowane, parametr odrzucony, duplikaty etykiet;
  - `coalesce`: pierwsza wartość, pusta tablica i pusty tekst to wartości, same `null` → `null`, mniej niż 2 pola.
- **Walidator definicji** dla reguł 1–8 z sekcji „Walidacja”.

### Testy integracyjne

> Testy integracyjne dla tego scenariusza powstają na podstawie scenariuszy Gherkin w
> `src/CustomDataView/docs/scenarios/scenario_TEMP-044/gherkin.md` (testy w `Tiles/FieldCombinationTests`
> i `Transformations/CombinationCatalogTests`); repozytorium: zapis i odczyt kombinatora i jego pól, pole bez źródła.
