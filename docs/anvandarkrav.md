# Taleshaven – Användarkrav för en ny version i PHP

Det här dokumentet beskriver vad Taleshaven ska göra, så att sajten kan byggas om från grunden i PHP som en
traditionell webbsida: varje sida genereras på servern, formulär skickas med vanliga POST-anrop och sidan laddas om.
Ingen del *kräver* JavaScript. Där den nuvarande .NET-versionen är interaktiv (liveuppdatering, utkast, verktygsrad)
beskrivs här en enklare lösning med formulär, och JavaScript nämns bara som valfri förbättring.

Dokumentet är en sammanställning av det som gäller **i dag** (efter beslut B43 i `projektbeskrivning.md`). Historiken,
ersatta beslut och den tekniska arkitekturen för .NET-versionen står i `projektbeskrivning.md`. Hänvisningar som (B42)
pekar dit, för den som vill veta varför något är som det är.

Gränssnittet är på **engelska**; texterna i citattecken (t.ex. "Post as") är de som visas för användaren.

---

## 1. Vad Taleshaven är

Taleshaven är en plattform för rollspel (TRPG) i **play-by-post-format**: spelarna och spelledaren skriver
berättelsen tillsammans som inlägg i trådar, i sin egen takt. Plattformen är systemoberoende; regler och fullständiga
rollformulär hanteras på andra sidor. En kampanj består av:

```text
Campaign
├── Threads      trådar med inlägg; berättelsens kapitel, OOC-snack, husregler, sammanfattningar …
├── Characters   spelarnas karaktärer och spelledarens NPC:er
└── Players      spelledare, spelare och ansökningar
```

Det finns inga fasta trådtyper. En kampanj brukar ha en tråd per kapitel, en OOC-tråd och gärna en tråd som
sammanfattar berättelsen hittills ("Read me first"), men det bestämmer spelledaren.

---

## 2. Roller

### 2.1 Roller per kampanj

| Roll | Beskrivning |
|---|---|
| Besökare | Inte inloggad. Ser startsidan, registrering och inloggning. |
| Användare | Inloggad men inte med i kampanjen. Kan läsa kampanjen och ansöka. |
| Ansökande | Har en ansökan som väntar på svar. Läser som en användare. |
| Spelare | Godkänd deltagare. Skriver inlägg och skapar egna karaktärer. |
| Spelledare (GM) | Skapade kampanjen och administrerar den. Det finns en GM per kampanj. |

Samma person kan vara GM i en kampanj och spelare i en annan.

### 2.2 Roller för hela sajten

| Roll | Får |
|---|---|
| Manager | Sköta porträttbiblioteket. |
| Administratör | Allt en manager får, och dela ut och ta bort roller. |

De första administratörerna anges med e-postadress i konfigurationen. De räknas alltid som administratörer när
adressen är bekräftad och kan inte tas bort på sajten, bara i konfigurationen. Därefter delar en administratör ut
roller på en egen sida. En administratör kan inte ta bort sin egen administratörsroll.

---

## 3. Behörigheter

Alla kontroller görs **på servern** vid varje anrop. Att dölja en knapp räcker aldrig.

| Funktion | Användare | Ansökande | Spelare | GM |
|---|:-:|:-:|:-:|:-:|
| Se kampanjlistan och läsa kampanjer | Ja | Ja | Ja | Ja |
| Ansöka till en kampanj | Ja¹ | – | – | – |
| Dra tillbaka sin ansökan | – | Ja | – | – |
| Skriva inlägg i aktiva trådar | Nej | Nej | Ja² | Ja |
| Skriva inlägg i avslutade trådar | Nej | Nej | Nej | Ja |
| Redigera egna inlägg | Nej | Nej | Ja² | Ja |
| Ta bort egna inlägg | Nej | Nej | Ja²,³ | Ja |
| Redigera och ta bort andras inlägg | Nej | Nej | Nej | Ja |
| Skapa, döpa om, ordna, avsluta och öppna trådar | Nej | Nej | Nej | Ja |
| Skapa och redigera egna karaktärer | Nej | Nej | Ja | – |
| Skapa och redigera NPC:er, redigera alla karaktärer | Nej | Nej | Nej | Ja |
| Se dolda NPC:er och GM-anteckningar | Nej | Nej | Nej | Ja |
| Hantera ansökningar och spelare | Nej | Nej | Nej | Ja |
| Ändra kampanjens inställningar och status, radera kampanjen | Nej | Nej | Nej | Ja |

¹ Bara när kampanjen har status *Open for applications*.
² Bara när tråden är *Active* och kampanjen är *Open for applications* eller *Ongoing*. Stängda och arkiverade
  kampanjer är skrivskyddade för alla utom GM.
³ Inlägg med tärningsslag kan bara GM ta bort, så att ingen kan ta bort ett dåligt slag.

Den som skriver kan välja att skriva **som** en karaktär: en spelare som sina egna karaktärer, GM som sina NPC:er.
En arkiverad NPC kan inte väljas.

---

## 4. Konto

- **KO-1 Registrering:** e-post, visningsnamn (2–50 tecken) och lösenord (minst 8 tecken). E-postadressen måste vara
  unik och **bekräftas** via en länk innan man kan logga in.
- **KO-2 Inloggning och utloggning,** med "Remember me" och tillfällig låsning efter flera felaktiga försök.
- **KO-3 Glömt lösenord:** en återställningslänk skickas med e-post.
- **KO-4 Mitt konto:** byta visningsnamn, e-postadress (med ny bekräftelse) och lösenord.
- **KO-5 Ta bort kontot** (med lösenordet som bekräftelse):
  - Den som är GM för någon kampanj måste först radera kampanjen.
  - Kontot blir en anonym "gravsten" med namnet **"Deleted user"**: e-post, lösenord och inloggningsmöjlighet tas bort.
  - Inlägg och karaktärer ligger kvar och visas som skrivna av "Deleted user".
  - Medlemskap, ansökningar, roller och läspositioner raderas.
