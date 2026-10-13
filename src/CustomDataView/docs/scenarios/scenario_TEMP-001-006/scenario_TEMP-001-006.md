# #TEMP-001-006 — Scenariusz implementacji

## Cel

Użytkownik może odczytać katalog predefiniowanych transformacji pól, a backend potrafi zastosować do wartości
uporządkowaną listę transformacji, jedna po drugiej. Jest to fundament techniczny pod kafelki i ich
podgląd (TEMP-015, TEMP-021 … TEMP-023).

## Kontekst biznesowy

Wartości z zewnętrznych API trzeba przedstawić w czytelnej postaci (zaokrąglenie, data, jednostka, wielkość liter,
skrócenie) bez zmian w kodzie przy każdej konfiguracji. Transformacje są zdefiniowane w kodzie i tylko do odczytu;
użytkownik wybiera je z katalogu i podaje wartości parametrów w polu kafelka.

## Domena

Folder `CustomDataView.Core/Transformations/`:

- `Abstractions/` — wspólne typy: `ITransformation`, `Transformation`, `TransformationParameters`,
  `TransformationParameterDescriptor`, `TransformationParameterTypes`, `TransformationValue`,
  `TransformationFailedException`, `TransformationFailureReasons`.
- `Implementations/` — 7 transformacji (np. `RoundTransformation.cs`); każda klasa jest jednocześnie wpisem katalogu
  i kodem wykonywalnym.
- `Catalog/` — katalog transformacji dla endpointu.
- `Execution/` — stosowanie listy transformacji do wartości.

### Zadanie 1 — Wspólne elementy (TEMP-002 … TEMP-006)

```csharp
public interface ITransformation
{
    string Key { get; }
    string Name { get; }
    string Description { get; }
    IReadOnlyList<TransformationParameterDescriptor> Parameters { get; }
    object Apply(object value, TransformationParameters parameters);
}
```

- Transformacja jest bezstanowym singletonem: ta sama instancja obsługuje wszystkie pola, a wartości parametrów z pola
  kafelka są przekazywane przy każdym wywołaniu `Apply`.
- `TransformationParameterDescriptor(Name, Type)`, gdzie `Type` to `integer` albo `string`; deskryptor jest prywatnym
  polem statycznym klasy transformacji i służy zarówno katalogowi, jak i odczytowi wartości parametru.
- Abstrakcyjna klasa `Transformation` przepuszcza `null` bez wywoływania logiki (`Transform`).
- Wartość w łańcuchu: `null`, `string`, `decimal`, `bool` albo `JsonNode` (obiekt/tablica).
  `TransformationValue.FromJson` zamienia wartość JSON na ten model.
- `TransformationFailedException(key, reason, parameter)`. Możliwe powody: `invalid_value`, `invalid_parameter`
  (z nazwą parametru) oraz `unknown_transformation`. Wiadomość wyjątku nie zawiera przetwarzanej wartości.
- `TransformationParameters` odczytuje parametry **przy wykonaniu**. Nadmiarowe
  parametry są ignorowane.
  - integer: liczba całkowita JSON (`2`) albo tekst złożony z samych cyfr (`"2"`); każda inna wartość lub jej brak daje
    `invalid_parameter`,
  - string: string JSON (także `""`) albo liczba zamieniona na tekst; `bool`, obiekt, `null` lub brak dają
    `invalid_parameter`.
- `ToText`: `decimal` → `"0.############################"` w kulturze niezmiennej (`21.50` → `"21.5"`), `bool` →
  `"true"`/`"false"`, obiekt/tablica → `invalid_value`.
- `ToNumber`: tekst po obcięciu spacji, najwyżej jeden separator dziesiętny (`.` lub `,`), wykładnik dozwolony, bez
  separatorów tysięcy; `bool`, obiekt i tekst nieliczbowy dają `invalid_value`.

Każde z zadań 2-5 dodaje klasę `XxxTransformation` i rejestruje ją w DI jako singleton `ITransformation`. Nazwy i opisy są po polsku; klucze i nazwy parametrów w
`snake_case`.

