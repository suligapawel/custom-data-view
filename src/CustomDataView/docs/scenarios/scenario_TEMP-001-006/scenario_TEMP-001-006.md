# #TEMP-001-006 — Scenariusz implementacji

## Cel

Użytkownik może odczytać katalog predefiniowanych transformacji pól, a backend potrafi zastosować do wartości
uporządkowaną listę transformacji złożoną w łańcuch dekoratorów. Jest to fundament techniczny pod kafelki i ich
podgląd (TEMP-015, TEMP-021 … TEMP-023).

## Kontekst biznesowy

Wartości z zewnętrznych API trzeba przedstawić w czytelnej postaci (zaokrąglenie, data, jednostka, wielkość liter,
skrócenie) bez zmian w kodzie przy każdej konfiguracji. Transformacje są zdefiniowane w kodzie i tylko do odczytu;
użytkownik wybiera je z katalogu i podaje wartości parametrów w polu kafelka.

## Domena

Folder `CustomDataView.Core/Transformations/`. Każda transformacja ma własny folder (np. `Transformations/Round/`), w
którym leżą jej definicja i klasa zachowania. Wszystko o danej transformacji jest w jednym miejscu.

### Zadanie 1 — Wspólne elementy (TEMP-002 … TEMP-006)

- `ITransformation { string Key; object Apply(object value); }` — zachowanie (dekorator).
- `ITransformationDefinition` — defi+nicja należąca do transformacji: metadane katalogu i fabryka.

```csharp
public interface ITransformationDefinition
{
    string Key { get; }
    string Name { get; }
    string Description { get; }
    IReadOnlyList<TransformationParameterDescriptor> Parameters { get; }
    ITransformation Create(ITransformation inner, TransformationParameters parameters);
}
```

- `TransformationParameterDescriptor(Name, Type)`, gdzie `Type` to `integer` albo `string`.
- Klucz transformacji jest stałą w jej definicji (np. `RoundTransformationDefinition.TransformationKey`); klasa
  zachowania używa tej samej stałej.
- Wartość w łańcuchu: `null`, `string`, `decimal`, `bool` albo `JsonNode` (obiekt/tablica).
  `TransformationValue.FromJson` zamienia wartość JSON na ten model.
- `IdentityTransformation` zwraca wartość bez zmian. Abstrakcyjna `DecoratorTransformation(inner, parameters)`
  wywołuje `inner.Apply`, a `null` przepuszcza dalej bez własnej logiki.
- `TransformationFailedException(key, reason, parameter)`. Możliwe powody: `invalid_value`, `invalid_parameter`
  (z nazwą parametru) oraz `unknown_transformation`. Wiadomość wyjątku nie zawiera przetwarzanej wartości.
- `TransformationParameters` odczytuje parametry **przy wykonaniu** (`Create` niczego nie waliduje). Nadmiarowe
  parametry są ignorowane.
  - integer: liczba całkowita JSON (`2`) albo tekst złożony z samych cyfr (`"2"`); każda inna wartość lub jej brak daje
    `invalid_parameter`,
  - string: string JSON (także `""`) albo liczba zamieniona na tekst; `bool`, obiekt, `null` lub brak dają
    `invalid_parameter`.
- `ToText`: `decimal` → `"0.############################"` w kulturze niezmiennej (`21.50` → `"21.5"`), `bool` →
  `"true"`/`"false"`, obiekt/tablica → `invalid_value`.
- `ToNumber`: tekst po obcięciu spacji, najwyżej jeden separator dziesiętny (`.` lub `,`), wykładnik dozwolony, bez
  separatorów tysięcy; `bool`, obiekt i tekst nieliczbowy dają `invalid_value`.

Każde z zadań 2-5 dodaje dla każdej transformacji parę `XxxTransformationDefinition` + `XxxTransformation` i rejestruje
definicję w DI jako singleton `ITransformationDefinition`. Nazwy i opisy są po polsku; klucze i nazwy parametrów w
`snake_case`.

### Zadanie 2 — `round(decimals: integer)` (TEMP-002)

- Definicja: „Zaokrąglenie” — „Zaokrągla liczbę do podanej liczby miejsc po przecinku (0-10).”
- `decimals` w zakresie 0-10, inaczej `invalid_parameter`. `Math.Round(..., MidpointRounding.AwayFromZero)`, wynik
  jest liczbą `decimal`.

### Zadanie 3 — `date_format(pattern: string)` (TEMP-003)

- Definicja: „Formatowanie daty” — „Formatuje datę ISO 8601 według wzorca w kulturze niezmiennej.”
- Wejście: tekst ISO 8601 (`yyyy-MM-dd` albo data z godziną, opcjonalnie `Z` lub `±HH:mm`), parsowany przez
  `DateTimeOffset.TryParseExact` w kulturze niezmiennej. Offset źródła jest zachowany, a jego brak oznacza UTC.
