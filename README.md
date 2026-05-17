# Hospital API — Tutorial 8

Aplikacja WebAPI w ASP.NET Core do zarządzania pacjentami i przypisaniami łóżek szpitalnych.

## Technologie

- .NET / ASP.NET Core Web API
- Entity Framework Core (podejście Database First)
- MS SQL Server (LocalDB)

## Struktura bazy danych

| Tabela | Opis |
|---|---|
| `Patients` | Pacjenci (PESEL, imię, nazwisko, wiek, płeć) |
| `Admissions` | Przyjęcia pacjentów na oddziały |
| `Wards` | Oddziały szpitalne |
| `Rooms` | Sale na oddziałach |
| `Beds` | Łóżka w salach |
| `BedTypes` | Typy łóżek (Standard, OIOM, Rehabilitacyjne, ...) |
| `BedAssignments` | Przypisania pacjentów do łóżek w określonym czasie |

## Uruchomienie

1. Utwórz bazę danych przy użyciu skryptu `create.sql`
2. Zaktualizuj connection string w `MasterContext.cs` (lub przenieś do `appsettings.json`)
3. Uruchom aplikację:

```bash
dotnet run
```

## Endpointy

### GET /api/patients

Zwraca listę wszystkich pacjentów wraz z ich przyjęciami i przypisaniami łóżek.

**Parametry query:**

| Parametr | Typ | Opis |
|---|---|---|
| `search` | `string` (opcjonalny) | Filtruje po imieniu lub nazwisku (LIKE) |

**Przykład:**
```
GET /api/patients
GET /api/patients?search=Kowalski
GET /api/patients?search=an
```

**Odpowiedź 200 OK:**
```json
[
  {
    "pesel": "90010112345",
    "firstName": "Jan",
    "lastName": "Kowalski",
    "age": 35,
    "sex": "Male",
    "admissions": [
      {
        "id": 1,
        "admissionDate": "2026-05-01T10:00:00",
        "dischargeDate": "2026-05-05T14:00:00",
        "ward": {
          "id": 1,
          "name": "Kardiologia",
          "description": "Oddział chorób serca i układu krążenia"
        }
      }
    ],
    "bedAssignments": [
      {
        "id": 1,
        "from": "2026-05-01T10:30:00",
        "to": "2026-05-05T13:00:00",
        "bed": {
          "id": 1,
          "bedType": {
            "id": 1,
            "name": "Standard",
            "description": "Łóżko standardowe"
          },
          "room": {
            "id": "A101",
            "hasTv": true,
            "ward": {
              "id": 1,
              "name": "Kardiologia",
              "description": "Oddział chorób serca i układu krążenia"
            }
          }
        }
      }
    ]
  }
]
```

---

### POST /api/patients/{pesel}/bedassignments

Przypisuje pacjentowi wolne łóżko danego typu na danym oddziale w podanym okresie czasu.

**Parametry route:**

| Parametr | Opis |
|---|---|
| `pesel` | Numer PESEL pacjenta |

**Body (JSON):**
```json
{
  "bedTypeId": 1,
  "wardId": 1,
  "from": "2026-07-01T09:00:00",
  "to": "2026-07-10T12:00:00"
}
```

| Pole | Typ | Opis |
|---|---|---|
| `bedTypeId` | `int` | ID typu łóżka |
| `wardId` | `int` | ID oddziału |
| `from` | `datetime` | Początek okresu |
| `to` | `datetime?` | Koniec okresu (opcjonalny, null = bezterminowo) |

**Odpowiedzi:**

| Kod | Opis |
|---|---|
| `201 Created` | Łóżko zostało pomyślnie przypisane |
| `404 Not Found` | Pacjent o podanym PESEL nie istnieje |
| `404 Not Found` | Brak wolnych łóżek o wskazanym typie na tym oddziale w podanym okresie |

---

## Logika wyszukiwania wolnego łóżka

Serwer szuka łóżka spełniającego jednocześnie:
- typ łóżka zgodny z `bedTypeId`
- łóżko należy do sali na oddziale `wardId`
- brak kolidujących przypisań w podanym przedziale czasowym

Dwa przedziały czasowe kolidują gdy:
```
istniejące.From < nowe.To  AND  istniejące.To > nowe.From

## Autor
Hanna Krechyk s32740
```

##
