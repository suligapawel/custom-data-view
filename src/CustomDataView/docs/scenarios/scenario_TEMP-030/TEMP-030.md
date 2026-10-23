- ID: TEMP-030  
  Tytuł: Unikalna etykieta i ukryte pole  
  Opis: Jako użytkownik chcę, aby etykiety pól kafelka były unikalne i aby można było ukryć pole techniczne, aby
  wiązania jednoznacznie wskazywały pola, a widok nie pokazywał wartości pomocniczych.  
  Kryteria akceptacji:
    - Etykieta pola jest unikalna w kafelku; porównanie bez rozróżniania wielkości liter i po przycięciu białych
      znaków. Duplikat zwraca błąd walidacji pod kluczem pola.
    - Pole ma flagę `hidden` (domyślnie `false`), zapisywaną i zwracaną w szczegółach kafelka.
    - Podgląd kafelka zwraca pola ukryte z `"hidden": true`; front ich nie pokazuje.
    - Pole ukryte przechodzi przez transformacje.
