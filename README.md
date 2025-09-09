# Besöksregistreringssystem i Azure

Ett litet system där användaren fyller i **Namn + Ålder + E-post** i ett formulär. Datan skickas till en **Azure Function** som sparar i **Azure SQL Database**. Varje registrering loggas i **Application Insights**. Frontend hostas på **GitHub Pages**. 

## Arkitektur (4 delar)
- **Frontend:** GitHub Pages (HTML-formulär)
- **Backend:** Azure Functions (HTTP POST)
- **Databas:** Azure SQL Database (tabell `Visitors`)
- **Loggning:** Application Insights (körningar/händelser)

## Instruktioner (för test)
1. Öppna frontend: **https://lojick.github.io/individuella-molnbaserade-applikationer/**
2. Fyll i **Namn**, **Ålder**, **E-post** och klicka **Skicka**.
3. Förväntat resultat: **Grönt** meddelande vid lyckad registrering, **Rött** vid fel.
4. Verifiering:
   - Visas i videoinspelningen: Datan syns i SQL-tabellen `Visitors`,
     loggning i Application Insights och tiden sparas i UTC.

## API (kort info)
Formuläret skickar `POST` till `/api/HttpRegisterVisitor` som `application/x-www-form-urlencoded` med fälten `name`, `age`, `email`.