- Liczba, `bool`, obiekt lub tekst inny niż ISO dają `invalid_value`. Pusty `pattern` albo `FormatException` przy
  formatowaniu dają `invalid_parameter` (`pattern`).

### Zadanie 4 — `prefix(text: string)` / `suffix(text: string)` (TEMP-004)

- Definicje: „Prefiks” — „Dokleja tekst przed wartością.”; „Sufiks” — „Dokleja tekst po wartości.”
- `text` + `ToText(value)` albo `ToText(value)` + `text`; pusty `text` jest dozwolony.

### Zadanie 5 — `uppercase` / `lowercase` / `truncate(max_length: integer)` (TEMP-005)

- Definicje: „Wielkie litery”, „Małe litery” (bez parametrów, pusta lista) oraz „Przycięcie tekstu” — „Przycina tekst
  do maksymalnej liczby znaków.”
- `ToUpperInvariant` / `ToLowerInvariant` na `ToText(value)`.
- `truncate`: `max_length` ≥ 1, inaczej `invalid_parameter`; zwykły `Substring(0, max_length)` bez wielokropka.
  Krótszy tekst zostaje bez zmian.

### Zadanie 6 — Katalog (TEMP-001)

- `ITransformationCatalog` / `TransformationCatalog` (singleton) przyjmuje `IEnumerable<ITransformationDefinition>`
  z DI i nie zna konkretnych transformacji.
- `GetAll()` zwraca definicje posortowane po kluczu (ordinalnie); `Find(key)` zwraca definicję albo `null`.
- Zduplikowany klucz rzuca wyjątek przy tworzeniu katalogu (błąd programisty wykrywany przy starcie).
- Katalog nie ma operacji zapisu. Nowa transformacja to nowy folder z definicją i klasą, bez zmian w katalogu.

### Zadanie 7 — Łańcuch dekoratorów (TEMP-006)

- `TransformationStep(Key, Parameters)` to jeden element listy z pola kafelka.
- `TransformationChainBuilder.Build(steps)` zaczyna od `IdentityTransformation`, a dla każdego kroku wywołuje
  `catalog.Find(key).Create(poprzedni, parameters)`. Nieznany klucz daje krok, który przy wykonaniu rzuca
  `unknown_transformation`. Powtórzenia są dozwolone.
- `TransformationChainExecutor.Execute(value, steps)` zwraca `TransformationChainResult`: sukces z wartością albo
  `transformation_failed` z kluczem i powodem (bez pozycji). Pierwszy błąd przerywa łańcuch. Inne wyjątki nie są
  przechwytywane.

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

`GET /api/custom-data-view/transformations` → `IQueryDispatcher.Handle(GetTransformationsQuery)` →
`ApiResponse<IReadOnlyList<TransformationDto>>` (200).

```json
{ "data": [ { "key": "date_format", "name": "Formatowanie daty", "description": "…",
              "parameters": [ { "name": "pattern", "type": "string" } ] } ] }
```

Pozostałe transformacje nie mają endpointu; ich efekt widać w podglądzie kafelka.

## Walidacja wejścia

Endpoint katalogu nie przyjmuje danych wejściowych (`GetTransformationsRequest.Validate()` jest pusty). Parametry
transformacji są sprawdzane przy wykonaniu (Zadanie 2).

## Handler i sekwencja wykonania

1. Endpoint mapuje `GetTransformationsRequest` na `GetTransformationsQuery`.
2. Handler pobiera `ITransformationCatalog.GetAll()` i mapuje definicje (`Key`, `Name`, `Description`, `Parameters`)
   na `TransformationDto`.
3. Endpoint zwraca `ApiResponse` z kodem 200.

Repozytorium nie ma jeszcze infrastruktury współdzielonej (`IQueryDispatcher`, `ApiResponse`, bootstrapper), więc
trzeba ją dodać zgodnie z `AGENTS.md` przed endpointem katalogu.

## Baza danych

Brak zmian.

## Repozytoria

Brak zmian.

## Frontend

Brak zmian.

## Testy

### Testy jednostkowe

Testy w NUnit bez mocków, na prawdziwych obiektach: katalog (zbudowany z listy prawdziwych definicji; także duplikat
klucza), metadane każdej definicji, `TransformationParameters`, `TransformationValue`,
każda transformacja (przypadki z kryteriów akceptacji) oraz łańcuch (kolejność, pusta lista, przerwanie, nieznany klucz,
powtórzenia).

### Testy integracyjne

> Nie dopisuj testów integracyjnych.

Projekty testowe wymagają pakietów NuGet (NUnit, host testowy), więc przed implementacją potrzebna jest zgoda.
