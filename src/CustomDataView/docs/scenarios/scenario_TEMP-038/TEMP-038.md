- ID: TEMP-038  
  Tytuł: Wiązania widoku  
  Opis: Jako użytkownik chcę łączyć kafelki widoku jawnymi wiązaniami i formularzami, aby wartości przechodziły między
  kafelkami bez płaskiej mapy parametrów.  
  Kryteria akceptacji:
    - Osadzenia widoku mają `key`; ten sam kafelek może wystąpić w widoku kilka razy.
    - Widok ma wiązania `from` → `to` (także ścieżka do kafelka zagnieżdżonego) z zakresami `selected` i `all`;
      walidacja jak w PRD 3.4.
    - Podgląd widoku przyjmuje tylko `forms`, wykonuje wiązania falami i zwraca osadzenia ze stanami; parametr bez
      wiązania ma tylko wartość domyślną.
    - Usunięcie kafelka będącego źródłem wiązania widoku i edycja usuwająca albo zmieniająca etykietę pola użytego w
      wiązaniu widoku są blokowane (409).
