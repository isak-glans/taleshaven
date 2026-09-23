# Taleshaven – Projektbeskrivning och användarkrav

Taleshaven är en webbaserad, systemoberoende plattform för rollspel (TRPG) i
**play-by-post-format**. Berättelsen och kommunikationen står i centrum;
regler och fullständiga rollformulär hanteras på externa sidor.

Dokumentet sammanfattar kraven från den ursprungliga kravspecifikationen och
skissen, tillsammans med de beslut som fattats hittills.

---

## 1. Beslut och förslag

### 1.1 Fattade beslut

| # | Fråga | Beslut |
|---|-------|--------|
| B1 | Krönikans struktur | **Ett krönikeinlägg = ett kapitel.** Kapitlen numreras löpande inom kampanjen. Max **5 000 tecken** per kapitel. |
| B2 | Ansökan till kampanj | Ansökan görs via ett formulär med **meddelandefält** till GM. Privata meddelanden (PM) ingår inte i MVP. |
| B3 | Tärningskast | Tärningar slås **endast i OOC-kanalen**. |
| B4 | Inloggning | Fas 1: **vanligt konto med e-post och lösenord** (primärt). Google, Facebook och Discord kan läggas till senare. |
| B5 | Databas | **PostgreSQL**. Under utveckling körs den i Docker via `docker-compose.yml` i repots rot. |

### 1.2 Arbetsförslag (ej slutligt beslutade)

| # | Fråga | Förslag |
|---|-------|---------|
| F1 | Läsbarhet för utomstående | Kampanjinnehåll är läsbart för alla inloggade som standard (enligt skissen). En kampanj kan markeras som privat. |
| F2 | Olästa inlägg | Enkel olästmarkering per tråd ingår i MVP (skissen visar t.ex. "OOC (2)"). |
| F3 | NPC:er | GM kan skapa NPC-karaktärer och skriva som dem i RPG-trådar. |
| F4 | Karaktär och kampanj | En karaktär tillhör en kampanj. En spelare kan ha flera karaktärer i samma kampanj. |
| F5 | Textformat | Text lagras som Markdown, renderas server-side och saneras före visning. |
| F6 | Redigering | Egna inlägg kan redigeras. Inlägget markeras som redigerat och historiken sparas. |

---

## 2. Användarroller

| Roll | Beskrivning |
|------|-------------|
| Besökare | Ej inloggad. Ser startsidan och registrering/inloggning. |
| Användare | Inloggad. Ser kampanjlistan, kan läsa kampanjer och ansöka. |
| Ansökande | Har en ansökan som väntar på beslut i en kampanj. |
| Spelare | Godkänd deltagare i en kampanj. |
| Spelledare (GM) | Har skapat kampanjen och administrerar den. |
| Administratör | (Senare) Plattformsövergripande moderering. |

Roller som spelare, ansökande och GM gäller **per kampanj**. Samma användare
kan vara GM i en kampanj och spelare i en annan.

---

## 3. Användarkrav

### 3.1 Konto och autentisering

- K-1: En besökare kan registrera ett konto med e-post, visningsnamn och lösenord.
- K-2: En användare kan logga in och ut.
- K-3: En användare kan återställa sitt lösenord via e-post.
- K-4: En användare har en profil med visningsnamn och eventuell profilbild.
- K-5 (senare): Extern inloggning via Google, Facebook och/eller Discord.

### 3.2 Kampanjlista

- L-1: Inloggade användare ser en lista över kampanjer.
- L-2: Varje kampanj visar namn, GM, kort beskrivning, antal spelare / max antal
  spelare och status.
- L-3: Kampanjer som tar emot ansökningar har knappen **Ansök**.
- L-4: Statusar: *Öppen för ansökningar*, *Pågående*, *Stängd*, *Arkiverad*.

Exempel från skissen:

```text
- CoS kampanj av Olof (2/4 spelare | Ansök)
- Phandelvers Pact kampanj av Petter (1 av 4 spelare | Ansök)
- Waterdeep Dragonheist kampanj (av Rickard | Arkiverad)
- Blå Tornet kampanj (av Rolf | Stängd)
```

### 3.3 Kampanjadministration (GM)

