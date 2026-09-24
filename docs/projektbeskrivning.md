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
| B3 | Tärningskast | Tärningar slås **endast i OOC-kanalen**. Tärningar i RPG kan övervägas senare. |
| B4 | Inloggning | Fas 1: **vanligt konto med e-post och lösenord** (primärt). Google, Facebook och Discord kan läggas till senare. |
| B5 | Databas | **PostgreSQL**. Under utveckling körs den i Docker via `docker-compose.yml` i repots rot. |
| B6 | RPG-flikens form | RPG är en **chatt i stil med Discord**, inte en lista med trådar. Varje kampanj har **en RPG-kanal**. Om fler kanaler behövs senare är oklart; datamodellen tillåter det. |
| B7 | OOC-flikens form | OOC är en chatt som fungerar som RPG-chatten, men här kan man även **slå tärningar**. |
| B8 | Vad som visas i chatten | Inläggen från de **senaste 7 dagarna**, men **minst 20** och **högst cirka 100** inlägg. Äldre inlägg laddas automatiskt när man **scrollar uppåt**. Gäller både RPG och OOC. |
| B9 | Skicka inlägg | **OOC:** Enter skickar, Shift+Enter ger ny rad. **RPG:** Enter ger ny rad, Ctrl+Enter skickar. En skicka-knapp finns alltid (mobil). |
| B10 | Krönikans form | Krönikan läses **som en bok**: alla kapitel i följd, äldst först, **med sidindelning** (5 kapitel per sida som start). |
| B11 | Befintliga RPG-trådar | De trådar som skapades innan chattformatet är testdata och **tas bort** vid ombyggnaden. |
| B12 | Redigering av inlägg | Egna chattinlägg kan redigeras och markeras då som **"redigerad"**. Byggs efter chattombyggnaden. |

### 1.2 Arbetsförslag (ej slutligt beslutade)

| # | Fråga | Förslag |
|---|-------|---------|
| F1 | Läsbarhet för utomstående | Kampanjinnehåll är läsbart för alla inloggade som standard (enligt skissen). En kampanj kan markeras som privat. |
| F2 | Olästa inlägg | Enkel olästmarkering per kanal (skissen visar t.ex. "OOC (2)"). Chatten kan då öppnas vid första olästa inlägget med en markering "Nya inlägg". |
| F3 | NPC:er | GM kan skapa NPC-karaktärer och skriva som dem i RPG-chatten. |
| F4 | Karaktär och kampanj | En karaktär tillhör en kampanj. En spelare kan ha flera karaktärer i samma kampanj. |
| F5 | Textformat | Text lagras som Markdown, renderas server-side och saneras före visning. |
| F6 | Redigeringshistorik | Tidigare versioner av redigerade inlägg sparas (se B12). Historiken visas inte i gränssnittet än. |
| F7 | Ny ansökan efter avslag | Tillåten. Den avslagna ansökan finns kvar som historik. |
| F8 | Meddelande i ansökan | Valfritt, högst 1 000 tecken. |
| F9 | Full kampanj | Väntande ansökningar ligger kvar men kan inte godkännas förrän det finns plats. |
| F10 | Längd på RPG- och OOC-inlägg | Högst 10 000 tecken (Markdown-källtexten). |
| F11 | Ansökningar i spelrummet | Ansökningsformuläret och GM:ns ansökningslista ligger under fliken **Spelare**. |
| F12 | Skrivskydd | Stängda och arkiverade kampanjer är skrivskyddade för alla utom GM. |
| F13 | Tärningskommando | I OOC slår man med `/slå 2d6+3` (eller `/roll`) eller via en tärningsknapp bredvid skrivfältet. |
| F14 | Krönikans numrering | Kapitelnumret följer ordningen i boken. Flyttar GM ett kapitel numreras de övriga om. |
| F15 | Karaktärsbilder | Laddas upp (JPG, PNG eller WebP, högst 5 MB). Bilden beskärs till en kvadrat, skalas till 256×256, metadata tas bort och den sparas som WebP utanför wwwroot. |
| F16 | GM:s karaktärer | Allt GM skapar är NPC:er. Spelare skriver som egna karaktärer eller som sig själva; GM som NPC eller som berättare. |
| F17 | Ta bort karaktär | Går bara om karaktären inte har skrivit några inlägg, så att gamla inlägg behåller sin karaktär. |

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

