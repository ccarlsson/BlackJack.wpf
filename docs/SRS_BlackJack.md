# Software Requirement Specification (SRS)

## Black Jack – WPF-applikation (undervisningsprojekt)

> **Målgrupp:** Gymnasieelever i programmering samt lärare/handledare.  
> **Teknik:** C# (målversion: 14), .NET (målversion: 10), **WPF-applikation** (grafiskt gränssnitt).  
> **Kort beskrivning:** En spelare möter datorn (dealern) i Black Jack med stöd för flera kortlekar, dubbla och splitta.

---

## 1. Introduktion

### 1.1 Syfte

Syftet med detta dokument är att specificera samtliga krav för utvecklingen av en konsolbaserad Black Jack-applikation avsedd för undervisning i objektorienterad programmering. Dokumentet ska vägleda elever i både **kravförståelse** och **designprinciper** (de fyra OOP-pelarna), samt ge en tydlig grund för implementation och testning.

### 1.2 Målgrupp

- **Primär:** Gymnasieelever som läser programmering (t.ex. Programmering 1/2).
- **Sekundär:** Lärare/handledare som bedömer arbetet och vill förklara OOP-struktur, testfall och spelregler.
- **Teknisk nivå:** Grundläggande C# och .NET, ingen tidigare erfarenhet av avancerade designmönster krävs.

### 1.3 Översikt

Dokumentet beskriver:

- Systemets kontext, användare och begränsningar.
- Funktionella krav (spelflöde, regler, användarinteraktion, dealerlogik).
- Icke-funktionella krav (prestanda, användbarhet, tillförlitlighet, testbarhet).
- Designkrav kopplat till OOP-pelarna.
- Tekniska specifikationer och versionsmål.
- Bilagor med klassdiagram och pseudokod.

---

## 2. Allmän beskrivning

### 2.1 Produktperspektiv

- **Typ:** Fristående WPF-applikation (grafiskt gränssnitt).
- **Miljö:** Lokalt på elevens dator (Windows/macOS/Linux) med .NET SDK.
- **Arkitektur:** Objektorienterad kärna (spelmodell) separerad från konsol-IO för att underlätta testning.
- **Data:** Ingen persistent lagring krävs (kan utökas med sparad bankrulle).

### 2.2 Användarprofil

- **Elev som spelare:** Interagerar via tangentbord (textmenyer/kommandon).
- **Lärare:** Kan ändra parametrar (antal kortlekar, min-/maxinsats, startkapital, testläge med seedad slump).

### 2.3 Begränsningar

- **Teknikversioner:** Projektet **målsätts** till **C# 14** och **.NET 10**.  
  *Anm.: Om skolmiljön saknar dessa versioner ska projektet kompileras mot närmaste tillgängliga .NET/C#-version utan att ändra beteende eller regler.*
- **UI:** Grafiskt gränssnitt med WPF (ingen konsolversion).
- **Bibliotek:** Endast standardbibliotek (.NET); inga tredjepartsberoenden.
- **Tid & scope:** För undervisning – försiktigt avgränsad funktionalitet; **försäkring** (insurance) ingår **inte** i baskraven.

---

## 3. Funktionella krav

### 3.1 Spelregler (översikt)

- Oklädda kort (2–10) har sitt numeriska värde.
- Klädda kort (knekt, dam, kung) = 10.
- Ess = 1 eller 11 (det värde som ger bästa icke-bust total).
- **Black Jack** = 21 med **endast två kort** (ess + 10/knekt/dam/kung).
- Dealern **stannar alltid** på **17 eller högre** (inkl. “soft 17” om inte annat anges).
- Stöd för **flera kortlekar** (t.ex. 1–8 st i en “shoe”).
- **Dubbla** (double down) och **splitta** (split) stöds.
- Standardutbetalningar: **Black Jack betalar 3:2**, vinst 1:1, push = insatsen tillbaka.

### 3.2 Användarinteraktion

