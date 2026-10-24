# #TEMP-045 — Scenariusz implementacji

## Cel

Użytkownik zamienia wartość pola na wartość ze słownika, np. kod statusu `NEW` na „Nowe” albo liczbę 6 na 8. Słownik
buduje zwykłym kafelkiem z elementami, który pobiera dane istniejącym połączeniem i który front może też wyświetlić.
Słowniki są trzymane w pamięci aplikacji, więc podgląd nie wywołuje ich endpointów.

## Kontekst biznesowy

Odpowiedzi API często zawierają kody (statusy, typy, poziomy), a czytelne nazwy przychodzą z innego endpointu, np.
`[{"key": "NEW", "translation": "Nowe"}]`. Dziś nie da się połączyć tych danych. Transformacje widzą tylko jedną
wartość i stały parametr, a kombinatory działają tylko w obrębie pól jednego kafelka (w elementach tylko na polach
źródła elementów). Transformacja `mapper` rozwiązuje to bez nowego pojęcia w modelu. Słownik jest kafelkiem, a mapper
podaje tylko, który kafelek i które pole elementu dają wartość.

```json
{
  "name": "Statusy zamówień", "display": "table",
  "sources": [ { "key": "statuses", "kind": "connection", "connectionId": "<ID_POŁĄCZENIA_STATUSÓW>" } ],
  "items": { "source": "statuses", "path": "$[*]", "key": "Klucz" },
  "fields": [
    { "source": "statuses", "path": "$.key", "label": "Klucz" },
    { "source": "statuses", "path": "$.translation", "label": "Tłumaczenie" }
  ]
}
```

```json
{ "source": "orders", "path": "$.status", "label": "Status",
  "transformations": [ { "key": "mapper", "parameter": "<ID_KAFELKA_STATUSÓW>:Tłumaczenie" } ] }
```

## Decyzje dewelopera

- Nazwa transformacji to `mapper`. Mapowanie nie musi być tłumaczeniem, np. liczba 6 na liczbę 8.
- Słownik nie ma osobnej sekcji w definicji kafelka. Słownikiem jest każdy kafelek wskazany przez `mapper`. Kluczem
  słownika jest pole `items.key`, a pole wartości wskazuje parametr mappera. Jeden kafelek może dawać kilka słowników,
  po jednym na każde pole wartości.
- Parametr zostaje tekstem w formacie `"<tileId>:<etykieta pola wartości>"`. Kontrakt `ITransformation` się nie
  zmienia.
- `MapperTransformation` dostaje w konstruktorze singleton słowników i czyta z niego synchronicznie, bez IO.
- Słowniki ładują się raz po starcie aplikacji oraz w tle po zapisie. W MVP nie ma okresowego odświeżania ani
  dociągania słownika w podglądzie.
- Zapis kafelka-słownika albo kafelka z odwołaniem do niezaładowanego (także nieudanego) słownika zleca załadowanie
  w tle. Zapis nie czeka na źródło i nie zawodzi przez nie.
- Słownik, który nie załadował się po starcie, pozostaje niedostępny do restartu albo do kolejnego zapisu.
- Przy przeładowaniu działa stara wersja aż do atomowej podmiany. Nieudane przeładowanie usuwa słownik z pamięci, bo
  stara wersja może nie pasować do nowej definicji.
- Zmiana połączenia kafelka-słownika nie przeładowuje słownika (deweloper nie zakłada takich zmian).
- Brak klucza daje błąd pola `transformation_failed` z powodem `missing_dictionary_key`, a niezaładowany słownik daje
  powód `dictionary_unavailable`. Pole z błędem nie zasila wiązań.
- Powtórzony klucz albo błąd pola klucza lub wartości w dowolnym elemencie oznacza, że słownik się nie ładuje. Klucz
  `null` jest pomijany.
- Kafelek-słownik może mieć dzieci, akcje, inne źródła i pola spoza elementów. Ładowanie je pomija i wykonuje tylko
  źródło elementów i pola elementu.
