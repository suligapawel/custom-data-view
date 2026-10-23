- ID: TEMP-032  
  Tytuł: Typ wyświetlania i elementy  
  Opis: Jako użytkownik chcę określić typ wyświetlania kafelka i wskazać kolekcję elementów, aby front wiedział, jak
  pokazać dane.  
  Kryteria akceptacji:
    - `display` jest wymagany: `table`, `list`, `details`, `summary` (formularz w TEMP-035); istniejące kafelki mają
      `details`.
    - Kafelek `table` i `list` wymaga `items` (`source`, `path` z wildcardem, `key` wskazujący etykietę pola);
      `details` i `summary` nie mają `items`.
    - Role pól: `title`, `subtitle`, `badge` dla `list` (wymagana rola `title`), `primary`, `secondary` dla `summary`
      (wymagane co najmniej jedno `primary`); niezgodna rola to błąd walidacji.
    - Podgląd kafelka z `items` zwraca `itemKey` i `items` (klucz i pola względne wobec elementu) albo `itemsError`;
      kafelek bez `items` zwraca `fields`.
