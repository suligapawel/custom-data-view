- ID: TEMP-035  
  Tytuł: Kafelek formularz  
  Opis: Jako użytkownik chcę zdefiniować kafelek-formularz, którego pola zasilają wiązania innych kafelków, aby
  wartości wpisywane przez użytkownika miały jawne źródło.  
  Kryteria akceptacji:
    - Kafelek `display: form` ma pola formularza (`label`, `type`: `text`, `number`, `boolean`, `date`, `required`,
      `default`) i nie ma źródeł, `items`, dzieci ani wiązań.
    - `default` musi pasować do typu; etykiety są unikalne.
    - Żądanie podglądu przyjmuje `forms` według adresu osadzenia formularza; podgląd formularza zwraca pola z typem,
      wymaganiem, wartością domyślną i bieżącą wartością.
    - Wartość niezgodna z typem daje błąd pola `invalid_value`, a pole wymagane bez wartości i bez `default`
      wstrzymuje cele wiązań (`awaiting_input`).