- Kafelek-słownik może mieć tylko opcjonalne parametry (albo parametry z wartością domyślną). Ładowanie używa wartości
  domyślnych.
- Edycja kafelka-słownika, która psuje odwołania, daje 409. Usunięcie daje 409 jak przy innych odwołaniach.
- Słowniki stałe (enumy) i źródło `static` są poza zakresem.

## Domena

### Odwołanie do słownika (`Domain/ValueObjects/DictionaryReference`)

- `DictionaryReference(Guid TileId, string ValueLabel)` z `static bool TryParse(string parameter, out
  DictionaryReference reference)`.
  - Format: GUID w zapisie `D` (36 znaków), dwukropek, etykieta (po przycięciu niepusta, najwyżej 100 znaków).
  - GUID ma stałą długość, więc dwukropek w etykiecie nie powoduje niejednoznaczności.
- Równość odwołań: ten sam `TileId` i etykiety równe według `TileLabels.AreEqual`. Dzięki temu
  `"…:Tłumaczenie"` i `"…: tłumaczenie "` wskazują ten sam słownik.

### Klucz słownika (`Domain/Services/Transformations/DictionaryKey`)

- `static bool TryFrom(object value, out string key)` to jedna funkcja zamiany na tekst, używana przy ładowaniu
  (wartość pola `items.key`) i przy wyszukiwaniu (wartość wejściowa mappera):
  - `decimal`: format jak `TransformationValue.ToText` (`6`, `6.5`, bez wiodących i końcowych zer),
  - `string`: bez zmian (z rozróżnianiem wielkości liter, bez przycinania),
  - `JsonNode` z wartością logiczną: `true`/`false`,
  - obiekt albo tablica: `false` (nie jest kluczem).
- Liczba `6` w polu i liczba `6` albo tekst `"6"` w kluczu słownika trafiają w ten sam wpis.

### Słownik (`Domain/ValueObjects/MapperDictionary`)

- Niezmienna tablica `FrozenDictionary<string, JsonNode>` z `bool TryGet(string key, out object value)`.
- Zwracana wartość ma postać transformacji (`TransformationValue.FromJson`, kopia węzła), więc liczba zostaje
  `decimal`, a obiekt `JsonNode`.
- `static MapperDictionary Build(IEnumerable<(object Key, JsonNode Value)> entries)`:
  - klucz `null` jest pomijany,
  - klucz, który nie jest skalarem, albo powtórzony klucz rzuca `DictionaryBuildException` z powodem
    (`invalid_key`, `duplicate_key`); powód służy tylko do logów.

### Port słowników (`Domain/Services/Transformations/IMapperDictionaries`)

```csharp
public interface IMapperDictionaries
{
    bool TryGet(DictionaryReference reference, out MapperDictionary dictionary);
}
```

### Transformacja `mapper` (`Domain/Services/Transformations/MapperTransformation`)

- `MapperTransformation(IMapperDictionaries dictionaries) : Transformation`
  - `Key = "mapper"`, `Name = "Słownik"`.
  - Opis: „Zamienia wartość na wartość ze słownika z kafelka.”
  - `Parameter = new("dictionary", TransformationParameterTypes.String)`.
- `Transform(value, parameter)` (dla `null` klasa bazowa zwraca `null` bez wywołania):
  1. Parametru nie da się odczytać przez `DictionaryReference.TryParse`: `invalid_parameter`.
  2. Wartości nie da się zamienić przez `DictionaryKey.TryFrom` (obiekt, tablica): `invalid_value`.
  3. Słownika nie ma w `IMapperDictionaries`: `dictionary_unavailable`.
  4. Klucza nie ma w słowniku: `missing_dictionary_key`.
  5. W przeciwnym razie zwraca wartość ze słownika.
- `TransformationFailureReasons` dostaje `MissingDictionaryKey = "missing_dictionary_key"` i
  `DictionaryUnavailable = "dictionary_unavailable"`.
- Pole-kolekcja (`$.statuses[*]`) jest mapowane element po elemencie. Jeden brakujący klucz daje błąd całego pola,
  zgodnie z obecnym `PreviewField`.

