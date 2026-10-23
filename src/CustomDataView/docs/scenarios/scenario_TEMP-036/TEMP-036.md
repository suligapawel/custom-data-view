- ID: TEMP-036  
  Tytuł: Kafelki złożone: dzieci scope tile  
  Opis: Jako użytkownik chcę osadzać w kafelku inne samodzielne kafelki i wiązać je z rodzicem, aby budować złożone
  komponenty wielokrotnego użytku.  
  Kryteria akceptacji:
    - Kafelek ma osadzenia dzieci (`key`, `tileId`, `position`, `cssClass`, `scope: tile`, `load`) z unikalnymi
      kluczami i pozycjami; brak cykli i najwyżej 3 poziomy.
    - Wiązania `parent` → dziecko, dziecko → dziecko, dziecko → źródło (np. formularz filtrujący połączenie rodzica) i
      cele wskazane ścieżką do potomków (`descendants` w żądaniu).
    - Podgląd wykonuje dzieci `eager` rekurencyjnie i zwraca osadzenia z `address` i stanem (`data`, `awaiting_input`,
      `awaiting_load`, `error`) z kompletem `inputs`.
    - Usunięcie kafelka osadzonego jako dziecko jest blokowane (409 `tile_in_use`); edycja usuwająca albo zmieniająca
      etykietę pola użytego w wiązaniu rodzica jest blokowana (409 `field_in_use`).