- R-1: RPG och OOC är chattar med skrivfältet fast längst ner. Krönika,
  Karaktärer och Spelare är vanliga sidor.
- R-2: Icke-deltagare kan läsa alla flikar (se F1) men inte skriva i någon.
- R-3: Flikar med olästa inlägg visar antalet olästa (se F2).

### 3.6 Chatt (gemensamt för RPG och OOC)

- CH-1: Inläggen visas kronologiskt med det senaste längst ner, som i Discord.
- CH-2: Vid öppning visas inläggen från de senaste 7 dagarna, men minst 20 och
  högst cirka 100 (B8). Gränserna ska vara lätta att ändra.
- CH-3: När man scrollar mot toppen laddas nästa omgång äldre inlägg (20 st)
  automatiskt och läggs ovanför, med bibehållen läsposition.
- CH-4: Flera inlägg i följd från samma person (inom 10 minuter, samma dag)
  grupperas under ett namn.
- CH-5: Datumavdelare visas mellan dagar (t.ex. "Tisdag 23 september").
- CH-6: Nya inlägg visas direkt för alla som har chatten öppen. Den som är
  längst ner följer med; den som läser längre upp får knappen "Nya inlägg ↓".
- CH-7: Inlägg stödjer formaterad text (se 3.13).
- CH-8: GM:s inlägg är tydligt märkta.
- CH-9: Egna inlägg kan redigeras och markeras som "(redigerad)" (B12).
  Det går så länge man får skriva i kanalen. Tärningskast kan aldrig redigeras (T-5).

### 3.7 RPG-flik

- P-1: Varje kampanj har en RPG-kanal (B6).
- P-2: Spelare och GM skriver inlägg med formaterad text.
- P-3: Vid skrivande väljer användaren vilken av sina karaktärer (eller NPC:er
  för GM) inlägget skrivs som.
- P-4: Inlägget visar författare, karaktärens namn och bild samt tidsstämpel.
- P-5: Det ska vara tydligt om inlägget är skrivet som GM, som spelare eller som
  en specifik karaktär.
- P-6: Enter ger ny rad; Ctrl+Enter eller knappen skickar (B9).
- P-7: Tärningar kan **inte** slås i RPG-fliken (B3).

### 3.8 OOC-flik

- O-1: En chatt per kampanj för kommunikation utanför rollspelet (B7).
- O-2: Meddelanden visar användarnamn och tidsstämpel.
- O-3: Enter skickar; Shift+Enter ger ny rad (B9).
- O-4: Här kan deltagare slå tärningar (se 3.11).

### 3.9 Krönika

Krönikan är kampanjens berättelse i sammanfattad form och läses som en bok.
Exempel: *"Kapitel 1 – Den mörka skogen. Spelarna gick in i den mörka skogen
och stred mot fyra spindlar. Sedan fortsatte de till orchbyn Xrashh …"*

- C-1: Krönikan består av löpande numrerade kapitel (B1).
- C-2: Varje kapitel har nummer, titel, formaterad text (max 5 000 tecken),
  författare, skapelsedatum och senast ändrad.
- C-3: Alla kapitel visas i följd, äldst först, med sidindelning
  (5 kapitel per sida som start) (B10).
- C-4: En innehållsförteckning överst listar alla kapitel och länkar till rätt
  sida och kapitel.
- C-5: "Läs från början" leder till första sidan, "Senaste kapitlet" till det
  senaste, som är tydligt markerat.
- C-6: Varje sida och kapitel har en egen adress som kan delas, t.ex.
  `/chronicle?sida=2#kapitel-7`.
- C-7: GM kan skapa, redigera, ta bort och ändra ordningen på kapitel
  (flytta ett steg i taget med ↑/↓). Numreringen följer ordningen (F14).
  Borttagning kräver bekräftelse.
- C-8: Teckengränsen syns för användaren och valideras på servern.
- C-9 (senare): Kapitel kan länka till relevanta inlägg i RPG-chatten.

### 3.10 Karaktärer

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

### 3.11 Tärningskast

