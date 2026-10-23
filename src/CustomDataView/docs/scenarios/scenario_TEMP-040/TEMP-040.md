- ID: TEMP-040  
  Tytuł: Filtr w ścieżce JSONPath  
  Opis: Jako użytkownik chcę wybierać z tablicy tylko elementy spełniające warunek (np. `[?@.example._type == 'x']`),
  aby pokazywać w polu albo w elementach kafelka wyłącznie pasujące obiekty.  
  Kryteria akceptacji:
    - Segment filtra `[?<warunek>]` albo `[?(<warunek>)]` porównuje operatorem `==` lub `!=` ścieżkę względną `@`
      (segmenty `.name`, `['name']`, `[n]`) z literałem: tekstem w apostrofach lub cudzysłowie, liczbą, `true`,
      `false` albo `null`.
    - Filtr działa na elementach tablicy i wartościach obiektu; ścieżka z filtrem zwraca płaską tablicę dopasowań
      jak ścieżka z wildcardem i może być ścieżką elementów (`items.path`).
    - Liczby porównywane są wartością, tekst dokładnie, a wartości różnych typów są różne. Brak wartości pod ścieżką
      `@` nie spełnia `==` i spełnia `!=`.
    - Warunek bez porównania, inny operator, niepoprawny literał albo wildcard lub filtr w ścieżce `@` odrzucają zapis
      kafelka z błędem ścieżki.
