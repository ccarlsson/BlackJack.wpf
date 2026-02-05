# BlackJackSRS

Ett undervisningsprojekt i C# och .NET 10 som implementerar Black Jack som WPF-applikation enligt Clean Architecture och MVVM.

## Projektstruktur

- **BlackJackSRS.Presentation** – WPF-klient, MVVM, CommunityToolkit.MVVM
- **BlackJackSRS.Application** – Applikationslogik och use cases
- **BlackJackSRS.Domain** – Domänmodeller och gränssnitt
- **BlackJackSRS.Infrastructure** – Dataåtkomst och externa tjänster
- **BlackJackSRS.Tests** – Enhetstester (xUnit)

## Teknik

- C# 14
- .NET 10
- WPF (MVVM, CommunityToolkit.MVVM)
- Clean Architecture
- xUnit för tester

## Bygga och köra

1. Klona repot och öppna i Visual Studio eller VS Code.
2. Kör `dotnet restore` och `dotnet build` i projektroten.
3. Starta WPF-applikationen via `dotnet run --project BlackJackSRS.Presentation`.
4. Kör tester med `dotnet test`.

## Licens

MIT (se LICENCE.md)

## Kontakt

Utvecklat för undervisning. Kontakta kursansvarig för frågor.