- **KO-6 Avatar:** användare har ingen egen bild. De visas med **initialer** i en färgad cirkel; färgen räknas fram
  från användarens id så att den alltid är densamma.

---

## 5. Kampanjer

### 5.1 Kampanjlistan (startsidan för inloggade)

- **KL-1:** Listan visar en **kompakt rad per kampanj**: namn, status, taggar, GM och antal spelare / max antal
  spelare. **Beskrivningen visas inte** i listan; den finns i kampanjen under "About the campaign".
- **KL-2:** Kampanjer man deltar i visar antalet olästa inlägg ("3 new"; över 99 visas "99+").
- **KL-3:** Kampanjer som tar emot ansökningar har knappen **Apply**; den som redan ansökt ser "Application sent",
  och en full kampanj visar "Full". Kampanjer man är GM i märks **GM**.
- **KL-5 Grupper:** kampanjer man är GM eller spelare i ligger överst under **My campaigns**. Övriga ligger under
  **Other campaigns** (eller **All campaigns** om man inte är med i någon), **25 per sida** med sidnavigeringen i
  avsnitt 8.2 (utan *Latest*). Inom grupperna ordnas kampanjerna efter status och sedan nyast först.
- **KL-6 Filter:** en sökruta överst ("Filter by name or tag") filtrerar på namn och taggar. Varje sökord ska matcha
  början av ett ord i namnet eller början av en tagg (`#` ignoreras, stora och små bokstäver spelar ingen roll), och
  kampanjen visas om **alla** ord matchar. Exempel: `lan dnd` hittar *Lanterns of Greywater* med `#dnd5e`, men
  `water` gör det inte. Filtret är ett GET-formulär (`/?q=horror&page=2`), och sidindelningen behåller filtret.
  Inga träffar: "No campaigns match …" med en länk till alla kampanjer.
- **KL-7 Taggar:** varje tagg i listan och i kampanjens huvud är en länk som filtrerar listan på taggen (`/?q=horror`).
- **KL-4:** Statusar:

| Status | Betydelse |
|---|---|
| Open for applications | Spelar och tar emot ansökningar. |
| Ongoing | Spelar, tar inte emot ansökningar. |
| Closed | Pausad eller avslutad. Skrivskyddad för alla utom GM. |
| Archived | Färdig och sparad som historik. Skrivskyddad för alla utom GM. |

### 5.2 Skapa och administrera

- **KA-1 Skapa kampanj:** namn (högst 100 tecken), beskrivning (Markdown, högst 2 000 tecken), max antal spelare
  (1–20), taggar och standardtärning. Den som skapar blir GM. En ny kampanj har inga trådar.
- **KA-2 Inställningar (GM):** ändra namn, beskrivning, max antal spelare, status, taggar och standardtärning.
  Status byts fritt mellan de fyra värdena.
- **KA-4 Taggar:** upp till **10 taggar**, skrivna som `#dnd5e #horror` eller `dnd5e, horror`. Samma regler som för
  porträttens taggar (PB-4): små bokstäver, utan `#`, högst 30 tecken, bara bokstäver, siffror och bindestreck;
  dubbletter tas bort. Taggar är valfria. Taggar som används på andra kampanjer visas som förslag under fältet.
- **KA-5 Standardtärning:** formeln som fylls i när någon lägger till ett tärningsslag i ett inlägg (TA-4), t.ex.
  `1d20` för D&D, `1d100` för Call of Cthulhu eller `2d6` för PbtA-spel. Den väljs ur förslag (en `<datalist>` med
  `1d20 1d100 2d6 3d6 1d6 1d8 1d10 1d12 1d4`) eller skrivs fritt, kontrolleras som andra formler (TA-1) och sparas
  normaliserad (`d100` → `1d100`). Tomt fält ger `1d20`, som också är standard för nya kampanjer.
- **KA-3 Radera kampanj (GM):** GM måste skriva kampanjens namn exakt för att bekräfta. Allt innehåll raderas:
  trådar, inlägg, karaktärer, ansökningar, medlemskap och läspositioner. Porträtten ligger kvar i biblioteket.
  Sidan rekommenderar arkivering för kampanjer som bara är avslutade.

### 5.3 Kampanjens huvud (på alla kampanjens sidor)

- "← Campaigns", kampanjens namn och status, "GM: namn · 2/4 players" och kampanjens taggar som länkar (KL-7).
- "About the campaign": beskrivningen, hopfälld (ett `<details>`-element räcker).
- En knapp **Settings** för GM.
- Flikarna **Threads · Characters · Players**. Threads visar antalet olästa inlägg i hela kampanjen.

---

## 6. Ansökningar och spelare (fliken Players)

- **SP-1:** Fliken visar GM och spelarna, med varje spelares karaktärer.
- **SP-2 Ansöka:** en användare som inte deltar ansöker med ett valfritt meddelande till GM (högst 1 000 tecken),
  bara när kampanjen är *Open for applications*. Man kan inte ha två väntande ansökningar till samma kampanj.
- **SP-3 Status:** den som ansökt ser sin ansökans status: *Pending*, *Approved* eller *Rejected*.
- **SP-4 Dra tillbaka:** en väntande ansökan kan dras tillbaka, med ett bekräftelsesteg. Den sparas som historik med
  status *Withdrawn*, försvinner ur GM:s lista, och man kan ansöka igen.
- **SP-5 GM hanterar ansökningar:** listan över väntande ansökningar visar namn, meddelande och datum, med
  **Approve** och **Reject**. En ansökan kan inte godkännas om kampanjen är full; den ligger då kvar. Efter ett
  avslag får man ansöka igen; det gamla avslaget sparas som historik.
- **SP-6:** När en ansökan behandlas sparas när och av vem.
- **SP-7 Ny spelare:** när en spelare godkänns räknas allt som redan skrivits som **läst**, så att den nya spelaren
  inte får hundratals olästa inlägg.
