- ID: TEMP-029  
  Tytuł: Podgląd widoku z danymi  
  Opis: Jako użytkownik chcę pobrać widok razem z danymi jego kafelków dla podanych parametrów, aby sprawdzić, jak
  wygląda cały skonfigurowany układ.  
  Kryteria akceptacji:
    - Podgląd zwraca nazwę widoku i kafelki w kolejności pozycji z identyfikatorem, nazwą, pozycją, klasą CSS i polami
      w formacie podglądu kafelka.
    - Parametry są przekazywane jako jedna płaska mapa metodą POST (w treści) albo GET (w adresie, powtórzony parametr
      tworzy listę) i trafiają do wszystkich kafelków.
    - Brak wymaganego lub niepoprawny parametr daje błąd tylko w kafelku, którego połączenia go wymagają; pozostałe
      kafelki zwracają dane, a odpowiedź ma status 200.
    - Błędy pól (niedostępne źródło, brak wartości pod ścieżką, błąd transformacji) są zwracane jak w podglądzie
      kafelka.
    - Widok bez kafelków zwraca pustą listę kafelków.
    - Podgląd nieistniejącego widoku zwraca 404.
    - Podgląd nie zmienia widoku ani kafelków, a jego wynik nie jest zapisywany.