- A-1: En användare kan skapa en kampanj och blir då dess GM.
- A-2: GM kan redigera namn, beskrivning, max antal spelare och status.
- A-3: GM kan öppna, stänga och arkivera kampanjen. Radering kräver bekräftelse.
- A-4: GM kan ta bort spelare från kampanjen.

### 3.4 Ansökan

- S-1: En användare kan ansöka till en kampanj som är öppen för ansökningar.
- S-2: Ansökan innehåller ett meddelandefält till GM.
- S-3: GM ser inkomna ansökningar och kan godkänna eller avslå dem.
- S-4: Användaren kan se status för sin ansökan (väntande, godkänd, avslagen).
- S-5: Systemet sparar när ansökan skickades och när den behandlades.
- S-6: En användare kan inte ansöka flera gånger samtidigt till samma kampanj.
- S-7: En godkänd ansökan kan inte överskrida kampanjens max antal spelare.

### 3.5 Spelrummet

Varje kampanj har ett spelrum med flikarna:

```text
RPG | OOC (2) | Krönika | Karaktärer | Spelare
```

- R-1: Flikarna visar innehåll ovanför och ett inmatningsfält nedanför. Fältet
  anpassas efter vilken flik som är aktiv.
- R-2: Icke-deltagare kan läsa alla flikar (se F1) men inte skriva i någon.
- R-3: Flikar med olästa inlägg visar antalet olästa (se F2).

### 3.6 RPG-flik

- P-1: En kampanj kan ha flera RPG-trådar (scener, platser, kapitel).
- P-2: GM skapar, redigerar, låser och arkiverar trådar.
- P-3: En tråd har titel, kort beskrivning, skapare, skapelsedatum och status
  (öppen/låst).
- P-4: Spelare och GM skriver inlägg med formaterad text.
- P-5: Vid skrivande väljer användaren vilken av sina karaktärer (eller NPC:er
  för GM) inlägget skrivs som.
- P-6: Inlägget visar författare, karaktärens namn och bild samt tidsstämpel.
- P-7: Det ska vara tydligt om inlägget är skrivet som GM, som spelare eller som
  en specifik karaktär.
- P-8: Tråden visar de **20 senaste** inläggen. "Ladda in fler" hämtar äldre
  inlägg och lägger dem ovanför, med bibehållen läsposition. Antalet ska vara
  konfigurerbart.
- P-9: Tärningar kan **inte** slås i RPG-fliken (B3).

### 3.7 OOC-flik

- O-1: En kronologisk chatt per kampanj för kommunikation utanför rollspelet.
- O-2: Meddelanden visar användarnamn och tidsstämpel.
- O-3: Samma paginering som RPG-trådar (20 senaste + "Ladda in fler").
- O-4: Här kan deltagare slå tärningar (se 3.10).

### 3.8 Krönika

- C-1: Krönikan består av löpande numrerade kapitel (B1).
- C-2: Varje kapitel har nummer, titel, formaterad text (max 5 000 tecken),
  författare, skapelsedatum och senast ändrad.
- C-3: GM kan skapa, redigera, ta bort och ändra ordningen på kapitel.
- C-4: Senaste kapitlet är tydligt markerat. Det går att hoppa till ett visst
  kapitelnummer.
- C-5: Teckengränsen syns för användaren och valideras på servern.
- C-6 (senare): Kapitel kan länka till relevanta RPG-trådar eller inlägg.

### 3.9 Karaktärer

- H-1: En spelare kan skapa en eller flera karaktärer i en kampanj där hen
  deltar (F4).
- H-2: En karaktär har namn, bild och ett formaterat textdokument på max
  **10 000 tecken** (konfigurerbart).
- H-3: Dokumentet används för beskrivning, aktuell HP, resurser, utrustning,
  tillstånd och anteckningar.
- H-4: En karaktär kan ha en extern länk till ett fullständigt rollformulär samt
  uppgift om regelsystem.
- H-5: Ägaren kan redigera sin karaktär. GM kan redigera alla karaktärer i
  kampanjen.
- H-6: GM kan skapa NPC:er (F3).

### 3.10 Tärningskast