- **SP-8 Ta bort spelare (GM),** med bekräftelse. Spelarens inlägg och karaktärer ligger kvar. Spelarens
  läspositioner raderas. Personen kan ansöka igen och börjar då om enligt SP-7.

---

## 7. Trådar (fliken Threads)

### 7.1 En tråd

En tråd har **titel** (högst 100 tecken), **status** (*Active* eller *Completed*), en **position** som styr
ordningen, vem som skapade den och när. *Completed* betyder avslutad men kvar som en del av kampanjens historia.

- **TR-1:** Bara GM skapar trådar, med en knapp **+ New thread** och ett formulär med bara en titel.
- **TR-2:** GM kan döpa om en tråd ("Rename thread").
- **TR-3:** GM avslutar en tråd ("Complete thread") efter en bekräftelse: *"Complete this thread? Players can no
  longer post in it."* En avslutad tråd är skrivskyddad för spelarna; GM kan fortfarande skriva i den.
- **TR-4:** GM kan öppna en avslutad tråd igen ("Reopen").
- **TR-5:** Trådar raderas inte en och en; de försvinner bara när kampanjen raderas.

### 7.2 Trådlistan

- **TL-1 Filter:** överst finns filtret **Active · Completed**, med antal trådar och antal olästa inlägg per filter
  ("Active 4 · 6 new"). *Active* är förvalt. Adress t.ex. `/campaigns/12?status=completed&page=2`.
- **TL-2 Ordning:** aktiva trådar i GM:s ordning (lägst position först); avslutade med **senaste först**
  (högst position först).
- **TL-3 Sidor:** 20 trådar per sida, med samma sidnavigering som i trådarna (avsnitt 8.2) men utan *Latest*.
- **TL-4 Rad:** varje tråd är en kompakt textrad utan bilder:
  - en röd prick och fet titel om det finns olästa inlägg, samt en länk "3 new posts";
  - märket *Completed* på avslutade trådar;
  - "14 posts · 3 participants";
  - "Latest: [avatar] namn · 2 hours ago" (den som skrev senast; skrivet som karaktär visas karaktärens namn och porträtt).
- **TL-5 Flytta (GM):** knapparna ↑ och ↓ byter plats med grannen bland trådarna med samma status. Efter flytten
  står man kvar på samma filter och sida. Knapparna är avstängda för första och sista tråden i filtret.
- **TL-6 Tom lista:** "No threads yet." (GM får tipset att skapa en tråd för första scenen och en för OOC).
  Tomt filter: "No active threads." med länk till de avslutade, eller "No completed threads yet."

```text
[ Active 4 · 6 new ]  [ Completed 3 ]                         + New thread
● Chapter 3 – The Bellfounder's House   [3 new posts]               ↑ ↓
  25 posts · 3 participants             Latest: (F) Freja · 6 hours ago
  OOC                                                               ↑ ↓
  25 posts · 3 participants             Latest: (G) Gunnar · 11 hours ago
```

### 7.3 Antal deltagare

Antalet deltagare i en tråd är antalet olika användare som har skrivit inlägg som inte är borttagna där. En dold NPC
räknas inte för sig (det är ju GM som skriver).

---

## 8. Trådsidan

### 8.1 Huvud

- "← Threads", titel och status (*Active* grön, *Completed* grå).
- GM har en **"…"-meny** längst till höger med *Complete thread* (aktiv tråd), *Rename thread* och *Reopen*
  (avslutad tråd). Bekräftelsen för att avsluta visas som en rad under rubriken, med *Yes, complete* och *Cancel*.
- "25 posts · 3 participants", samt länkarna **First unread** (om det finns olästa) och **Latest**.

Utan JavaScript kan menyn vara ett `<details>`-element, och bekräftelsen en egen sida eller samma sida med en
parameter (t.ex. `?confirm=complete`).

### 8.2 Inlägg och sidor

- **TS-1:** Inläggen visas **äldst först**, **20 per sida**. Ingen oändlig scroll.
- **TS-2 Sidnavigering** längst ner: *Previous* till vänster (avstängd på första sidan), sidnumren och *Next* i
  mitten, *Latest* till höger, och "Page 2 of 5" under. Sidnumren visar första och sista sidan och två sidor på
  var sida om den aktuella; en lucka på en enda sida visas som sidans nummer, längre luckor som "…".
  Exempel: `1 2 3 4 5 … 12` och `1 … 6 7 8 9 10 … 14`.
- **TS-3 Vilken sida öppnas:**
  1. `?post=N` – sidan där inlägg N står, med inlägget markerat och scrollat till (`#post-N`).
  2. `?page=last` – sista sidan.
  3. `?page=N` – sida N (begränsas till 1…antal sidor).
  4. Annars: sidan med **första olästa inlägget**, scrollad dit (`#first-unread`), och om allt är läst den sista sidan.

  I PHP görs scrollningen enklast med en omdirigering till rätt adress med ankare, t.ex. `?page=3#post-123`.
- **TS-4 Olästa:** före första olästa inlägget visas en avdelare "New posts".
- **TS-5 Fast länk:** varje inlägg har en adress (`?post=N`) som hittar rätt sida även när tråden växer.
- **TS-6 Tom tråd:** "No posts yet." (och "Write the first one!" för den som får skriva).

### 8.3 Ett inlägg

Inlägget visas som ett ljust kort:

```text
( 56px )  Ilse Marrow  Freja  · 3 days ago (edited)                         …
 porträtt  ↪ Replying to Brannoc Ashby
           Ilse slips behind a hanging bell to hide, then drops …
           ┌──────────────────────────────────────────────────────────┐
           │ 🎲 Stealth        1d20+7               [12] + 7 = 19      │
           │ 🎲 Rapier attack  1d20+5  Advantage    [4̶] [18] + 5 = 23  │
           └──────────────────────────────────────────────────────────┘
           ↩  ❝  ✎
```

