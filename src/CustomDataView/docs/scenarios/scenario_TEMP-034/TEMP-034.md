- ID: TEMP-034  
  Tytuł: Wejścia danych i wartości testowe podglądu  
  Opis: Jako użytkownik chcę, aby kafelek mógł przyjąć gotowy JSON zamiast pobierać dane, a podgląd przyjmował
  wartości testowe wejść, aby sprawdzić kafelek osadzany w innym kafelku.  
  Kryteria akceptacji:
    - Źródło `input` deklaruje wejście danych; jego klucz jest nazwą wejścia w kontrakcie.
    - Żądanie podglądu kafelka przyjmuje `parameters` (wartości wejść-parametrów kontraktu) i `inputs` (wejścia
      danych); wiązanie w definicji wygrywa z wartością testową.
    - Brak wartości wejścia danych daje polom z tego źródła błąd `invalid_parameters`, a kafelkowi z `items` z tego
      źródła `itemsError`.