- T-1: Stöd för d4, d6, d8, d10, d12, d20 och d100.
- T-2: Syntax `NdX`, `NdX+M` och `NdX-M`, t.ex. `1d20`, `2d6+3`, `1d20-1`.
- T-3: Man slår genom att skriva `/slå 2d6+3` (eller `/roll`) i OOC-chatten,
  eller via en tärningsknapp där man väljer tärning, antal och modifierare (F13).
  Text efter notationen blir en valfri beskrivning, t.ex. `/slå 1d20+5 anfall`
  (högst 100 tecken).
- T-4: Kastet visas som ett eget meddelande i OOC-chatten med enskilda
  tärningar, modifierare, total, vem som slog och när. Kastet visar alltid
  namnet på den som slog. En naturlig 20 eller 1 på d20 markeras.
- T-5: Kasten genereras **på servern** med kryptografiskt säker slump och kan
  inte ändras eller tas bort i efterhand.
- T-6: Rimliga gränser, t.ex. max 50 tärningar per kast.

Exempel:

```text
🎲 Anna slog 2d6+3 → [4, 5] + 3 = 12
```

### 3.12 Spelare-flik

- M-1: Visar kampanjens GM och spelare samt deras karaktärer.
- M-2: Här ansöker man till kampanjen, och GM ser och hanterar väntande
  ansökningar (F11).
- M-3 (senare): Skicka privat meddelande till en spelare.

### 3.13 Formaterad text

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
| Skriva i RPG | Nej | Nej | Ja | Ja |
| Skriva i OOC och slå tärningar | Nej | Nej | Ja | Ja |
| Redigera egna chattinlägg | Nej | Nej | Ja | Ja |
| Skapa/redigera egen karaktär | Nej | Nej | Ja | Ja |
| Redigera andras karaktärer | Nej | Nej | Nej | Ja |
| Skapa/redigera krönika | Nej | Nej | Nej | Ja |
| Hantera ansökningar och spelare | Nej | Nej | Nej | Ja |
| Administrera kampanjen | Nej | Nej | Nej | Ja |

¹ Om kampanjen inte är privat (F1).

Arkiverade och stängda kampanjer är skrivskyddade för alla utom GM (F12).

Alla behörighetskontroller görs på servern.

---

## 5. MVP och faser

Status: ✅ klart · ⏳ delvis · (tomt) inte påbörjat.

### Fas 1 – Grund
1. ✅ Konto: registrering, inloggning, profil (e-post och lösenord).
2. ⏳ Kampanjer: skapa, lista och status visas. GM kan ännu inte redigera kampanjen.
3. ✅ Ansökningsflöde med meddelande och GM-godkännande.

### Fas 2 – Spelrummet
4. ✅ Spelrum med flikar.
5. ✅ RPG och OOC som chatt enligt B6–B9: en kanal av varje per kampanj,
   gruppering och datumavdelare, 7 dagar/min 20/max 100, automatisk laddning
   uppåt, "Nya inlägg ↓", Enter/Ctrl+Enter och liveuppdatering.
6. ✅ Tärningar i OOC: `/slå` och `/roll` med valfri beskrivning, tärningspanel,
   kast på servern och visning i chatten.
7. ✅ Krönika som bok: innehållsförteckning, 5 kapitel per sida, "Läs från början"
   och "Senaste kapitlet", länkbara kapitel, GM skriver, redigerar, flyttar och
   tar bort kapitel.
8. ✅ Karaktärer med bild och textdokument, NPC:er för GM, val av karaktär
   ("Skriv som") i RPG-chatten och karaktärerna under fliken Spelare.

### Fas 3 – Komplettering
9. ✅ Redigering av egna inlägg (B12): "(redigerad)", liveuppdatering och sparad historik.
10. Olästmarkeringar.
11. Kampanjredigering för GM (A-2 till A-4).

### Senare
Social inloggning, privata meddelanden, notiser/e-postnotiser, flera
RPG-kanaler, tärningar i RPG, privata tärningskast, reaktioner, svar på
specifika inlägg, bilder i inlägg, sökning, bokmärken, export, dolda scener,
mer avancerad tärningssyntax, PWA.

---

## 6. Datamodell (översikt)

```text
User ──< CampaignMembership >── Campaign
User ──< CampaignApplication >── Campaign
Campaign ──< Thread (kanal, typ: Rpg | Ooc) ──< Post
Campaign ──< Character (IsNpc) ──< Post (valfri karaktär)
Campaign ──< ChronicleChapter
Post ── DiceRoll? (tärningskast, endast OOC)
User ──< ReadMarker >── Thread
Post ──< PostRevision
```