- **IN-1 Porträtt** (rund, 56 px): karaktärens porträtt, annars initialer. Skrivet utan karaktär: användarens initialer.
- **IN-2 Namn:** karaktärens namn, och spelarens namn i grått bredvid. Utan karaktär: användarens namn.
  Märken: **NPC** för NPC:er, **GM** när GM skriver som berättare, **Hidden** (bara för GM) på dolda NPC:er.
- **IN-3 Länk till karaktären:** namn och porträtt på en karaktär eller NPC länkar till karaktärens sida, utom en
  dold NPC för spelarna. Inlägg utan karaktär länkas inte.
- **IN-4 Tid:** relativ tid ("just now", "5 minutes ago", "yesterday", "3 days ago", sedan datum) med exakt datum och
  klockslag vid hovring (`title`). Tiden är en länk till inläggets fasta adress. Tider visas på engelska men i
  svensk tid (Europe/Stockholm).
- **IN-5** "(edited)" om inlägget har redigerats, med tidpunkten vid hovring.
- **IN-6** "Replying to *namn*" med länk till inlägget det svarar på.
- **IN-7 Texten** renderas enligt avsnitt 11, med en maxbredd på ungefär 72 tecken per rad för läsbarhetens skull.
- **IN-8 Tärningsslagen** visas som en lista under texten (avsnitt 10).
- **IN-9 Åtgärder** i inläggets övre högra hörn: en liten ikon **Reply** (för den som får skriva) och en "…"-meny med
  **Quote** (för den som får skriva), **Edit** (om man får redigera), **Copy link** och **Delete** (om man får ta bort).
  Det finns ingen ikonrad under texten.
- **IN-10 Borttaget inlägg:** visas på sin plats som *"This post was deleted."* utan namn, text eller åtgärder.

### 8.4 Svara, citera, redigera och ta bort

- **SV-1 Reply:** skrivfältet får raden "↪ Replying to *namn*" med ett ✕ för att ångra. Inlägget sparar vilket
  inlägg det svarar på. Trådarna är platta, utan nästlade svar.
  I PHP: länken går till sista sidan med `?reply=N#composer`.
- **SV-2 Quote:** skrivfältet fylls med inlägget som citat:
  `> **Namn** wrote:` följt av texten med `> ` före varje rad. Tärningsslagen följer med som text
  (`> 🎲 Attack: 1d20+5 (advantage): [4, 18] + 5 = 23`) och blir inga nya slag. I PHP: `?quote=N#composer`.
- **SV-3 Edit:** inlägget byts mot ett skrivfält med texten. Bara texten kan ändras; tärningsslagen visas under
  fältet med texten *"The dice rolls can't be changed."* Ett inlägg med slag får redigeras till tom text.
  Den tidigare versionen sparas som historik (visas inte i gränssnittet). I PHP: `?edit=N#post-N`.
- **SV-4 Delete:** kräver bekräftelse ("Delete this post? Yes, delete / Cancel"). Borttagningen är **mjuk**:
  inlägget ligger kvar i databasen med tidpunkt och vem som tog bort det, men visas som borttaget (IN-10).
- **SV-5 Copy link:** kopierar inläggets fasta adress. Utan JavaScript räcker det att tiden är en länk (IN-4).

### 8.5 Skrivfältet

Skrivfältet (`#composer`) finns bara på **sista sidan**. Annars visas en av raderna:

- "This thread is completed." – spelare i en avslutad tråd.
- "You can read but not post here." – den som inte deltar, eller när kampanjen är stängd eller arkiverad.
- "Go to the latest page to post." – med länk, för den som står på en tidigare sida.

Skrivfältet innehåller, uppifrån och ner:

1. **"Post as"** (avsnitt 9.3), om man har något att välja. En spelare utan karaktärer ser i stället
   "Create a character under Characters to post as it."
2. **Textfältet** för Markdown, högst **5 000 tecken**, med en teckenräknare ("123 / 5000"). Under fältet en kort
   hjälptext om formateringen (fetstil, kursiv, listor, citat, länkar, `[ooc]…[/ooc]`, `[spoiler]…[/spoiler]`).
3. **Tärningsslagen** (avsnitt 10.2).
4. Knapparna **Preview** och **Post**.

- **SK-1 Preview:** visar texten som den kommer att se ut, och slagen som en streckad lista med *"rolls when posted"*.
  I PHP är det en egen knapp i formuläret som visar samma sida med förhandsgranskningen och allt ifyllt kvar.
- **SK-2 Post:** inlägget sparas, slagen slås (avsnitt 10) och man hamnar på inlägget (`?post=N`). Vid fel visas
  felmeddelandet och allt som var ifyllt ligger kvar.
- **SK-3 Tomt inlägg:** ett inlägg måste ha text eller minst ett tärningsslag ("Write something first.").

**Valfria förbättringar med JavaScript** (finns i .NET-versionen, men behövs inte):

- En verktygsrad som lägger in formateringen runt markerad text: **B**, *I*, Heading, • List, 1. List, Quote, Link,
  OOC och Spoiler. Spoiler-knappen lägger `[spoiler=Spoiler]` och `[/spoiler]` på egna rader.
- Ctrl+Enter publicerar. Enter ger alltid ny rad.
- Utkast av text och slag sparas i webbläsaren (localStorage) per tråd, så att inget går förlorat.
- Tangenterna ← → bläddrar mellan sidorna.

### 8.6 Nya inlägg medan man läser

.NET-versionen lägger till nya inlägg direkt för den som står på sista sidan. Det krävs **inte** i PHP-versionen:
sidan visar läget när den laddades. Vill man ha något kan en enkel länk "Check for new posts" eller en
automatisk omladdning användas, men aldrig medan man skriver.

---

## 9. Läsmarkeringar och "Post as"