- Huvudmeny med alternativ: Ny runda, Inställningar, Hjälp, Avsluta.
- Insats inom [min, max] och <= saldo.
- Initial giv: två kort till spelaren, två till dealern (ett dolt).
- Spelarens tur: Hit, Stand, Double Down, Split (enligt regler).
- Dealer spelar enligt stanneregel (17 eller högre).
- Resultat visas per hand och saldo uppdateras.

### 3.3 Datorns beteende

- Hantering av flera kortlekar och blandning.
- Kortgivning från shoe.
- Värdering av händer (ess som 1 eller 11).
- Dealerlogik: stannar på 17 eller högre (soft 17 valbart).

---

## 4. Icke-funktionella krav

### 4.1 Prestanda

- Start av applikationen ska ske inom **2 sekunder**.
- En normalrunda ska avslutas inom **200 ms** beräkningstid.
- Shuffle av upp till **8 lekar** ska ske inom **300 ms**.

### 4.2 Användbarhet

- Alla texter på svenska med tydliga instruktioner.
- Kommandon accepterar både bokstav och hela ordet.
- Färger med god kontrast; möjlighet att stänga av färg.
- Felinmatning hanteras med tydlig återkoppling.

### 4.3 Tillförlitlighet

- Hantera ogiltig input utan att krascha.
- Testläge med seedad slump för reproducerbarhet.
- Enhetstester för centrala funktioner.
- Vid undantag: visa vänligt felmeddelande och avsluta kontrollerat.

### 4.4 Testbarhet

- Systemet ska vara **modulärt uppbyggt** så att centrala komponenter (t.ex. `Hand`, `Card`, `Dealer`, `Game`) kan testas **utan beroende av användarinmatning eller konsolutskrift**.
- Funktioner för handvärdering, Black Jack-detektion, dealerlogik och resultatberäkning ska ha **enhetstester**.
- Slumpmässiga moment (kortdragning, blandning) ska kunna **styras via seed** eller **mockade gränssnitt** (`IRandomProvider`) för att möjliggöra **reproducerbara testfall**.
- Projektet ska innehålla ett **testprojekt** (t.ex. med xUnit eller MSTest) som kan köras via `dotnet test`.
- Testfall ska inkludera både **normala scenarier** och **gränsfall** (t.ex. esshantering, bust, push, Black Jack).

---

## 5. Designkrav (OOP, Clean Architecture, MVVM)

- Inkapsling: privata fält, publika properties/metoder.
- Arv: `Player` som basklass, `HumanPlayer` och `Dealer` som subklasser.
- Polymorfism: virtuella metoder för beslut.
- Abstraktion: gränssnitt för slump, blandning, utbetalning.
- **Clean Architecture:** Applikationen ska vara uppdelad i tydliga lager (t.ex. Presentation, Application, Domain, Infrastructure) med beroenden riktade inåt enligt Clean Architecture-principer.
- **MVVM:** WPF-applikationen ska implementera MVVM-mönstret (Model-View-ViewModel) för att separera UI-logik från affärslogik och underlätta testning och underhåll.

---

## 6. Tekniska specifikationer

- Språk: C# 14
- Ramverk: .NET 10
- **WPF-applikation** (Windows Presentation Foundation) med MVVM-mönster
  - Applikationen ska följa Clean Architecture-principer
- Test: xUnit eller MSTest
- Plattformsstöd: Windows

---

## 7. Spelregler för Black Jack

- Kortvärden: 2–10 = numeriskt, J/Q/K = 10, Ess = 1 eller 11.
- Black Jack = 21 med två kort (ess + 10/J/Q/K).
- Dealer stannar på 17 eller högre (soft 17 valbart).
- Flera kortlekar (1–8).
- Double Down och Split enligt regler.
- Utbetalning: Black Jack 3:2, vinst 1:1, push = insats tillbaka.

---

## 8. Bilagor och exempel

- Klassdiagram (ASCII)
- Pseudokod för handvärdering och rundflöde
- Exempel på konsolinteraktion
- Rekommenderade testfall
- Spårbarhetsmatris
