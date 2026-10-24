- ID: TEMP-045  
  Tytuł: Transformacja mapper ze słownikami z kafelków  
  Opis: Jako użytkownik chcę zamienić wartość pola na wartość ze słownika pobranego innym kafelkiem (np. kod statusu
  na jego tłumaczenie albo liczbę 6 na 8), aby pokazywać czytelne wartości bez zmian w kodzie i bez wywoływania
  endpointu słownika przy każdym podglądzie.  
  Kryteria akceptacji:
    - Katalog transformacji zawiera `mapper` z parametrem `"<tileId>:<etykieta pola wartości>"`.
    - Słownikiem jest zwykły kafelek z `items`: kluczem jest wartość pola `items.key`, a wartością wartość pola
      elementu wskazanego w parametrze (po transformacjach). Kafelek-słownik można nadal wyświetlać jak każdy kafelek.
    - Słowniki są ładowane do pamięci raz po starcie aplikacji oraz w tle po zapisie kafelka-słownika albo kafelka
      z niezaładowanym odwołaniem `mapper`; start i zapis nie czekają na źródła słowników.
    - Klucze są porównywane jako tekst, a wartość zachowuje typ (liczba zostaje liczbą).
    - Brak klucza daje `transformation_failed` z powodem `missing_dictionary_key`, niezaładowany słownik —
      `dictionary_unavailable`, a zły format parametru — `invalid_parameter`.
    - Zapis jest odrzucany, gdy wskazany kafelek nie istnieje, nie ma `items`, nie ma pola wartości wśród pól elementu,
      ma wymagane parametry lub wejścia, paginację albo sam używa `mapper`.
    - Edycja kafelka-słownika, która psuje odwołania `mapper`, daje 409 `dictionary_in_use`, a jego usunięcie —
      409 `tile_in_use`.