### 9.1 Olästa inlägg

- **OL-1:** Varje deltagare (spelare och GM) har en **läsposition per tråd**: id för det senaste inlägget hen har sett.
- **OL-2:** Olästa är **andras** inlägg efter läspositionen som inte är borttagna. Egna inlägg räknas aldrig.
- **OL-3:** När en sida i en tråd visas flyttas läspositionen fram till det sista inlägget **på den sidan** (aldrig bakåt).
- **OL-4:** Antal olästa visas i trådlistan per tråd, per filter, på fliken Threads och i kampanjlistan.
  Över 99 visas "99+".
- **OL-5:** Den som inte deltar har inga läspositioner och ser inga olästmarkeringar.

### 9.2 Ny spelare

Se SP-7: när en spelare godkänns sätts läspositionen i varje tråd till det senaste inlägget.

### 9.3 "Post as"

- **PA-1 Val:** en spelare väljer mellan sina egna karaktärer och **"Myself, no character"**. GM väljer mellan
  **"GM (narrator)"** och sina NPC:er som inte är arkiverade. Dolda NPC:er är märkta *Hidden* i listan.
- **PA-2 Ordning:** de fem senast använda (efter senaste inlägg som karaktären) överst under rubriken
  "Recently used", sedan alla i bokstavsordning under "All". Med en vanlig `<select>` blir det två `<optgroup>`.
- **PA-3 Förvalt:** det man **senast skrev som i just den här tråden** (även "Myself" eller "GM (narrator)"), om
  valet fortfarande finns. Har man inte skrivit i tråden: en spelares första karaktär, eller "GM (narrator)" för GM.
- **PA-4:** Servern kontrollerar att man får skriva som den valda karaktären (avsnitt 3).

---

## 10. Tärningsslag

Tärningar skrivs **inte** i texten. Man lägger till slag som rader i skrivfältet, och de slås på servern när inlägget
postas. De visas som en lista under inlägget.

### 10.1 Formel

- **TA-1 Syntax:** `NdX`, `NdX+M` eller `NdX-M`, t.ex. `1d20`, `d20` (= `1d20`), `2d6+3`, `1d8-1`, `d100`.
  Mellanslag ignoreras och `D` fungerar som `d`. Reguljärt uttryck: `^(\d{1,3})?[dD](\d{1,3})([+-]\d{1,4})?$`.
- **TA-2 Tärningar:** d4, d6, d8, d10, d12, d20 och d100. Antal 1–50. Modifierare −999 till +999.
- **TA-3 Felmeddelanden:** "Invalid dice. Write e.g. 2d6+3, 1d20 or d100.", "The d7 die isn't supported. Use d4, d6,
  d8, d10, d12, d20, d100.", "The number of dice must be between 1 and 50." osv.

### 10.2 Lägga till slag

```text
🎲 [ 1d20+5 ] [ Athletics                    ] [ Advantage    ▾ ]  ✕
🎲 [ 2d6    ] [ Damage                       ] [ Normal       ▾ ]  ✕
[ 🎲 Add roll ]   The dice are rolled when you post.
```

- **TA-4 Rad:** formel, beskrivning (valfri, högst 100 tecken) och läge **Normal / Advantage / Disadvantage**.
  En ny rad får kampanjens **standardtärning** (KA-5) ifylld i formelfältet, med markören sist så att man bara skriver
  t.ex. `+5`.
- **TA-5 Fördel och nackdel** går bara att välja för en ensam d20 (`1d20` med eller utan modifierare). För andra
  formler är valet avstängt och läget Normal.
- **TA-6:** Högst **10 slag** per inlägg.
- **TA-7:** Tomma rader (varken formel eller beskrivning) ignoreras. En rad med beskrivning men utan formel ger
  felet "Enter the dice, e.g. 1d20+5."
- **TA-8 Fel:** en ogiltig formel markeras på raden med felmeddelandet, och inget postas förrän den är rättad
  ("Fix the dice rolls first.").

I PHP räcker ett formulär med raderna som fält i en array (`rolls[0][notation]`, `rolls[0][label]`,
`rolls[0][mode]`). **Add roll** är en submit-knapp som visar samma formulär med en rad till (texten ligger kvar);
**✕** tar bort raden på samma sätt. Formlerna kontrolleras när formuläret skickas.

### 10.3 Slå

- **TA-9:** Slagen görs **på servern** när inlägget postas, med kryptografiskt säker slump (i PHP `random_int()`).
- **TA-10 Normal:** N tärningar slås; summan är tärningarnas summa plus modifieraren.
- **TA-11 Fördel/nackdel:** två d20 slås; den högsta (fördel) eller lägsta (nackdel) räknas, plus modifieraren.
  Båda tärningarna sparas.
- **TA-12 Oföränderliga:** ett slag kan aldrig ändras, slås om eller tas bort genom redigering. Bara GM kan ta bort
  ett inlägg med slag.
- **TA-13 Sparas per slag:** formel, beskrivning, antal, sidor, modifierare, läge, de enskilda tärningarna och summan,
  i den ordning skribenten lade till dem.

### 10.4 Visa

- **TA-14:** Varje slag är en rad under inläggets text: 🎲, beskrivningen i fetstil, formeln i grått, ett märke
  *Advantage* eller *Disadvantage*, tärningarna som små rutor, modifieraren och **= summan**.
- **TA-15:** Vid fördel och nackdel är tärningen som inte räknas överstruken och nedtonad.
- **TA-16:** En naturlig 20 (grön) eller 1 (röd) markeras på en ensam d20, och på den tärning som räknas vid fördel
  eller nackdel.
- **TA-17:** Slaget som text, för citat och `title`: `Attack: 1d20+5 (advantage): [4, 18] + 5 = 23`.

---

## 11. Formaterad text

All text som användare skriver (inlägg, kampanjbeskrivning, karaktärsdokument, GM-anteckningar) är **Markdown**
som renderas på servern.

