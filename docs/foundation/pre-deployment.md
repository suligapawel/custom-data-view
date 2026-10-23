# Przed wdrożeniem: kafelki złożone i migracje EF Core

Lista kroków przed wdrożeniem zmian z gałęzi `master` od commita `39ab313` (kafelki złożone, wiązania, usunięcie
widoków, przejście na migrację EF Core `Initial`). Każdy krok na bazie docelowej wymaga zgody właściciela bazy.

## 1. Kopia zapasowa

- Wykonaj kopię schematu `custom_data_view` (`pg_dump -n custom_data_view`). Nowy schemat nie jest zgodny ze starym,
  a dane trzeba odtworzyć (kroki 3–7).

## 2. Narzędzia

- `dotnet tool restore` — przywraca `dotnet-ef` 10.0.4 z `dotnet-tools.json`.
- Zbuduj rozwiązanie i uruchom testy, także repozytoriów na bazie testowej:

  ```bash
  CUSTOM_DATA_VIEW_TEST_DATABASE="Host=...;Database=custom_data_view_test;..." dotnet test CustomDataView.sln
  ```

## 3. Rodzaj bazy docelowej

Schemat tworzy jedna migracja EF Core `Initial`. Nie przenosi danych ze schematu utworzonego dawnymi skryptami SQL
(`create_connections`, `create_tiles`, `create_views`): tam pola kafelków wskazywały połączenie, a widoki miały
własne tabele.

- **Nowa, pusta baza** — przejdź do kroku 6.
- **Baza z dawnym schematem i danymi** — wykonaj kroki 4 i 5, potem utwórz schemat od nowa (krok 6) i odtwórz
  połączenia oraz kafelki przez API (wzór: `docs/foundation/rest/`).

## 4. Eksport danych ze starego schematu

```sql
-- połączenia z parametrami
SELECT * FROM custom_data_view.connections ORDER BY created_at, id;
SELECT * FROM custom_data_view.connection_path_parameters ORDER BY connection_id, name;
SELECT * FROM custom_data_view.connection_query_parameters ORDER BY connection_id, name;
SELECT * FROM custom_data_view.connection_body_parameters ORDER BY connection_id, name;

-- kafelki z polami i transformacjami
SELECT t.id, t.name, f.position, f.connection_id, f.path, f.label, tr.position AS step, tr.key, p.name AS parameter, p.value
FROM custom_data_view.tiles t
JOIN custom_data_view.tile_fields f ON f.tile_id = t.id
LEFT JOIN custom_data_view.tile_field_transformations tr ON tr.tile_id = f.tile_id AND tr.field_position = f.position
LEFT JOIN custom_data_view.tile_field_transformation_parameters p
       ON p.tile_id = tr.tile_id AND p.field_position = tr.field_position AND p.transformation_position = tr.position
ORDER BY t.created_at, t.id, f.position, tr.position, p.name;

-- widoki (odtwarzane jako kafelki złożone: korzeń z dziećmi)
SELECT v.id, v.name, vt.tile_id, vt.position, vt.css_class
FROM custom_data_view.views v
LEFT JOIN custom_data_view.view_tiles vt ON vt.view_id = v.id
ORDER BY v.created_at, v.id, vt.position;
```

## 5. Przygotowanie danych do odtworzenia

- Etykiety pól muszą być unikalne w kafelku (bez rozróżniania wielkości liter i białych znaków na brzegach):

  ```sql
  SELECT tile_id, lower(btrim(label)) AS normalized_label, count(*)
  FROM custom_data_view.tile_fields
  GROUP BY tile_id, lower(btrim(label))
  HAVING count(*) > 1;
  ```

  Duplikatom nadaj nowe etykiety przed odtworzeniem.
- Każdy stary kafelek dostaje `display: "details"` i jedno źródło `connection` na każde użyte połączenie
  (`source_1`, `source_2`, … w kolejności pierwszego użycia); pole wskazuje źródło (`source`) zamiast `connectionId`.
- Każdy stary widok staje się kafelkiem złożonym: kafelki widoku są jego dziećmi (`key`, `position`, `cssClass`),
  a wartości parametrów przekazują jawne wiązania albo formularz-dziecko.

## 6. Utworzenie schematu

- Wygeneruj skrypt idempotentny i przejrzyj go:

  ```bash
  dotnet ef migrations script --idempotent \
    --project src/CustomDataView/CustomDataView.Infrastructure -o custom-data-view-schema.sql
  ```

- Na bazie z dawnym schematem najpierw usuń stary schemat (`DROP SCHEMA custom_data_view CASCADE`) — dopiero po
  eksporcie z kroku 4.
- Po akceptacji uruchom skrypt albo `dotnet ef database update --project
  src/CustomDataView/CustomDataView.Infrastructure --connection "<connection string>"`. Aplikacja nie uruchamia
  migracji sama przy starcie.

## 7. Odtworzenie danych

- Odtwórz połączenia (`POST /api/custom-data-view/connections`), potem kafelki od liści do korzeni
  (`POST /api/custom-data-view/tiles`) — identyfikatory się zmienią, więc odwołania (`connectionId`, `tileId`) bierz
  z odpowiedzi, tak jak w `docs/foundation/rest/*.http`.
- Sprawdź liczby: tyle samo połączeń i kafelków (plus kafelki-korzenie zastępujące widoki) co w eksporcie.

## 8. Zmiany niezgodne wstecz w API (poinformuj klientów)

| Obszar | Było | Jest |
|---|---|---|
| Zapis kafelka | pole z `connectionId` | `sources` z kluczami, pole z `source`; wymagany `display` |
| Etykiety pól | mogły się powtarzać | unikalne w kafelku (400 przy duplikacie) |
| Błąd parametrów w podglądzie | 400 z błędem walidacji | 200, pola połączenia z błędem `invalid_parameters` |
| Odpowiedź podglądu | `id`, `name`, `fields` | dodatkowo `display`, `items`/`itemKey`/`itemsError`, `children` |
| Treść podglądu | płaska mapa `parameters` | `parameters`, `inputs`, `forms`, `descendants` (GET: tylko `parameters`) |
| Widoki | `/api/custom-data-view/views` | usunięte (404); rolę widoku pełni kafelek złożony |
| Usunięcie kafelka | kaskadowo z widoków | 409 `tile_in_use`, gdy kafelek jest dzieckiem innego kafelka |
| Edycja kafelka | bez ograniczeń | 409 `field_in_use`, gdy zmienia etykietę pola używanego w wiązaniu rodzica |

## 9. Konfiguracja

- Brak nowych ustawień. Limity podglądu (50 elementów, 200 wywołań, 10 s, 8 równoległych wywołań) są stałymi
  w kodzie (`TilePreviewer`, `PreviewContext`).
- Endpointy `FakeWeatherEndpoints` działają tylko w środowisku `Development`; na środowisku docelowym przykłady
  z `docs/foundation/rest` wymagają podmiany adresów połączeń na prawdziwe API.

## 10. Po wdrożeniu

- `GET /api/custom-data-view/tiles` — lista kafelków działa, liczba pól się zgadza.
- `GET /api/custom-data-view/tiles/{id}` dla kilku istniejących kafelków — `display`, `sources` i `contract`.
- Podgląd jednego istniejącego kafelka z jego dotychczasowymi parametrami — status 200 i te same wartości pól co przed
  wdrożeniem.
- Usunięcie połączenia używanego przez kafelek nadal zwraca 409 `connection_in_use`.