- T-1: Stöd för d4, d6, d8, d10, d12, d20 och d100.
- T-2: Syntax `NdX`, `NdX+M` och `NdX-M`, t.ex. `1d20`, `2d6+3`, `1d20-1`.
- T-3: Användaren kan välja tärning, antal och modifierare via knappar eller
  skriva notationen.
- T-4: Resultatet visar enskilda tärningar, modifierare, total, vem som slog
  och när.
- T-5: Kasten genereras **på servern** med kryptografiskt säker slump och kan
  inte ändras eller tas bort i efterhand.
- T-6: Rimliga gränser, t.ex. max 50 tärningar per kast.

Exempel:

```text
Olof slog 2d6+3
Tärningar: 4, 5   Modifierare: +3   Totalt: 12
```

### 3.11 Spelare-flik

- M-1: Visar kampanjens GM och spelare samt deras karaktärer.
- M-2: GM ser här även väntande ansökningar.
- M-3 (senare): Skicka privat meddelande till en spelare.

### 3.12 Formaterad text

- X-1: Gemensam editor för RPG, OOC, krönika, karaktärer och kampanjbeskrivning.
- X-2: Stöd för fetstil, kursiv, rubriker, punktlistor, numrerade listor, länkar
  och citatblock.
- X-3: Teckenräknare visas där en maxgräns finns.
- X-4: All användartext saneras innan den visas (skydd mot XSS).
- X-5: Editorn fungerar på dator och mobil.
- X-6: Utkast sparas automatiskt så att text inte går förlorad om anslutningen
  bryts.

---

## 4. Behörigheter

| Funktion | Användare (ej deltagare) | Ansökande | Spelare | GM |
|---|:-:|:-:|:-:|:-:|
| Se kampanjlistan | Ja | Ja | Ja | Ja |
| Ansöka till kampanj | Ja | – | – | – |
| Läsa RPG, OOC, krönika, karaktärer | Ja¹ | Ja¹ | Ja | Ja |
| Skriva i RPG | Nej | Nej | Ja² | Ja |
| Skriva i OOC och slå tärningar | Nej | Nej | Ja | Ja |
| Skapa/redigera egen karaktär | Nej | Nej | Ja | Ja |
| Redigera andras karaktärer | Nej | Nej | Nej | Ja |
| Skapa/redigera krönika | Nej | Nej | Nej | Ja |
| Hantera trådar | Nej | Nej | Nej | Ja |
| Hantera ansökningar och spelare | Nej | Nej | Nej | Ja |
| Administrera kampanjen | Nej | Nej | Nej | Ja |

¹ Om kampanjen inte är privat (F1).
² Om tråden inte är låst.

Arkiverade och stängda kampanjer är skrivskyddade för alla utom GM.

Alla behörighetskontroller görs på servern.

---

## 5. MVP och faser

### Fas 1 – Grund
1. Konto: registrering, inloggning, profil (e-post och lösenord).
2. Kampanjer: skapa, redigera, lista, status.
3. Ansökningsflöde med meddelande och GM-godkännande.

### Fas 2 – Spelrummet
4. Spelrum med flikar.
5. RPG-trådar och inlägg med formaterad text och paginering.
6. OOC-kanal.
7. Karaktärer med bild och textdokument, val av karaktär vid RPG-inlägg.

### Fas 3 – Komplettering
8. Tärningskast i OOC.
9. Krönika.
10. Olästmarkeringar.

### Senare
Social inloggning, privata meddelanden, notiser/e-postnotiser, privata
tärningskast, reaktioner, svar på specifika inlägg, bilder i inlägg, sökning,
bokmärken, export, dolda scener, mer avancerad tärningssyntax, PWA.

---

## 6. Datamodell (översikt)

```text
User ──< CampaignMembership >── Campaign
User ──< CampaignApplication >── Campaign
Campaign ──< Thread (typ: Rpg | Ooc) ──< Post
Campaign ──< Character (IsNpc) ──< Post (valfri karaktär)
Campaign ──< ChronicleChapter
Thread ──< DiceRoll (endast OOC)
User ──< ReadMarker >── Thread
Post ──< PostRevision
```

