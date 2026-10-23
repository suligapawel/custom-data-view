- ID: TEMP-044  
  Tytuł: Kombinatory pól  
  Opis: Jako użytkownik chcę złożyć jedno pole kafelka z kilku innych pól (sklejony tekst, tekst ze wzorca, obiekt albo
  pierwsza dostępna wartość), aby pokazywać np. pełny adres w jednej kolumnie i przekazywać złożone wartości
  wiązaniami bez zmian w kodzie.  
  Kryteria akceptacji:
    - Katalog kombinatorów w kodzie (`join`, `template`, `object`, `coalesce`) jest dostępny do odczytu z kluczem,
      nazwą, opisem, parametrem i wymaganą liczbą pól.
    - Pole kafelka ma albo `source` i `path`, albo `combine` (`key`, opcjonalny `parameter`, opcjonalne `fields`);
      wejściami są etykiety innych pól kafelka (wartości po ich transformacjach), a kombinator sam interpretuje parametr
      i listę pól.
    - Wynik pola złożonego przechodzi przez łańcuch transformacji (poza `object`), może być ukryty, mieć rolę, należeć
      do kontraktu i zasilać wiązania z `parent` i z dziecka oraz akcje.
    - W kafelku z elementami pole złożone jest wyliczane dla każdego elementu z pól źródła elementów.
    - Błąd pola wejściowego daje `dependency_failed` z przyczyną, a wartość, której kombinator nie umie złożyć,
      daje `combination_failed`.
    - Zapis jest odrzucany przy nieznanym kombinatorze, nieznanym, złożonym albo własnym polu wejściowym, niepoprawnym
      parametrze lub liczbie pól, polu-tablicy w `template`, transformacjach przy `object`, wejściu spoza źródła
      elementów i polu złożonym jako `items.key`.
