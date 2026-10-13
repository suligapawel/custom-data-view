# MVP - Custom data view

## Cel

Aplikacja ma za zadanie łączyć się z różnymi zewnętrznymi źródłami danych (głównie REST API), w celu pobrania danych do
widoku użytkownika.
Pobieranie danych powinno być zdefiniowane a definicja będzie przetrzymywana w bazie danych aplikacji.
Najmniejsza definicja to transformacja poszczególnych propertiesów, większa to definicja kafelków, która będzie trzymała
czyste lub przetranfsormowane dane, a następnie agregacja kafelków do widoku użytkownika.

### Główne funkcjonalności

- Zdefiniowanie połączeń do zewnętrznych źródeł danych
- Zdefiniowanie sposobu przetwarzania danych z zewnętrznych źródeł
- Agregacja i pobieranie danych dla zdefiniowanych przez użytkowników kafelków
- Agregacja kafelków do widoku użytkownika
- Transformacja danych
- Zapisywanie konfiguracji kafelków oraz widoku użytkownika

### Definicje

- Kafelek - element widoku użytkownika, który prezentuje jedną lub wiele danych z zewnętrznych źródeł
- Widok użytkownika - cały widok który pobiera gotowe kafelki z danymi

### Pomijalne funkcjonalności

- Obsługa użytkowników - w MVP dajemy możliwość zapisu konfiguracji per GUID
- Uwierzytelnianie użytkowników
