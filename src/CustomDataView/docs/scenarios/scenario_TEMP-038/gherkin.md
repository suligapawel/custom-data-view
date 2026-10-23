## Feature

Widoki zastąpione kafelkami złożonymi

### Background

Given użytkownik konfiguruje kafelki dla widoków danych

---

### Role: Użytkownik konfigurujący widoki danych

#### Scenario #1

Zasób widoków nie istnieje

```
When  użytkownik pobiera listę widoków, zapisuje widok albo ogląda podgląd widoku
Then  odpowiedź ma status 404
```