| Entitet | Viktiga fält |
|---|---|
| User | Id, e-post, visningsnamn, profilbild |
| Campaign | Id, namn, beskrivning, GM, max spelare, status, privat |
| CampaignMembership | CampaignId, UserId, anslöt |
| CampaignApplication | CampaignId, UserId, meddelande, status, skickad, behandlad, behandlad av |
| Thread | Id, CampaignId, typ, titel, beskrivning, status, skapare, skapad |
| Post | Id, ThreadId, författare, CharacterId?, innehåll, skapad, redigerad |
| PostRevision | PostId, tidigare innehåll, tidpunkt |
| Character | Id, CampaignId, ägare, namn, bildnyckel, dokument, extern länk, regelsystem, IsNpc, skapad, ändrad |
| ChronicleChapter | Id, CampaignId, nummer, titel, innehåll, författare, skapad, ändrad |
| DiceRoll | Notation, beskrivning, antal, sidor, modifierare, resultat, total. Lagras som jsonb-kolumnen `Roll` på inlägget; användare och tidpunkt kommer från inlägget. |
| ReadMarker | UserId, ThreadId, LastReadPostId |

RPG- och OOC-chatten är var sin kanal i tabellen `Threads` (typ `Rpg` resp.
`Ooc`), en av varje per kampanj. De delar därför logik för inlägg, laddning,
liveuppdatering och olästmarkering. Fler RPG-kanaler kräver ingen ändring av
datamodellen.

---

## 7. Teknisk arkitektur

| Område | Val |
|---|---|
| Ramverk | .NET 10, Blazor Web App |
| Rendering | Interactive Server för chattarna. Statisk SSR för övriga sidor där det räcker. |
| Autentisering | ASP.NET Core Identity (e-post/lösenord), externa leverantörer senare |
| Databas | PostgreSQL via Entity Framework Core (Npgsql). Lokalt i Docker (`postgres:18`). |
| Formaterad text | Markdown, renderat med Markdig och sanerat med HtmlSanitizer |
| Bilder | SkiaSharp: beskärs, skalas om, EXIF rensas och kodas om till WebP. Lagras på disk (`App_Data/media`) och serveras via `/media/avatars/{nyckel}` för inloggade. Drift på Linux kräver paketet SkiaSharp.NativeAssets.Linux. |
| Realtid | Nya inlägg pushas till öppna sessioner via Blazor Servers anslutning (fungerar inom en serverinstans) |
| Tärningar | `RandomNumberGenerator` på servern |

### Projektstruktur

```text
Taleshaven.slnx
├── src/
│   ├── Taleshaven.Web             Blazor-UI, Identity-sidor, startpunkt
│   ├── Taleshaven.Core            Domänmodell, affärsregler, behörigheter, tärningsparser
│   └── Taleshaven.Infrastructure  EF Core DbContext, migreringar, Markdown-rendering, bildlagring
├── tests/
│   └── Taleshaven.Tests           Enhetstester för affärsregler, behörigheter och textsanering
└── docs/
    └── projektbeskrivning.md
```

Beroenden: `Web → Core, Infrastructure` och `Infrastructure → Core`.
`Core` har inga beroenden på andra projekt.

### Riktlinjer

- Behörighetsregler samlas i Core (`CampaignPermissions`) och testas mot
  tabellen i avsnitt 4.
- Teckengränser och andra regler valideras på servern, inte bara i UI.
- Chattarna laddar inlägg med keyset-paginering, så att inte alla inlägg hålls
  i minnet.

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
- **Redigering och borttagning:** inlägg markeras som redigerade och historiken
  sparas. Borttagning av innehåll görs som mjuk radering.

---

## 9. Öppna frågor

1. Behövs flera RPG-kanaler (t.ex. när gruppen delar på sig)?
2. Ska spelare kunna föreslå krönikekapitel, eller skriver bara GM?
3. Ska GM kunna dölja tärningskast (privata kast)?
4. Hur hanteras borttagning av ett användarkonto: anonymiseras inläggen?
5. Ska GM kunna exportera kampanjen?
6. Ska arkiverade kampanjer kunna återställas?
7. Ska den som ansöker kunna dra tillbaka sin ansökan?