### Kwalifikacja kafelka jako słownika (`TileDefinition`)

`IReadOnlyList<string> DictionaryViolations(string valueLabel, TileContract contract)` zwraca listę naruszeń (pusta
lista oznacza, że kafelek może być słownikiem dla tej etykiety):

1. Kafelek ma `items`.
2. `valueLabel` jest polem elementu (`IsItemField`). Może to być pole złożone.
3. Kontrakt nie ma wymaganych parametrów (`TileContractParameter.Required`) ani wejść danych.
4. Kafelek nie ma paginacji.
5. Żadne pole kafelka nie ma kroku `mapper` (brak łańcuchów słowników, więc nie ma cykli ani kolejności ładowania).

`IEnumerable<DictionaryReference> DictionaryReferences()` zwraca odwołania ze wszystkich kroków `mapper` pól kafelka,
także pól złożonych. Kroki z nieczytelnym parametrem są pomijane.

`TileDefinition AsDictionarySource()` zwraca definicję do ładowania:
- tylko źródło `items.source`,
- pola elementu, czyli pola tego źródła oraz pola złożone,
- bez dzieci, akcji, wiązań i paginacji.

### Walidacja odwołań (`TileReferencesPolicy`)

Dla każdego kroku `mapper` w polu `fields[i].transformations[j]` naruszenie trafia pod
`fields[i].transformations[j].parameter`:

1. Parametr nie daje się odczytać: „Parametr transformacji 'mapper' ma format '<tileId>:<etykieta pola wartości>'.”
2. Wskazany kafelek nie istnieje (`TileTree`/`TileOf`): „Kafelek '<id>' nie istnieje.”
3. Wskazany kafelek to kafelek zapisywany (`tileId`): „Kafelek nie może być własnym słownikiem.”
4. Każde naruszenie z `DictionaryViolations(valueLabel, contract)` wskazanego kafelka.
5. Zapisywany kafelek ma kroki `mapper`, a sam jest słownikiem dla innych kafelków: zapis jest blokowany przez 409
   (sekcja „Blokady”), a nie przez tę walidację.

### Blokady (`Tile`)

- `Tile.Redefine(definition, fieldUses, dictionaryUses, contract)`. `dictionaryUses` to lista
  `TileDictionaryUse(TileReference Tile, string ValueLabel)` z repozytorium.
- Dla każdego użycia sprawdza `definition.DictionaryViolations(use.ValueLabel, contract)` oraz to, czy etykieta
  `items.key` nie zmieniła się względem obecnej definicji.
- Gdy któreś użycie jest naruszone, rzuca `DictionaryInUseException(tileId, valueLabels, tiles)`, co daje 409
  `dictionary_in_use`.
- `Tile.EnsureCanBeDeleted(parents, actionSources, dictionarySources)`: kafelki używające słownika dołączają do
  listy 409 `tile_in_use`.

## Kryteria akceptacji

1. Katalog transformacji zawiera `mapper` z nazwą, opisem i parametrem `dictionary` (`string`).
2. Po starcie aplikacji pole `$.status` z `mapper` wskazującym kafelek „Statusy zamówień” i pole „Tłumaczenie”
   zamienia `NEW` na „Nowe”. Podgląd nie wywołuje połączenia słownika.
3. Wartość słownika zachowuje typ: słownik z liczbą 8 dla klucza 6 zamienia liczbę 6 na liczbę 8. Kolejna
   transformacja `round`/`suffix` działa na liczbie.
4. Klucze porównywane są jako tekst: liczba 6 w polu trafia w klucz słownika `"6"`.
5. Pole słownika z transformacjami (np. `uppercase` na „Tłumaczenie”) daje wartości po transformacjach. Pole złożone
   może być polem wartości.
6. Pole-kolekcja z `mapper` jest mapowane element po elemencie.
7. Wartości, której nie ma w słowniku, odpowiada błąd pola `transformation_failed` (`transformation: mapper`,
   `reason: missing_dictionary_key`). Wiązanie zasilane tym polem daje dziecku `dependency_failed`. Pozostałe pola
   zwracają dane (200).
