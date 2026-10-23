- ID: TEMP-037  
  Tytuł: Dzieci scope item i limity podglądu  
  Opis: Jako użytkownik chcę osadzić kafelek w każdym elemencie kolekcji rodzica, aby pokazać szczegóły każdego
  wiersza, bez ryzyka lawiny wywołań.  
  Kryteria akceptacji:
    - Dziecko `scope: item` (tylko u rodzica z `items`) ma instancję na element; wiązanie `parent` ze `scope: item`
      przekazuje pole bieżącego elementu.
    - Adresy instancji zawierają klucz elementu (`[17]/lines`); dzieci `lazy` mają stan `awaiting_load` z `inputs`.
    - Limity: 50 elementów na osadzenie `eager` ze `scope: item`, 200 wywołań na żądanie, 10 s na żądanie, 8
      równoległych instancji; niewykonane osadzenia dostają `awaiting_load`.
    - Wywołanie tego samego połączenia z tymi samymi wartościami jest wykonywane raz na całe żądanie.
