- ID: TEMP-038  
  Tytuł: Usunięcie widoków na rzecz kafelków złożonych  
  Opis: Jako użytkownik chcę budować układ danych jednym pojęciem — kafelkiem złożonym — aby nie utrzymywać osobnych
  definicji widoków z własnymi wiązaniami.  
  Kryteria akceptacji:
    - Moduł nie udostępnia już zasobu widoków (`/views`); żądania do niego zwracają 404.
    - Rolę widoku pełni kafelek złożony: korzeń drzewa z dziećmi, formularzami i wiązaniami (TEMP-036, TEMP-037).
    - Migracja usuwa tabele `views` i `view_tiles` razem z danymi; jej uruchomienie wymaga zgody dewelopera.
    - Blokady usunięcia i edycji kafelka uwzględniają tylko rodziców (dzieci i wiązania w kafelkach).