8. `null` na wejściu mappera zostaje `null` bez błędu.
9. Słownik, którego źródło nie odpowiadało przy starcie, daje `dictionary_unavailable`. Inne słowniki działają.
   Ponowny zapis kafelka-słownika ładuje go w tle i po załadowaniu mapper działa.
10. Słownik z powtórzonym kluczem albo z błędem pola klucza lub wartości w dowolnym elemencie się nie ładuje
    (`dictionary_unavailable`). Element z kluczem `null` jest pomijany.
11. Nowy kafelek z `mapper` wskazującym kafelek, który nie był jeszcze słownikiem, zleca załadowanie słownika w tle.
    Po załadowaniu podgląd go używa.
12. Zapis kafelka-słownika (np. nowa transformacja pola wartości) przeładowuje słownik w tle. Do końca ładowania
    działa poprzednia wersja, a nieudane przeładowanie daje `dictionary_unavailable`.
13. Ładowanie słownika pomija dzieci, akcje i inne źródła kafelka-słownika. Używa wartości domyślnych parametrów.
14. Zapis jest odrzucany (400) przy: złym formacie parametru `mapper`, nieistniejącym kafelku, odwołaniu do samego
    siebie, kafelku bez `items`, etykiecie spoza pól elementu, wymaganym parametrze lub wejściu w kontrakcie,
    paginacji i kafelku-słowniku używającym `mapper`.
15. Edycja kafelka-słownika, która usuwa albo zmienia etykietę pola wartości, zmienia `items.key`, usuwa `items`,
    dodaje wymagany parametr (np. nowym źródłem z połączenia) lub wejście danych, paginację albo `mapper`, daje 409
    `dictionary_in_use` z etykietami i kafelkami. Edycja niepsująca odwołań przechodzi.
16. Usunięcie kafelka-słownika daje 409 `tile_in_use` z kafelkami używającymi słownika.
17. Kafelek-słownik daje się wyświetlić podglądem jak każdy kafelek (jego połączenie jest wtedy wywoływane).
18. Podgląd niezapisanego kafelka z `mapper` używa załadowanych słowników. Niczego nie zleca do załadowania, więc
    słownik niezaładowany daje `dictionary_unavailable`, a parametr w złym formacie daje `invalid_parameter`
    (podgląd niezapisanej definicji nie przechodzi walidacji zapisu).
19. Uruchomienie aplikacji nie czeka na załadowanie słowników i nie zawodzi, gdy źródło słownika nie odpowiada.

## Endpoint

Nowych endpointów nie ma.

`GET /api/custom-data-view/transformations` zawiera:

```json
{ "key": "mapper", "name": "Słownik", "description": "Zamienia wartość na wartość ze słownika z kafelka.",
  "parameter": { "name": "dictionary", "type": "string" } }
```

Podgląd pola z brakującym kluczem i z niezaładowanym słownikiem:

```json
{ "label": "Status", "error": { "code": "transformation_failed", "transformation": "mapper", "reason": "missing_dictionary_key" } }
{ "label": "Status", "error": { "code": "transformation_failed", "transformation": "mapper", "reason": "dictionary_unavailable" } }
```

`PUT /api/custom-data-view/tiles/{id}` kafelka-słownika, który psuje odwołania (409):

```json
{ "code": "dictionary_in_use", "fields": ["Tłumaczenie"], "tiles": [ { "id": "…", "name": "Zamówienia" } ] }
```

`DELETE /api/custom-data-view/tiles/{id}` kafelka-słownika (409) zwraca dotychczasową odpowiedź `tile_in_use`
z kafelkami używającymi słownika.

## Walidacja wejścia

- `transformations[j].parameter` dla `mapper`: wymagany, format `"<GUID>:<etykieta>"`, etykieta 1–100 znaków po
  przycięciu. Liczba jako parametr jest zamieniana na tekst jak dotąd i nie przechodzi walidacji formatu.
- Pozostałe reguły w sekcji „Walidacja odwołań”.