### Zadanie 2 — `round(decimals: integer)` (TEMP-002)

- Nazwa i opis: „Zaokrąglenie” — „Zaokrągla liczbę do podanej liczby miejsc po przecinku (0-10).”
- `decimals` w zakresie 0-10, inaczej `invalid_parameter`. `Math.Round(..., MidpointRounding.AwayFromZero)`, wynik
  jest liczbą `decimal`.

### Zadanie 3 — `date_format(pattern: string)` (TEMP-003)

- Nazwa i opis: „Formatowanie daty” — „Formatuje datę ISO 8601 według wzorca w kulturze niezmiennej.”
- Wejście: tekst ISO 8601 (`yyyy-MM-dd` albo data z godziną, opcjonalnie `Z` lub `±HH:mm`), parsowany przez
  `DateTimeOffset.TryParseExact` w kulturze niezmiennej. Offset źródła jest zachowany, a jego brak oznacza UTC.
- Liczba, `bool`, obiekt lub tekst inny niż ISO dają `invalid_value`. Pusty `pattern` albo `FormatException` przy
  formatowaniu dają `invalid_parameter` (`pattern`).

### Zadanie 4 — `prefix(text: string)` / `suffix(text: string)` (TEMP-004)

- Nazwy i opisy: „Prefiks” — „Dokleja tekst przed wartością.”; „Sufiks” — „Dokleja tekst po wartości.”
- `text` + `ToText(value)` albo `ToText(value)` + `text`; pusty `text` jest dozwolony.

### Zadanie 5 — `uppercase` / `lowercase` / `truncate(max_length: integer)` (TEMP-005)

- Nazwy i opisy: „Wielkie litery”, „Małe litery” (bez parametrów, pusta lista) oraz „Przycięcie tekstu” — „Przycina tekst
  do maksymalnej liczby znaków.”
- `ToUpperInvariant` / `ToLowerInvariant` na `ToText(value)`.
- `truncate`: `max_length` ≥ 1, inaczej `invalid_parameter`; zwykły `Substring(0, max_length)` bez wielokropka.
  Krótszy tekst zostaje bez zmian.

### Zadanie 6 — Katalog (TEMP-001)

- Folder `Transformations/Catalog/`: `ITransformationCatalogService`, `TransformationCatalogService`,
  `TransformationDto`, `TransformationParameterDto`.
- `TransformationCatalogService` (singleton) przyjmuje `IEnumerable<ITransformation>` z DI; `GetAll()` zwraca katalog
  jako `TransformationDto` posortowane po kluczu (ordinalnie), tylko do odczytu.
- Katalog nie ma operacji zapisu. Nowa transformacja to nowa klasa i jej rejestracja w DI.

### Zadanie 7 — Łańcuch transformacji (TEMP-006)

- Stosowanie transformacji nie jest częścią katalogu: `ITransformationService` / `TransformationService` (singleton)
  w `Transformations/Execution/` przyjmuje `IEnumerable<ITransformation>` z DI; tam też leżą `TransformationStep`,
  `TransformationResult` i `TransformationError`.
- `TransformationStep(Key, Parameters)` to jeden element listy z pola kafelka.
- `Apply(value, steps)` przechodzi po krokach w kolejności listy: wyszukuje transformację po kluczu i wywołuje
  `transformation.Apply(value, parameters)`. Nieznany klucz kończy się `unknown_transformation`. Powtórzenia są
  dozwolone.
- Wynik `TransformationResult`: sukces z wartością albo `TransformationError` `transformation_failed` z kluczem
  i powodem (bez pozycji). Pierwszy błąd przerywa łańcuch. Inne wyjątki nie są przechwytywane.

## Kryteria akceptacji

- [ ] Katalog zwraca 7 transformacji posortowanych po kluczu, każdą z kluczem, nazwą, opisem i parametrami
  (nazwa, typ).
