- ID: TEMP-039  
  Tytuł: Wybór elementu  
  Opis: Jako użytkownik chcę, aby wybór elementu w kafelku z kolekcją zasilał inne kafelki, aby budować układy
  master–detail.  
  Kryteria akceptacji:
    - Zakres `selected` (domyślny dla węzła z `items`) jest wykonywany przez front.
    - Cele wiązań `selected` mają stan `awaiting_input` z `pending` (adres węzła źródłowego, pole, zakres, parametr
      albo wejście) i kompletem pozostałych `inputs`.
    - Front po wyborze woła podgląd kafelka osadzonego z uzupełnionymi wartościami.
