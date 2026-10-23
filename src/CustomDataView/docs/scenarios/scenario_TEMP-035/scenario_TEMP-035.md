# #TEMP-035 — Scenariusz implementacji

## Cel

Wartości wpisywane przez użytkownika pochodzą z kafelka `display: form`. Formularz wystawia swoje pola jak każdy
kafelek, więc jest zwykłym źródłem wiązań (TEMP-036, TEMP-038).

## Domena i aplikacja

- `TileFormField(Label, Type, Required, Default)` i `TileDefinition.FormFields`; słownik `TileFormFieldTypes`.
- Żądanie kafelka: dla `display: form` sekcja `fields` zawiera `label`, `type`, `required`, `default`.
- `TileDefinitionValidator`: formularz bez źródeł, `items`, dzieci i wiązań; typ i zgodność `default`.
- `TileFormValues`: wartość pola = wartość z `forms` dla adresu osadzenia, inaczej `default`; zgodność z typem
  (`date` jako `yyyy-MM-dd`) albo błąd `invalid_value`.
- Żądanie podglądu: `forms` (adres osadzenia formularza → etykieta → wartość). Podgląd samego formularza bierze
  wartości spod pustego adresu (`""`).

## Baza danych

Migracja `AddTileFormFields`: tabela `tile_form_fields` z kolumną generowaną `normalized_label` i indeksem unikalnym.

## Testy

API: `src/CustomDataView/docs/scenarios/scenario_TEMP-035/gherkin.md`; repozytorium: zapis i odczyt pól formularza.