## Handler i sekwencja wykonania

### Słowniki w pamięci (`Application/Dictionaries`)

- `MapperDictionaries : IMapperDictionaries` (singleton) trzyma
  `ConcurrentDictionary<DictionaryReference, MapperDictionary>`.
  - `Set(reference, dictionary)` podmienia cały słownik atomowo.
  - `Remove(reference)` usuwa słownik.
  - `Clear()` służy do resetu w testach integracyjnych.
- `DictionaryLoader` (singleton) wykonuje `Load(DictionaryReference reference, CancellationToken)`:
  1. Pobiera kafelek z `ITileRepository`. Gdy go nie ma albo `DictionaryViolations` jest niepuste: `Remove` i log
     ostrzeżenia.
  2. Wywołuje `TilePreviewer.Preview(definition.AsDictionarySource(), TilePreviewValues.Empty, …)`.
  3. Gdy jest `itemsError` albo dowolne pole klucza lub wartości ma błąd: `Remove`.
  4. `MapperDictionary.Build(elementy → (Key: wartość pola items.key, Value: wartość pola ValueLabel))`.
     `DictionaryBuildException` daje `Remove`.
  5. Sukces daje `Set`.
  - Kafelek z kilkoma polami wartości jest wykonywany raz na partię: `Load` grupuje odwołania po `TileId`.
  - Logi: identyfikator kafelka, etykieta, liczba wpisów, powód (`items_error`, `field_error`, `invalid_key`,
    `duplicate_key`). Bez wartości słownika i treści odpowiedzi.
- `IDictionaryLoadQueue` z `Enqueue(IEnumerable<DictionaryReference>)` ma implementację na `Channel` (unbounded,
  jeden konsument).
- `DictionaryLoadingService : BackgroundService`:
  1. Po starcie czyta `ITileRepository.GetAll()`, zbiera `DictionaryReferences()` wszystkich kafelków (bez
     powtórzeń) i ładuje je partiami po kafelku.
  2. Potem w pętli pobiera zlecenia z kolejki i ładuje je.
  - Wyjątek przy jednym słowniku nie zatrzymuje usługi: log i `Remove`.
  - Start aplikacji nie czeka na ładowanie (pętla w `ExecuteAsync` zaczyna się od `await Task.Yield()`).
- Rejestracja: `MapperTransformation` jako `ITransformation`, `MapperDictionaries` jako singleton i
  `IMapperDictionaries`, `DictionaryLoader`, `IDictionaryLoadQueue` i `AddHostedService<DictionaryLoadingService>()`.

### `AddTile`

1. Walidacja jak dotąd (`TileReferencesValidator` z nowymi regułami `mapper`).
2. `Tile.Create`, `repository.Add`.
3. Po zapisie `queue.Enqueue` odwołań `DictionaryReferences()` nowego kafelka, których nie ma w
   `IMapperDictionaries`.

### `UpdateTile`

1. `repository.Get` albo `NotFoundException`.
2. `referencesValidator.EnsureValid`.
3. `tile.Redefine(definition, repository.GetFieldUses(id), repository.GetDictionaryUses(id), contract)`. Kontrakt
   jest liczony z połączeń jak w `TileDefinition.Contract`. Psujące odwołania kończą się
   `DictionaryInUseException` (409).
4. `repository.Update`.
5. Po zapisie `queue.Enqueue`:
   - odwołań z własnych kroków `mapper`, których nie ma w pamięci,
   - wszystkich odwołań do tego kafelka z `GetDictionaryUses(id)` (przeładowanie słownika).

### `DeleteTile`

1. `tile.EnsureCanBeDeleted(parents, actionReferences, dictionaryUses.Select(use => use.Tile))`.
2. `repository.Delete`.
- Słowniki usuniętego kafelka nie mogą być używane (blokada), więc nie trzeba ich czyścić.

### Podgląd

- `TilePreviewer` bez zmian. `mapper` działa w istniejącym łańcuchu transformacji (`PreviewField`, `CombineField`,
  pola elementów).