- **FT-1 Markdown:** stycken, radbrytningar (en enkel radbrytning blir `<br>`), **fetstil**, *kursiv*,
  ~~genomstruken~~, rubriker, punktlistor, numrerade listor, länkar, automatiska länkar, citatblock, kod och
  horisontell linje.
- **FT-2 Ingen rå HTML:** HTML som användaren skriver visas som text, och all renderad HTML **saneras** mot en
  vitlista innan den visas: `p br strong em del h1–h6 ul ol li blockquote a code pre hr` med attributen `href`
  (bara `http`, `https` och `mailto`) och `start`. Allt annat tas bort.
- **FT-3 OOC:** `[ooc]…[/ooc]` markerar text utanför rollspelet. Den visas grå och kursiv med en liten etikett
  **OOC**, så att den inte läses som in character.
  - Om taggarna omsluter hela stycken (från början av ett stycke till slutet av samma eller ett senare stycke)
    blir det ett eget block med grå bakgrund och kantlinje.
  - Annars, mitt i ett stycke, blir det markerad text i löpande text. Taggarna får inte spänna över stycken.
- **FT-4 Spoiler:** `[spoiler=Rubrik]…[/spoiler]` på egna rader blir en **hopfälld ruta** (`<details>` med
  `<summary>`) som är stängd tills läsaren klickar ("Map of the cellar – click to show"). Utan rubrik heter den
  "Spoiler". Rutan kan innehålla flera stycken och listor, och OOC fungerar inuti.
  `[spoiler]…[/spoiler]` mitt i en mening **döljer texten** (samma färg som bakgrunden) tills man klickar eller
  fokuserar på den; det går med enbart CSS (`:focus` på ett element med `tabindex="0"`).
  En spoiler är bara hopfälld, **inte hemlig**: texten skickas till alla som får läsa inlägget.
- **FT-5 Ordning:** OOC och spoilers tolkas **efter** att Markdown renderats och sanerats, på den färdiga HTML:en.
  Det som läggs till är fasta element; en spoilerrubrik får inte innehålla `<`, `>`, `"`, `[` eller `]` och är högst
  60 tecken. Taggar som inte stängs visas som vanlig text.
- **FT-6 Teckengränser** syns för den som skriver och kontrolleras på servern (avsnitt 15).

---

## 12. Karaktärer (fliken Characters)

### 12.1 Listan

- **KR-1:** Karaktärerna visas som kort med porträtt och namn i två grupper: spelarnas karaktärer (med spelarens
  namn) och **NPCs**.
- **KR-2:** Dolda NPC:er syns bara för GM, märkta *Hidden*. Arkiverade NPC:er ligger i en hopfälld del
  "Archived NPCs", bara för GM.
- **KR-3:** Knappen **New character** för spelare och GM. Det GM skapar är alltid en NPC.

### 12.2 Spelarnas karaktärer

- **KC-1 Fält:** namn (högst 60 tecken), porträtt ur biblioteket (avsnitt 13), regelsystem (högst 60 tecken, t.ex.
  "D&D 5e"), länk till ett externt rollformulär (högst 500 tecken, `http`/`https`) och ett **karaktärsdokument**
  i Markdown (högst 10 000 tecken) för beskrivning, HP, utrustning, tillstånd och anteckningar.
- **KC-2 Karaktärens sida:** porträtt, namn, "Played by *spelare*", regelsystem, länk till rollformuläret och
  dokumentet. Knappen *Edit* för ägaren och GM.
- **KC-3:** En spelare kan ha flera karaktärer i samma kampanj. En karaktär hör till en kampanj.

### 12.3 NPC:er

- **KN-1 Fält:** namn, porträtt, en **GM-anteckning** (Markdown, högst 2 000 tecken) som **bara GM ser**, samt
  "Hidden from players" med ett **alias** (samma gräns som namnet).
- **KN-2 Dolda NPC:er:** en dold NPC syns inte för spelarna, varken bland karaktärerna eller med riktigt namn i
  trådarna. Spelarna ser i stället aliaset (utan alias **"Unknown"**) och en neutral **siluett** i stället för
  porträttet, och namnet länkar inte till någon sida. Karaktärens sida ger **404** för spelare.
  GM ser alltid riktigt namn och porträtt, märkt *Hidden* och med "Players see: *alias*".
- **KN-3:** När GM gör NPC:n synlig visas riktigt namn och porträtt för alla, även i gamla inlägg. Namnet räknas
  alltså fram när sidan visas, inte när inlägget skrivs.
- **KN-4 Säkerhet:** riktigt namn, porträtt och GM-anteckning för en dold NPC får **aldrig** finnas i den HTML som
  skickas till en spelare, inte ens i dolda fält, attribut eller kommentarer.
- **KN-5 Arkivera:** GM kan arkivera en NPC som inte längre behövs och återställa den. En arkiverad NPC kan inte
  väljas i "Post as" men ligger kvar i gamla inlägg.

### 12.4 Ta bort karaktär

- **KD-1:** En karaktär kan bara tas bort om den **inte har skrivit några inlägg**, så att gamla inlägg behåller sin
  karaktär. Annars visas "*Namn* has written posts and can't be deleted." (för NPC:er: arkivera i stället).
- **KD-2:** Borttagning kräver bekräftelse.

---

## 13. Porträttbiblioteket

Porträtt laddas **inte** upp per karaktär. Administratörer och managers sköter ett gemensamt bibliotek, och alla väljer
porträtt därifrån.

- **PB-1 Ladda upp** (administratörer och managers, på sidan *Portraits*): JPG, PNG eller WebP, högst 5 MB och
  högst 8 000 px åt något håll.
- **PB-2 Bearbetning:** bilden beskärs till en kvadrat (mitten), skalas till 256×256, all metadata (EXIF m.m.) tas
  bort och den sparas som **WebP** med ett **slumpat filnamn** **utanför webbroten**.