| Entitet | Viktiga fält |
|---|---|
| User | Id, e-post, visningsnamn, profilbild |
| Campaign | Id, namn, beskrivning, GM, max spelare, status, privat |
| CampaignMembership | CampaignId, UserId, roll (Gm/Player), anslöt |
| CampaignApplication | CampaignId, UserId, meddelande, status, skickad, behandlad, behandlad av |
| Thread | Id, CampaignId, typ, titel, beskrivning, status, skapare, skapad |
| Post | Id, ThreadId, författare, CharacterId?, innehåll, skapad, redigerad |
| PostRevision | PostId, tidigare innehåll, tidpunkt |
| Character | Id, CampaignId, ägare, namn, bild, dokument, extern länk, regelsystem, IsNpc |
| ChronicleChapter | Id, CampaignId, nummer, titel, innehåll, författare, skapad, ändrad |
| DiceRoll | Id, ThreadId, användare, notation, resultat, modifierare, total, tidpunkt |
| ReadMarker | UserId, ThreadId, LastReadPostId |

OOC modelleras som en tråd av typen `Ooc`. På så sätt delar RPG och OOC samma
logik för inlägg, paginering och olästmarkering.

---

## 7. Teknisk arkitektur

| Område | Val |
|---|---|
| Ramverk | .NET 10, Blazor Web App |
| Rendering | Interactive Server. Statisk SSR för publika sidor där det räcker. |
| Autentisering | ASP.NET Core Identity (e-post/lösenord), externa leverantörer senare |
| Databas | PostgreSQL via Entity Framework Core (Npgsql). Lokalt i Docker (`postgres:18`). |
| Formaterad text | Markdown, renderat med Markdig och sanerat med HtmlSanitizer |
| Bilder | Omskalas, EXIF rensas och bilden kodas om vid uppladdning. Lagras på disk eller i blob-lagring. |
| Realtid | Nya inlägg pushas till öppna sessioner via Blazor Servers anslutning |
| Tärningar | `RandomNumberGenerator` på servern |

### Projektstruktur

```text
Taleshaven.slnx
├── src/
│   ├── Taleshaven.Web             Blazor-UI, Identity-sidor, startpunkt
│   ├── Taleshaven.Core            Domänmodell, affärsregler, behörigheter, tärningsparser
│   └── Taleshaven.Infrastructure  EF Core DbContext, migreringar, bildlagring
├── tests/
│   └── Taleshaven.Tests           Enhetstester för affärsregler och behörigheter
└── docs/
    └── projektbeskrivning.md
```

Beroenden: `Web → Core, Infrastructure` och `Infrastructure → Core`.
`Core` har inga beroenden på andra projekt.

### Riktlinjer

- Behörighetskontroller samlas i Core (t.ex. `ICampaignAuthorizer`) och testas
  mot tabellen i avsnitt 4.
- Teckengränser och andra regler valideras på servern, inte bara i UI.
- Långa trådar pagineras med keyset-paginering, så att inte alla inlägg hålls i
  minnet.

---

## 8. Säkerhet

- **Autentisering:** Identity med lösenordspolicy, kontolåsning och
  e-postbekräftelse.
- **Auktorisering:** varje handling kontrolleras på servern mot kampanjroll.
  UI-döljning räcker inte.
- **Formaterad text:** ingen rå HTML från användare, och all renderad HTML saneras.
- **Bilduppladdning:** kontroll av typ och storlek, omkodning och slumpade
  filnamn. Bilderna serveras aldrig som körbart innehåll.
- **Tärningskast:** genereras och sparas på servern och kan inte ändras eller
  tas bort.
- **Redigering och borttagning:** redigeringshistorik för inlägg. Borttagning av
  innehåll görs som mjuk radering.

---

## 9. Öppna frågor

1. Ska spelare kunna föreslå krönikekapitel, eller skriver bara GM?
2. Ska karaktärsbilder laddas upp eller anges som extern länk (eller både och)?
3. Ska GM kunna dölja tärningskast (privata kast)?
4. Hur hanteras borttagning av ett användarkonto: anonymiseras inläggen?
5. Ska GM kunna exportera kampanjen?
6. Ska arkiverade kampanjer kunna återställas?
7. Ska det gå att ansöka på nytt efter ett avslag?
