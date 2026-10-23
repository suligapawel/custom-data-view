- ID: TEMP-031  
  Tytuł: Walidacja parametrów per połączenie  
  Opis: Jako użytkownik chcę, aby brakujący lub błędny parametr jednego połączenia nie blokował podglądu pozostałych
  pól, aby widzieć wszystkie dane, które da się pobrać.  
  Kryteria akceptacji:
    - Parametry każdego połączenia są walidowane osobno, tuż przed jego wywołaniem.
    - Brak wymaganego parametru albo zły typ daje pola tego połączenia z błędem `invalid_parameters` ze szczegółami
      (parametr, komunikat, czy wartość pochodzi z wiązania); połączenie nie jest wywoływane.
    - Pozostałe pola kafelka zwracają dane, a podgląd kafelka (zapisanego i niezapisanego, POST i GET) ma status 200.
