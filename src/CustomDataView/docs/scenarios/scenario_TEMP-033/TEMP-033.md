- ID: TEMP-033  
  Tytuł: Źródła danych i wiązania źródło → źródło  
  Opis: Jako użytkownik chcę, aby kafelek pobierał dane z nazwanych źródeł, a wartość z odpowiedzi jednego połączenia
  zasilała parametr innego, aby budować łańcuchy zapytań bez frontu.  
  Kryteria akceptacji:
    - Kafelek ma listę źródeł z unikalnym kluczem; źródło `connection` wskazuje istniejące połączenie, a pole wskazuje
      źródło (`source`).
    - Wiązanie `from` (źródło) → `to` (źródło) z mapowaniem `field` → `parameter` przekazuje wartość pola po
      transformacjach do parametru połączenia docelowego.
    - Walidacja wiązań: węzły istnieją, para występuje raz, pole pochodzi ze źródła `from`, parametr jest
      zadeklarowany w połączeniu `to`, cel ma jedno źródło, brak cykli.
    - Podgląd wykonuje źródła falami według wiązań; błąd pola źródłowego daje polom celu `dependency_failed`, a
      parametr niezadeklarowany przez połączenie daje `invalid_binding`; `null` oznacza brak wartości.
    - Szczegóły kafelka zwracają kontrakt: wejścia-parametry niezwiązane wewnętrznie, wejścia danych i wyjścia.