- [ ] Katalog jest tylko do odczytu: nie ma operacji dodania, edycji ani usunięcia (405).
- [ ] `round(2)`: `21.4567` → `21.46`; `2.5` z `round(0)` → `3`; `"21,4567"` i `" 21.4567 "` → `21.46`.
- [ ] `round` na `"abc"`, `"1,234.5"` lub `true` kończy się `transformation_failed` (`round`, `invalid_value`).
- [ ] `round` bez `decimals` lub z `decimals` spoza 0-10 kończy się `transformation_failed` (`round`,
  `invalid_parameter`).
- [ ] `date_format("yyyy-MM-dd HH:mm")`: `"2026-10-13T10:00:00+02:00"` → `"2026-10-13 10:00"`; sama data `"2026-10-13"`
  daje godzinę `00:00`.
- [ ] `date_format` na `"jutro"` lub liczbie kończy się `invalid_value`; pusty lub brakujący `pattern` kończy się
  `invalid_parameter`.
- [ ] `suffix(" °C")`: `21.5` → `"21.5 °C"`; `prefix("ID: ")`: `7` → `"ID: 7"`; brak `text` kończy się
  `invalid_parameter`.
- [ ] `uppercase`: `"abc"` → `"ABC"`; `lowercase`: `"ABC"` → `"abc"`; `truncate(3)`: `"abcdef"` → `"abc"`, a `"ab"`
  zostaje `"ab"`.
- [ ] `truncate` bez `max_length` lub z `max_length` < 1 kończy się `invalid_parameter`.
- [ ] Liczba trafiająca do transformacji tekstowej jest zamieniana na tekst w kulturze niezmiennej (`21.50` →
  `"21.5"`).
- [ ] `null` przechodzi przez każdą transformację bez zmian; obiekt/tablica kończy się `invalid_value`.
- [ ] Łańcuch `round(1)` → `suffix(" °C")` na `21.46` daje `"21.5 °C"`.
- [ ] Pusta lista transformacji zwraca wartość bez zmian.
- [ ] Błąd dowolnej transformacji przerywa łańcuch i zwraca `transformation_failed` z kluczem tej transformacji.
- [ ] Nieznany klucz w łańcuchu kończy się `transformation_failed` z powodem `unknown_transformation`.
- [ ] Ta sama transformacja może wystąpić w łańcuchu kilka razy (`prefix("a")`, `prefix("b")` na `"x"` → `"bax"`).

## Endpoint

`GET /api/custom-data-view/transformations` → `ITransformationCatalogService.GetAll()` →
`IReadOnlyList<TransformationDto>` (200), bez opakowania w `ApiResponse`.

```json
[ { "key": "date_format", "name": "Formatowanie daty", "description": "…",
    "parameters": [ { "name": "pattern", "type": "string" } ] } ]
```

Pozostałe transformacje nie mają endpointu; ich efekt widać w podglądzie kafelka.

## Walidacja wejścia

Endpoint katalogu nie przyjmuje danych wejściowych. Parametry transformacji są sprawdzane przy wykonaniu (Zadanie 1).

## Handler i sekwencja wykonania

1. Endpoint (minimal API) wstrzykuje `ITransformationCatalogService`.
2. Serwis zwraca transformacje z DI zmapowane na `TransformationDto`.
3. Endpoint zwraca listę z kodem 200. `CustomDataView.Bootstrapper` rejestruje usługi i mapuje endpoint.

## Baza danych

Brak zmian.

## Repozytoria

Brak zmian.

## Frontend

Brak zmian.

## Testy

### Testy jednostkowe

Testy w NUnit bez mocków, na prawdziwych obiektach: katalog (serwis z prawdziwej rejestracji DI), `TransformationParameters`, `TransformationValue`,
każda transformacja (przypadki z kryteriów akceptacji) oraz łańcuch (kolejność, pusta lista, przerwanie, nieznany klucz,
powtórzenia).

### Testy integracyjne

> Nie dopisuj testów integracyjnych.

Projekty testowe wymagają pakietów NuGet (NUnit, host testowy), więc przed implementacją potrzebna jest zgoda.