- **PB-3 Servering:** bilderna serveras via ett skript bara för inloggade, med `Content-Type: image/webp`,
  `X-Content-Type-Options: nosniff` och lång cache (filnamnet ändras aldrig). Okänt filnamn eller försök att nå
  andra filer (`../`) ger 404.
- **PB-4 Taggar:** varje porträtt har taggar, t.ex. `#dwarf #fighter` (högst 20 taggar, högst 30 tecken var).
  Taggar sparas med små bokstäver utan `#`. Redan använda taggar föreslås när man skriver.
- **PB-5 Källa och licens:** valfritt fält, högst 300 tecken.
- **PB-6 Ändra och ta bort:** taggar och källa kan ändras. Ett porträtt får tas bort även om det används;
  karaktärerna som använde det får då initialer.
- **PB-7 Välja porträtt** (på karaktärens redigeringssida): ett rutnät med porträtt och en sökruta. Varje sökord
  matchar **början** av en tagg, och ett porträtt visas om det matchar **alla** sökord. Utan JavaScript räcker en
  vanlig sökning (GET-formulär) och ett rutnät av radioknappar.
- **PB-8:** Porträtten ligger kvar när en kampanj raderas.

---

## 14. Administration (sajtens roller)

- **AD-1 Rollsidan** (bara administratörer): lista över alla med rollen Administratör eller Manager, och
  administratörerna från konfigurationen (märkta, utan ta bort-knapp).
- **AD-2:** Ge en roll genom att skriva en registrerad användares e-postadress och välja roll. Tydliga fel för okänd
  adress och roll som användaren redan har.
- **AD-3:** Ta bort en roll. Man kan inte ta bort sin egen administratörsroll.
- **AD-4:** Ändrade roller gäller direkt, utan att användaren behöver logga in igen.
- **AD-5 Menyn** visar *Portraits* för managers och administratörer och *Roles* för administratörer. Andra får 404
  på sidorna.

---

## 15. Gränser och regler

| Vad | Gräns |
|---|---|
| Visningsnamn | 2–50 tecken |
| Lösenord | minst 8 tecken |
| Kampanjens namn | 1–100 tecken |
| Kampanjens taggar | högst 10, högst 30 tecken var |
| Kampanjer per sida (Other campaigns) | 25 |
| Kampanjens beskrivning | högst 2 000 tecken |
| Max antal spelare | 1–20 |
| Meddelande i ansökan | högst 1 000 tecken, valfritt |
| Trådens titel | 1–100 tecken |
| Inlägg | högst 5 000 tecken (får vara tomt om inlägget har slag) |
| Inlägg per sida | 20 |
| Trådar per sida | 20 |
| Tärningsslag per inlägg | högst 10 |
| Tärningar per slag | 1–50 |
| Modifierare | −999 till +999 |
| Beskrivning av slag | högst 100 tecken |
| Karaktärens namn och alias | 1–60 tecken |
| Regelsystem | högst 60 tecken |
| Länk till rollformulär | högst 500 tecken |
| Karaktärsdokument | högst 10 000 tecken |
| GM-anteckning | högst 2 000 tecken |
| Porträtt | JPG/PNG/WebP, högst 5 MB, högst 8 000 px; sparas 256×256 WebP |
| Taggar per porträtt | högst 20, högst 30 tecken var |
| Källa och licens | högst 300 tecken |
| Olästa som visas | 99, därefter "99+" |

Text trimmas innan längden kontrolleras. Alla gränser kontrolleras **på servern**; i formulären kan de dessutom
anges med `maxlength`.

---

## 16. Datamodell (förslag)

Tabellerna nedan räcker för allt i dokumentet. Namnen är förslag. Tider sparas i UTC.

```text
users ──< campaign_memberships >── campaigns
users ──< campaign_applications >── campaigns
campaigns ──< threads ──< posts ──< post_rolls
campaigns ──< characters ──< posts (valfri karaktär)
posts ──< post_revisions
posts ── reply_to? (posts)
users ──< read_markers >── threads
characters ── portrait? (portraits)
users ──< user_roles
```

| Tabell | Viktiga kolumner |
|---|---|
| users | id, email (unik, null för borttaget konto), email_confirmed, display_name, password_hash, is_deleted, failed_logins, locked_until, created_at |
| user_roles | user_id, role (`admin` / `manager`) |
| campaigns | id, name, description, game_master_id, max_players, status (`open` / `ongoing` / `closed` / `archived`), tags, default_roll (standard `1d20`), created_at, updated_at |
| campaign_memberships | campaign_id, user_id, joined_at |
| campaign_applications | id, campaign_id, user_id, message, status (`pending` / `approved` / `rejected` / `withdrawn`), submitted_at, decided_at, decided_by |
| threads | id, campaign_id, title, status (`active` / `completed`), position, created_by, created_at, updated_at |
| posts | id, thread_id, author_id, character_id (null), content, reply_to_post_id (null), created_at, edited_at (null), deleted_at (null), deleted_by (null) |
| post_rolls | post_id, sort_order, notation, label (null), count, sides, modifier, mode (`normal` / `advantage` / `disadvantage`), results (t.ex. `"4,18"` eller JSON), total |
| post_revisions | id, post_id, content, written_at, replaced_at |
| characters | id, campaign_id, owner_id, is_npc, name, portrait_id (null), sheet, sheet_url, rule_system, gm_note, is_hidden, alias, is_archived, created_at, updated_at |
| portraits | id, image_key (filnamn), tags, source, uploaded_by, created_at |
| read_markers | user_id, thread_id, last_read_post_id, updated_at |

Kommentarer:

- **Ordning på inlägg:** `posts.id` är stigande och används för ordning, sidor och läspositioner.
- **Taggar:** i PostgreSQL en `text[]`-kolumn med GIN-index, i MySQL/MariaDB egna tabeller `portrait_tags` och
  `campaign_tags`.