- Podgląd zapisanego i niezapisanego kafelka niczego nie zleca do załadowania.

### API

- `CustomDataViewExceptionHandler` mapuje `DictionaryInUseException` na 409 `DictionaryInUseResponse(Code, Fields,
  Tiles)` z `Code = "dictionary_in_use"`.

## Baza danych

Brak zmian w schemacie. Parametr `mapper` jest zapisywany w istniejącej kolumnie `parameter`
`tile_field_transformations`. `db-plan.md` dostaje tylko notatkę o odczycie użyć słownika (zapytanie po
`key = 'mapper'` i prefiksie parametru).

## Repozytoria

- `ITileRepository.GetDictionaryUses(Guid tileId)` zwraca `IReadOnlyList<TileDictionaryUse>`, czyli kafelki
  (`TileReference`) i etykiety wartości z kroków `mapper`, których parametr wskazuje `tileId`.
  - `PostgresTileRepository`: zapytanie `AsNoTracking` po `tile_field_transformations`
    z `key == "mapper"` i `parameter.StartsWith($"{tileId:D}:")` (parametryzowane przez LINQ), złączone z `tiles`.
    Etykieta jest wycinana i normalizowana w pamięci, a wynik `Distinct` po kafelku i znormalizowanej etykiecie.
  - `TestTileRepository` (testy): ta sama logika na kafelkach w pamięci, przez `DictionaryReferences()`.
- Odczyt wszystkich kafelków przy starcie używa istniejącego `GetAll()`.

## Frontend

Brak zmian. Frontend nie jest częścią MVP, a kafelek-słownik wyświetla się jak każdy kafelek.

## Testy

### Testy jednostkowe

- **Domena (`DictionaryReference`)**: poprawny format, brak dwukropka, zły GUID, pusta i za długa etykieta,
  dwukropek w etykiecie, równość z normalizacją etykiet.
- **Domena (`DictionaryKey`)**: liczby (`6`, `6.50`, `6` i `"6"` dają ten sam klucz), tekst z wielkością liter,
  wartości logiczne, obiekt i tablica odrzucone.
- **Domena (`MapperDictionary.Build`)**: pominięcie `null`, powtórzony klucz, klucz nieskalarny, wartość liczbowa
  zachowuje typ, wartość obiektowa jest kopią.
- **Domena (`MapperTransformation`)** z prawdziwym `MapperDictionaries` w pamięci (bez mocków): trafienie,
  `missing_dictionary_key`, `dictionary_unavailable`, `invalid_parameter`, `invalid_value`, `null`.
- **Domena (`TileDefinition`)**: `DictionaryViolations` dla reguł 1–5, `DictionaryReferences` (także pola złożone,
  pominięcie złego parametru), `AsDictionarySource`.
- **Domena (`Tile`)**: `Redefine` z użyciami słownika (każde psujące odwołanie, zmiana `items.key`, edycja
  niepsująca), `EnsureCanBeDeleted` z użyciami słownika.
- **Aplikacja (`DictionaryLoader`)** z fałszywym połączeniem w pamięci: sukces, `itemsError`, błąd pola, powtórzony
  klucz, kilka pól wartości jednego kafelka w jednym wykonaniu, podmiana i usunięcie przy nieudanym przeładowaniu.

### Testy integracyjne

> Testy integracyjne dla tego scenariusza powstają na podstawie scenariuszy Gherkin w
> `src/CustomDataView/docs/scenarios/scenario_TEMP-045/gherkin.md` (testy w `Tiles/MapperTransformationTests`
> i `Transformations/TransformationCatalogTests`). Host testowy jest współdzielony, więc przed każdym testem
> `MapperDictionaries.Clear()`. Ładowanie w tle testy obserwują przez kolejkę testową, która ładuje synchronicznie
> albo pozwala poczekać na opróżnienie kolejki. Repozytorium: `GetDictionaryUses` (prefiks GUID, kilka etykiet,
> normalizacja, brak użyć). Do tego przykład `docs/foundation/rest/07-dictionaries.http` z fałszywym endpointem
> statusów oraz sekcja w README.