- **Radering av kampanj:** främmande nycklar med `ON DELETE CASCADE` från campaigns till trådar, karaktärer,
  medlemskap och ansökningar, och vidare till inlägg, slag, revisioner och läspositioner.
- **Borttaget konto:** användarraden behålls som gravsten (`is_deleted`, namnet "Deleted user"), så att inlägg och
  karaktärer har kvar sin författare.
- **"Senast använd" i "Post as"** räknas fram från senaste inlägg per karaktär; ingen egen kolumn behövs.

---

## 17. Sidor och adresser (förslag)

| Adress | Sida |
|---|---|
| `/` | Startsida för besökare; kampanjlistan för inloggade |
| `/account/register`, `/account/login`, `/account/logout`, `/account/forgot-password`, `/account/reset-password`, `/account/confirm-email` | Konto |
| `/account` | Mitt konto: namn, e-post, lösenord, ta bort kontot |
| `/campaigns/new` | Skapa kampanj |
| `/campaigns/{id}` | Trådlistan (`?status=completed&page=N`) |
| `/campaigns/{id}/settings` | Inställningar och radering (GM) |
| `/campaigns/{id}/threads/new` | Ny tråd (GM) |
| `/campaigns/{id}/threads/{tid}` | Trådsidan (`?page=N`, `?page=last`, `?post=N`, `?reply=N`, `?quote=N`, `?edit=N`) |
| `/campaigns/{id}/threads/{tid}/edit` | Döp om tråden (GM) |
| `/campaigns/{id}/characters` | Karaktärslistan |
| `/campaigns/{id}/characters/new` | Ny karaktär eller NPC |
| `/campaigns/{id}/characters/{cid}` | Karaktärens sida |
| `/campaigns/{id}/characters/{cid}/edit` | Redigera karaktären |
| `/campaigns/{id}/players` | Spelare och ansökningar |
| `/portraits` | Porträttbiblioteket (manager, administratör) |
| `/admin/roles` | Roller (administratör) |
| `/media/portraits/{key}` | Porträttbild (inloggad) |

Alla ändringar görs med **POST** (aldrig GET) och följs av en omdirigering (Post/Redirect/Get), så att en omladdning
inte skickar formuläret igen. Okända adresser och sådant man inte får se ger en vänlig 404-sida.

---

## 18. Utseende och tillgänglighet

- **UT-1:** Sajten har en sidomeny (mörk, med *Campaigns*, *Create campaign*, *Portraits*/*Roles* för de som har
  rollen, användarens namn och *Log out*) och innehållet till höger. På mobil fälls menyn ihop.
- **UT-2:** Alla sidor fungerar på mobil, utan horisontell scroll.
- **UT-3:** Inläggen är ljusa kort med runda porträtt (56 px), tydliga namn och små ikonknappar med text för
  skärmläsare (`aria-label`) och `title`.
- **UT-4:** Statusar och märken som små runda etiketter: *Active* grön, *Completed* grå, *GM* mörk, *NPC* grå,
  *Hidden* gul, olästa röda.
- **UT-5:** Formulär har etiketter, felmeddelanden visas vid fältet och överst, och allt ifyllt ligger kvar efter
  ett fel.
- **UT-6:** Gränssnittet är på engelska. Datum skrivs på engelska ("2 Oct 2026, 19:44") i svensk tid.

---

## 19. Säkerhet

- **SÄ-1 Lösenord** sparas med `password_hash()` och kontrolleras med `password_verify()`.
- **SÄ-2 Sessioner:** säkra cookies (`HttpOnly`, `Secure`, `SameSite=Lax`), nytt sessions-id vid inloggning,
  tillfällig låsning efter upprepade felaktiga inloggningar.
- **SÄ-3 CSRF-skydd** på alla formulär.
- **SÄ-4 SQL:** bara förberedda frågor (prepared statements).
- **SÄ-5 Utdata:** all text som inte är sanerad Markdown skrivs ut med `htmlspecialchars()`.
- **SÄ-6 Behörighet** kontrolleras på servern för varje anrop (avsnitt 3).
- **SÄ-7 Dolda NPC:er:** se KN-4.
- **SÄ-8 Tärningar:** slås och sparas bara på servern (avsnitt 10.3); data från formuläret kan aldrig ange ett
  resultat.
- **SÄ-9 Bilder:** se PB-1–PB-3. Uppladdade filer körs aldrig och ligger utanför webbroten.
- **SÄ-10 Mjuk radering och historik:** borttagna inlägg och tidigare versioner av redigerade inlägg sparas i
  databasen men visas inte.
- **SÄ-11 E-post:** bekräftelse och lösenordsåterställning kräver en riktig e-posttjänst (SMTP) i drift.

---

## 20. Förslag på bibliotek i PHP

Inget av detta är ett krav, men det motsvarar det .NET-versionen använder:

| Behov | Förslag |
|---|---|
| Markdown | `league/commonmark` med `html_input: escape`, `allow_unsafe_links: false` och tillägget för genomstruken text |
| Sanering av HTML | `ezyang/htmlpurifier` med vitlistan i FT-2 |
| Bilder | GD eller Imagick (beskära, skala, spara som WebP utan metadata) |
| Databas | PDO mot PostgreSQL eller MySQL/MariaDB |
| E-post | `symfony/mailer` eller `phpmailer/phpmailer` |
| Slump | `random_int()` |

---

## 21. Utanför den här versionen

Sådant som har diskuterats men inte är byggt: inloggning med Google, Facebook eller Discord, privata meddelanden,
notiser och e-postnotiser, privata (dolda) tärningsslag, reaktioner, bilder i inlägg, sökning, bokmärken, export,
mer avancerad tärningssyntax (t.ex. `4d6kh3`), att spelare skapar egna trådar och förhandsvisning av en karaktär när
man håller muspekaren över namnet.
