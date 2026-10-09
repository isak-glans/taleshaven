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
| B1 | Krönikans struktur | *Ersätts av B33 i fas 6.* **Ett krönikeinlägg = ett kapitel.** Kapitlen numreras löpande inom kampanjen. Max **5 000 tecken** per kapitel. |
| B2 | Ansökan till kampanj | Ansökan görs via ett formulär med **meddelandefält** till GM. Privata meddelanden (PM) ingår inte i MVP. |
| B3 | Tärningskast | *Ersätts av B31 i fas 6.* Tärningar slås **endast i OOC-kanalen**. Tärningar i RPG kan övervägas senare. |
| B4 | Inloggning | Fas 1: **vanligt konto med e-post och lösenord** (primärt). Google, Facebook och Discord kan läggas till senare. |
| B5 | Databas | **PostgreSQL**. Under utveckling körs den i Docker via `docker-compose.yml` i repots rot. |
| B6 | RPG-flikens form | *Ersätts av B25 i fas 6.* RPG är en **chatt i stil med Discord**, inte en lista med trådar. Varje kampanj har **en RPG-kanal**. Flera kanaler är en möjlig förbättring (avsnitt 5); datamodellen tillåter det. |
| B7 | OOC-flikens form | *Ersätts av B25 i fas 6.* OOC är en chatt som fungerar som RPG-chatten, men här kan man även **slå tärningar**. |
| B8 | Vad som visas i chatten | *Ersätts av B28 i fas 6.* Inläggen från de **senaste 7 dagarna**, men **minst 20** och **högst cirka 100** inlägg. Äldre inlägg laddas automatiskt när man **scrollar uppåt**. Gäller både RPG och OOC. |
| B9 | Skicka inlägg | *Ersätts av B28 i fas 6.* **OOC:** Enter skickar, Shift+Enter ger ny rad. **RPG:** Enter ger ny rad, Ctrl+Enter skickar. En skicka-knapp finns alltid (mobil). |
| B10 | Krönikans form | *Ersätts av B33 i fas 6.* Krönikan läses **som en bok**, äldst först, **ett kapitel i taget** (ändrat från 5 per sida, se B14). |
| B11 | Befintliga RPG-trådar | De trådar som skapades innan chattformatet är testdata och **tas bort** vid ombyggnaden. |
| B12 | Redigering av inlägg | Egna chattinlägg kan redigeras och markeras då som **"redigerad"**. Byggs efter chattombyggnaden. |
| B13 | Chattens höjd | *Ersätts av B28 i fas 6.* RPG- och OOC-chatten **fyller resten av skärmen**. Rubrik och flikar står fast; bara inläggen scrollar, och skrivfältet ligger längst ner. |
| B14 | Bläddra i krönikan | *Ersätts av B33 i fas 6.* **Ett kapitel per sida** (sida N = kapitel N), max **5 000 tecken** per kapitel. Kapitlet visas i en **läsyta som fyller resten av skärmen**, så att rubrik och flikar alltid syns; långa kapitel scrollar inuti läsytan. Innehållsförteckningen öppnas som en meny. **Längst ner** finns sidnavigering som alltid syns: med fler än två kapitel klassisk forumnavigering (« Första · ‹ Föregående · 1 … 5 6 7 … 12 · Nästa › · Sista »), med två kapitel bara Föregående/Nästa. Tangenterna ← → bläddrar. |
| B15 | NPC:ers innehåll | *NPC:er kan även ha räknare, tillstånd och sparade slag (B56).* En NPC har bara **namn, porträtt och en anteckning som bara GM ser** (högst 2 000 tecken, Markdown). Inget karaktärsdokument, ingen extern länk och inget regelsystem; äldre sådana uppgifter på NPC:er döljs men raderas inte. Anteckningen skickas aldrig till spelarnas webbläsare. |
| B16 | Dolda NPC:er | GM kan välja **"Dold för spelarna"** och visa NPC:n senare. En dold NPC syns inte för spelarna, varken på fliken Karaktärer eller i chatten. I chatten visas i stället ett **alias** som GM väljer (utan alias "Okänd") och en neutral siluett. När NPC:n görs synlig visas riktigt namn och porträtt, även i gamla inlägg. GM ser alltid det riktiga namnet, märkt "Dold". |
| B17 | Välja NPC | "Skriv som" blir en **sökbar väljare** med de senast använda NPC:erna överst. GM kan **arkivera** NPC:er som inte längre behövs; de göms i väljaren men finns kvar i gamla inlägg. |
| B18 | Roller för hela sajten | **Administratör:** får allt, även dela ut roller. **Manager:** sköter porträttbiblioteket. De första administratörerna anges med e-postadress i konfigurationen (`Admin:Emails`); därefter delar en administratör ut roller på en egen sida. |
| B19 | Porträttbibliotek | Porträtt kan **inte längre laddas upp per karaktär**. Administratörer och managers laddar upp porträtt till ett gemensamt bibliotek och sätter **taggar** (t.ex. `#dvärg #krigare`). När man väljer porträtt för en karaktär eller NPC bläddrar man i biblioteket och söker på taggar. Varje porträtt har ett valfritt fält för **källa och licens**. De uppladdade testbilderna tas bort när biblioteket införs. |
| B20 | Ta bort porträtt som används | Tillåtet. Karaktärerna som använde porträttet får initialer i stället. |
| B21 | Borttaget användarkonto | Inläggen ligger kvar men visas som **"Deleted user"**; kontot och e-postadressen raderas. |
| B22 | Dra tillbaka ansökan | Den som väntar på svar kan **dra tillbaka sin ansökan** under fliken Spelare. |
| B23 | Längd på chattinlägg | RPG- och OOC-inlägg får vara högst **5 000 tecken** (ändrat från 10 000, F10). Befintliga längre inlägg ligger kvar. |
| B24 | Språk | **Gränssnittet är på engelska**: rubriker, knappar, felmeddelanden, datum och kontosidor. Tärningskommandot är `/roll` (`/slå` tas bort; `/roll` tas i sin tur bort i B42). Den här projektbeskrivningen skrivs fortfarande på svenska. |

#### Beslut för fas 6 – Trådar (2026-09-27)

Kampanjen byggs om från chatt till ett forum med trådar. Grundidén: *Taleshaven hanterar en kampanjs berättelse
som en samling trådar och inlägg, inte som en chatt.* Känslan ska vara lika kompakt och lättillgänglig som
dagens RPG-vy, men inläggen är bestående delar av berättelsen, inte flyktiga chattmeddelanden.
Besluten nedan ersätter B1, B3, B6–B10, B13, B14, F2 (delvis), F13 och F14 när fas 6 är byggd.

| # | Fråga | Beslut |
|---|-------|--------|
| B25 | Trådar | *Typ, introduktion och krönika ersätts av B37.* En kampanj består av **trådar** som GM skapar och namnger fritt, i valfritt antal. En tråd har titel, **typ** (*Story* eller *Discussion*), **status** (*Active* eller *Completed*), introduktion och, för Story-trådar, krönika. Det finns **inga fasta RPG- eller OOC-trådar**; OOC är en vanlig Discussion-tråd som GM skapar om hen vill. *Completed* är inte samma sak som arkiverad: tråden är avslutad men är fortfarande en del av kampanjens historia. Bara GM skapar trådar i första versionen. |
| B26 | Flikar | Kampanjens flikar blir **Threads · Characters · Players**. Flikarna RPG, OOC och Chronicle tas bort. Threads är kampanjens startsida. |
| B27 | Trådlistan | *Typ och utdrag tas bort i B37.* Trådarna visas som **kompakta textrader** utan bilder och utan stora ikoner: titel, typ, status, antal inlägg, antal deltagare, senaste inlägg (vem och när, med avatar) och olästmarkering med antal ("3 new posts"). **Aktiva trådar ligger före avslutade.** GM styr ordningen med en position (flytta ↑/↓), eftersom kapitlens ordning inte alltid följer när de skapades. En dold NPC visas med sitt alias under "senaste inlägg" och räknas inte som deltagare (B16). |
| B28 | Trådsidan | *Introduktion, krönika och typ tas bort i B37; 20 per sida enligt B35.* Överst titel, typ, status, antal inlägg och deltagare, och därunder **introduktionen** (eller krönikan, B33). Inläggen är **sidindelade, 25 per sida**, med sidnavigering längst ner: Previous/Next, sidnummer med … för långa trådar, och **Latest**. **Ingen oändlig scroll och ingen automatisk scroll.** Sidan scrollar som en vanlig webbsida (ersätter B8 och B13). En tråd öppnas vid **första olästa inlägget**. Nya inlägg läggs till direkt bara för den som står på sista sidan; andra ser en rad "N new posts – go to latest". Skrivfältet ligger under sista sidan och knappen heter **Post**. Enter ger ny rad och Ctrl+Enter publicerar i alla trådar (ersätter B9). En liten trådinformation visar typ, status, deltagare, introduktionen (när krönikan visas) och genvägar till första olästa och senaste inlägget. |
| B29 | Skriva som | *Ersätts av B37.* I **Story-trådar** skriver man som karaktär, NPC eller berättare, som i dagens RPG-chatt. I **Discussion-trådar** skriver man som sig själv. |
| B30 | Svara, citera, redigera, ta bort | Varje inlägg har **Reply, Quote, Edit och Delete** (vid hovring eller i en `…`-meny). Trådarna är **platta**, utan nästlade svar. **Quote** lägger in det citerade som ett citat med namn. **Reply** visar en rad "Replying to …" med länk till originalet. Varje inlägg har en fast länk (`?post=N`) som hittar rätt sida. Man redigerar och tar bort sina egna inlägg; **GM kan redigera och ta bort alla inlägg i sin kampanj.** Redigerade inlägg visas med "(edited)". Borttagning är **mjuk**: inlägget ligger kvar på sin plats med texten "This post was deleted.", och innehållet sparas i historiken. |
| B31 | Tärningar i texten | *Ersätts av B42: slagen läggs inte längre i texten, och `/roll` är borttaget. Reglerna om att slag görs på servern och inte kan ändras gäller fortfarande.* Tärningar kan slås i **alla trådar**, mitt i texten: `[dice]1d20+3[/dice]`. Slaget görs **på servern när inlägget publiceras**, och resultatet visas där taggen stod. Förhandsgranskningen visar "rolls when posted" i stället för ett resultat. I den sparade texten byts taggen mot en referens (`[dice:N]`) till det sparade slaget. **Slag kan inte ändras eller tas bort genom redigering**, och nya taggar i en redigering slås inte. **Inlägg med slag kan bara tas bort av GM.** Högst 10 slag per inlägg. En knapp 🎲 och tärningspanelen lägger in taggen, och `/roll 1d20` finns kvar som genväg för ett inlägg med bara ett slag. Ersätter B3 och F13. |
| B32 | Avsluta en tråd | *Krönikesteget tas bort i B37.* GM sätter en tråd till *Completed*. För en Story-tråd **uppmanas GM att skriva krönikan** i samma steg, men kan hoppa över och skriva den senare. En avslutad tråd är **skrivskyddad för spelarna**. GM kan öppna den igen om den avslutades av misstag. Discussion-trådar kan också avslutas, men har ingen krönika. |
| B33 | Krönika per Story-tråd | *Ersätts av B37.* Den globala krönikan tas bort. **Varje Story-tråd har en egen krönika**: ett redigerbart dokument (Markdown, högst 5 000 tecken), inte en serie inlägg. När tråden är *Completed* **visas krönikan i stället för introduktionen**. Introduktionen raderas inte utan finns kvar under trådinformationen. Bara GM skriver och redigerar krönikan i första versionen. Den visar "Last edited by …". Syftet är att en ny spelare ska kunna läsa de avslutade kapitlens krönikor ("Previously on …") och sedan gå in i den aktiva tråden, utan att läsa varje gammalt inlägg. Ersätter B1, B10, B14 och F14. |
| B34 | Befintlig data | **Ingenting flyttas över**; det som finns är testdata. Gamla chattar och krönikekapitel tas bort, och en ny exempelkampanj skapas i trådformatet. Gamla databasfält tas bort först när den nya modellen fungerar. |
| B35 | Trådsidans utseende (2026-09-27) | Inläggen visas som **ljusa kort** med **stående, rektangulära porträtt** (ca 64×80, rundade hörn; de kvadratiska biblioteksporträtten beskärs i visningen, initialer och siluett i samma form), namn, märkning (GM, NPC, Hidden) och **relativ tid** (exakt tid vid hovring). **20 inlägg per sida** (ändrat från 25 i B28). Åtgärderna är **små ikoner**: Reply, Quote och Edit syns, och Delete och Copy link ligger i en **"…"-meny** per inlägg. Uppe till höger syns **Complete thread**; Edit thread, Chronicle och Reopen ligger i en "…"-meny. Sidnavigeringen: Previous till vänster, sidnumren i mitten, Latest till höger. Inga flikar i tråden, och inga knappar för bilder eller emoji i skrivfältet (bilder i inlägg ligger under "Senare"). Gränssnittet är på engelska (B24). |
| B36 | Läsbarhet, OOC och tärningsknapp (2026-09-27) | **Porträtten är runda igen**, men större: 56 px (ersätter de rektangulära i B35). Inläggstexten får en **maxbredd** (ca 72 tecken per rad) så att den blir lätt att läsa. **OOC-text i inlägg:** `[ooc]…[/ooc]` visas grå, kursiv och märkt "OOC" – mitt i ett stycke eller som ett eget block över flera stycken – så att den inte läses som in character. *Dice-knappen tas bort i B42.* Verktygsraden får knapparna **🎲 Dice** och **OOC**, som lägger taggarna runt markerad text. Den separata tärningsknappen och tärningspanelen ovanför skrivfältet tas bort. Dice-knappen finns bara när man skriver ett nytt inlägg, eftersom nya slag inte görs vid redigering (B31). |
| B37 | Bara trådar (2026-10-02) | **Trådtyperna Story och Discussion, introduktionen och krönikan per tråd tas bort.** En tråd har bara **titel** och **status** (*Active* eller *Completed*); statusen syns i trådlistan och på trådsidan. Vill gruppen ha en krönika skapar GM en vanlig tråd för den, t.ex. *Chronicle*, med ett inlägg per kapitel. GM avslutar en tråd med knappen **Complete thread** och en bekräftelse direkt i tråden, utan krönikesteg. **"Post as" finns i alla trådar** och minns valet: förvalt är det man senast skrev som i just den tråden, annars spelarens första karaktär eller GM som berättare. **Namn och porträtt på en karaktär eller NPC i ett inlägg länkar till karaktärens sida**, utom en dold NPC för spelarna (B16). Inlägg utan karaktär länkas inte. Ersätter B29 och B33 och delar av B25, B27, B28 och B32. |
| B38 | Ny exempeldata (2026-10-02) | **Alla kampanjer raderas**, även testkampanjerna, och exempelkampanjen *The Salt Road* skapas om efter B37: en *Chronicle*-tråd med en sammanfattning per kapitel, kapitel 1 och 2 avslutade, kapitel 3 (en strid i cisternen med tärningsslag) pågående, samt *OOC* och *House rules*. |
| B39 | Trådens meny (2026-10-02) | Alla trådens åtgärder ligger i **"…"-menyn** längst till höger: Complete thread, Rename thread och Reopen. Bekräftelsen för att avsluta visas som en rad under trådens rubrik. |
| B40 | Spoiler | `[spoiler=Rubrik]…[/spoiler]` på egna rader blir en **hopfälld ruta** som är stängd tills läsaren öppnar den (utan rubrik heter den "Spoiler"). `[spoiler]…[/spoiler]` mitt i en mening döljer texten tills man klickar på den. Verktygsraden får knappen **Spoiler**. Texten är bara hopfälld, inte hemlig: den skickas till alla som får läsa inlägget. |
| B41 | Trådlistan med filter och sidor | Trådlistan får filtret **Active · Completed** (Active är förvalt) med antal trådar och antal olästa inlägg per filter. Varje filter visas **20 trådar per sida** med samma sidnavigering som i trådarna, men utan Latest. ↑/↓ fungerar som förut, och man stannar på samma filter och sida. Avslutade trådar visas med den senaste först. |
| B42 | Tärningsslag som lista | Tärningar skrivs **inte längre i texten**. Under skrivfältet finns **🎲 Add roll**; varje rad har formel (`1d20+5`), beskrivning och **Normal / Advantage / Disadvantage** (bara för en ensam d20). Formeln kontrolleras medan man skriver och felet visas när man lämnar fältet. Slagen sparas i utkastet, visas som "rolls when posted" i förhandsgranskningen och **slås på servern när inlägget postas**. I inlägget visas de som en **lista under texten**: beskrivning, formel, tärningarna och summan; vid fördel eller nackdel visas båda tärningarna och den som inte räknas är överstruken. Ett inlägg kan bestå av bara slag. Vid redigering ändras bara texten. Högst 10 slag per inlägg, och inlägg med slag kan bara tas bort av GM (som i B31). `[dice]` i texten slås inte längre, och **`/roll` tas bort**. Ersätter B31:s taggar i texten. |
| B43 | Ny exempelkampanj (2026-10-02) | Alla kampanjer raderas och en ny kampanj skrivs: *Lanterns of Greywater* (GM Leif, spelare Gunnar och Freja), med slag som lista, fördel och nackdel, spoilers, OOC-text och en dold NPC. |
| B44 | Inläggets åtgärder (2026-10-07) | **Reply** är en liten ikon i inläggets övre högra hörn, bredvid "…". **Quote, Edit, Copy link och Delete** ligger i "…"-menyn. Ikonraden under texten tas bort (ändrar B35). |
| B45 | Sidindelning av trådar | Finns redan: trådlistan visar 20 trådar per sida och filter (B41), och inläggen 20 per sida (B35). Frågan gällde kampanjlistan, se B48. |
| B46 | Standardtärning per kampanj | GM väljer under *Settings* (och när kampanjen skapas) en **standardformel**, t.ex. `1d20`, `1d100` eller `2d6`, ur en lista med förslag eller som egen formel. Den kontrolleras som andra formler och sparas normaliserad (`d100` → `1d100`). **Add roll** fyller i formeln och lägger markören sist, så att man bara skriver t.ex. `+5`. Standard är `1d20`. |
| B47 | Taggar på kampanjer | GM sätter **upp till 10 taggar** på kampanjen, t.ex. `#dnd5e #horror`, när den skapas och under *Settings*. Samma regler som för porträtt: små bokstäver, högst 30 tecken, bokstäver, siffror och bindestreck. Taggar som används på andra kampanjer visas som förslag. Taggarna visas i kampanjlistan och i kampanjens huvud; ett klick filtrerar kampanjlistan på taggen. |
| B48 | Kompakt kampanjlista | Kampanjlistan visar en **rad per kampanj**: namn, status, taggar, olästa, GM och antal spelare, och Apply eller "Application sent". **Beskrivningen visas inte** i listan, bara under "About the campaign". Kampanjer man är GM eller spelare i ligger överst under **My campaigns**; övriga under **Other campaigns** (eller **All campaigns** om man inte är med i någon), **25 per sida**. |
| B49 | Filter i kampanjlistan | En sökruta överst filtrerar på **namn och taggar**: varje ord ska matcha början av ett ord i namnet eller början av en tagg, och kampanjen visas om **alla** ord matchar. Filtret ligger i adressen (`/?q=horror`) och fungerar utan JavaScript; sidindelningen behåller filtret. |
| B50 | Profilbild (2026-10-09) | Användaren väljer en **profilbild ur porträttbiblioteket** under Account › Profile; ingen egen uppladdning (B19). Väljaren visar först bilder med taggen **`#profile`** (finns inga visas hela biblioteket), och man kan söka på andra taggar eller visa alla (ersatt av B62: alla porträtt). Bilden visas där användaren visas utan karaktär: inlägg skrivna som sig själv eller **som GM-berättare**, "Latest" i trådlistan, spelarlistan, ansökningar hos GM och i sidomenyn. Utan bild visas initialer. Tas porträttet bort ur biblioteket får användaren initialer igen (som B20), och ett borttaget konto förlorar bilden (B21). |
| B51 | Sidor i profilbildsväljaren | *40 per sida enligt B53.* Väljaren visar **20 bilder per sida** med samma sidnavigering som i trådarna (utan Latest), så att den fungerar även med hundratals bilder. Sidbytet behåller sökningen. |
| B52 | Profilsida (2026-10-09) | Varje användare har en **profilsida** (`/users/{id}`) som alla inloggade kan se: profilbild, namn, **"Member since"** (när kontot skapades), **antal inlägg** (borttagna räknas inte), texten **"About me"** och kampanjerna personen är GM eller spelare i. "About me" skrivs under Account › Profile (Markdown, högst 2 000 tecken). Namn och bild för inlägg skrivna utan karaktär, spelarens namn bredvid en karaktär, namnen i spelarlistan och ansökningarna samt GM:s namn i kampanjens huvud **länkar till profilsidan**. Borttagna konton har ingen profil och länkas inte. För konton från tiden före B52 räknas skapandedatumet fram från deras tidigaste aktivitet. |
| B53 | Profilbilder i biblioteket (2026-10-09) | **168 profilbilder** klipps ut ur tre ark i `assets/` (runda, 256×256 WebP med genomskinliga hörn) och läggs in i porträttbiblioteket med taggen `profile` och beskrivande taggar (t.ex. `fox animal`, `dragon creature`, `elf woman character`, `lantern object`). De utklippta filerna ligger i `assets/profile_images/` men versionshanteras inte. De 25 tidigare uppladdade dubbletterna tas bort. Profilbildsväljaren visar **40 bilder per sida** (ändrar B51). |
| B54 | Tydligare profilsida (2026-10-09) | Profilbilden ligger **först** på Account › Profile, och **ett klick på en bild sparar den direkt** (utan JavaScript finns knappen *Use selected picture* kvar). Namn och About me ligger under rubriken *Name and about me* med knappen *Save name and about me*, så att det inte ser ut som att bilden kräver den knappen. Exempel-NPC:erna i *Lanterns of Greywater* får bilder ur de nya profilbilderna. |
| B55 | "Post as" syns alltid (2026-10-09) | Raden **Post as** visas alltid ovanför skrivfältet, även när man inte har något att välja mellan. Utan NPC:er ser GM *GM (narrator)* med sin profilbild och "Create an NPC under Characters to post as it"; en spelare utan karaktärer ser *Myself, no character* och motsvarande länk. Tidigare försvann raden helt, så att GM inte såg att det går att skriva som NPC:er. |
| B56 | Räknare, tillstånd och sparade slag (2026-10-09) | Alla karaktärer, även NPC:er, kan ha **räknare** (namn, värde och max, t.ex. *HP 28 / 38*, med − och + och en stapel), **tillstånd** (t.ex. *Poisoned*) och **sparade tärningsslag** (namn, formel och fördel/nackdel). De visas och ändras direkt på karaktärens sida. Ägaren och GM ändrar; alla som får se karaktären ser dem. För en **NPC** ser bara GM räknare och slag – tillstånden ser alla. Tillståndsfältet föreslår **D&D 5e:s tillstånd och *Bloodied*** samt de som redan används i kampanjen; *Bloodied* och *Unconscious* markeras rött. Högst 20 räknare, 20 tillstånd och 30 slag, namn högst 40 tecken. Ändringar syns bara på karaktärssidan, inte i tråden. |
| B57 | Sparade slag i skrivfältet | När man skriver som en karaktär visas dess sparade slag under *🎲 Add roll* (högst fem; fler hittas med en sökruta). Ett klick lägger till en vanlig slagrad med formel, beskrivning och fördel/nackdel ifyllda, som slås när inlägget postas (B42). |
| B58 | Ikoner (2026-10-09) | Räknare, tillstånd och sparade slag kan ha en **ikon** ur biblioteket: bilder med taggen **`icon`**. När man lägger till något **föreslås en ikon automatiskt** efter namnet ("Shortsword" → ikonen taggad `shortsword`, annars `sword`; "Thieves' tools" → `thieves-tools`; kategoritaggar som `weapon` räknas inte). Förslaget **sparas** och byts inte av sig själv; den som får ändra klickar på ikonen för att välja en annan (sökbar), *Suggest* igen eller *No icon*. Ikonerna syns på karaktärssidan och på de sparade slagen i skrivfältet. Porträtt- och profilbildsväljaren visar inte ikoner om man inte söker på `icon`. 96 föremålsikoner och 77 monsterporträtt (taggen `monster`) klipps ut ur arken i `assets/` och läggs i biblioteket; de utklippta filerna versionshanteras inte. |
| B59 | Bilder i inlägg (2026-10-09) | En **https-länk till en bildfil** (`.png`, `.jpg`, `.jpeg`, `.gif`, `.webp`, ev. med `?…`) i ett inlägg visas som **bild direkt under länken**, i skyddat läge: bara en `<img>` (aldrig text, iframe eller skript), högst 480 px bred och 300 px hög, `loading="lazy"` och `referrerpolicy="no-referrer"`, och bilden länkar till originalet i en ny flik (`rel="nofollow noopener noreferrer"`). Högst 5 bilder per inlägg; fler länkar förblir vanliga länkar. `![alt](url)` blir en länk med samma förhandsvisning. Bilden läggs till efter sanering, så rå HTML är fortfarande avstängd. Gäller trådinlägg, inte kampanj- eller karaktärstexter. Bilden hämtas från den andra servern av läsarens webbläsare (servern ser IP-adressen). |
| B60 | Ikoner på postade slag (2026-10-09) | Ett slag i ett inlägg visar sin **ikon** i stället för 🎲. Ett sparat slag tar med sin ikon (kontrolleras mot taggen `icon`); ett slag med beskrivning men utan ikon får ett förslag efter beskrivningen som i B58. Ikonen sparas med slaget och ändras inte. **Lås tråd** utgår: *Completed* räcker. |
| B61 | Bildtyp: porträtt eller ikon (2026-10-09) | Varje bild i biblioteket har en **typ**: *Portrait* eller *Icon*. Typen väljs när bilden laddas upp och kan ändras. Porträtt väljs till karaktärer, NPC:er och profiler; ikoner till räknare, tillstånd och tärningsslag (B58, B60). En ikon visas **aldrig** i porträttväljarna och ett porträtt aldrig i ikonväljaren. Gränsen mellan porträtt och varelse (är en goblin ett porträtt eller ett monster?) styrs inte av typen utan av taggar, eftersom en bild kan ha flera. Taggarna `icon` och `profile` styr inte längre något och tas bort. |
| B62 | En porträttväljare (2026-10-09) | Profilbildsväljaren och porträttväljaren för karaktärer och NPC:er visar **samma bilder**: alla porträtt, utan förvalt filter (ersätter `#profile` i B50). Överst finns **kategoriknappar** för de kategorier som finns i biblioteket: *#human #elf #dwarf #monster #animal #object*, följt av de vanligaste övriga taggarna. Spelare kan välja alla porträtt, även djur, föremål och monster. |
| B63 | Behörighet för biblioteket (2026-10-09) | Bara **administratörer och managers** lägger till, ändrar och tar bort bilder (gällde redan sedan B19). Sidan heter nu *Image library* och kan filtreras på *All / Portraits / Icons*. |
| B64 | Taggstandard och import (2026-10-09) | **Taggar:** engelska, små bokstäver, singular, bindestreck mellan ord (`red-hair`, `wood-elf`). Ett porträtt får först folkslag eller kategori (`human`, `elf`, `dwarf`, `halfling`, `gnome`, `orc`, `tiefling` … eller `monster`, `animal`, `object`), sedan `man`/`woman` när det syns, sedan kännetecken (hår, skägg, utrustning) och roll (`wizard`, `ranger`). En ikon får först sitt namn (den första taggen räknas som namnet vid automatiskt förslag, B58), sedan synonymer och kategori (`weapon`, `condition`). *(Källark med taggfiler, importnyckel och kommandona `images preview/import/export` infördes här men ersattes samma dag av B65.)* |
| B65 | Manifest och inkorg för bilder (2026-10-09) | **Bildmappen** heter `media/images/` (förut `media/portraits/`) och adresserna `media/images/{nyckel}`; filnamnen är fortfarande slumpade. **Manifest:** `media/images/manifest.csv` (`file,kind,tags,source`) skrivs om från databasen efter varje ändring i biblioteket, **bara i utvecklingsmiljön** (i produktion läses det bara, så att flera servrar aldrig skriver samma fil). När appen startar läggs bilder som står i manifestet och finns som fil, men saknas i databasen, till; så fylls en ny databas i genom att kopiera bildmappen (t.ex. i produktion). **Inkorg (bara utvecklingsläge):** ark som läggs i `assets/new_images/sheets/` klipps ut när appen startar: varje rund bild blir en 256×256 WebP i inkorgen (`<ark>_<rad>-<kolumn>.webp`) och ett numrerat översiktsark skapas, och arket flyttas till `assets/sources/`; utklippen taggas sedan i inkorgens manifest (t.ex. av AI-agenten). nya bilder läggs i `assets/new_images/` tillsammans med en `manifest.csv` i samma format (filnamnet är inkorgens). Vid start bearbetas varje bild som har en rad (beskärs, skalas och kodas om som vid uppladdning), sparas i bildmappen och databasen, och tas bort ur inkorgen; när inkorgen är tom tas även manifestet och översiktsarken bort. Bilder utan rad eller med ogiltiga taggar ligger kvar och nämns i loggen. **Kontrollsumma:** varje bild har en SHA-256 av den sparade filen, och en bild i inkorgen som redan finns läggs inte in igen. (Bilder som lades in före B65 utan bearbetning känns inte igen på det sättet.) **Källark** ligger kvar som råmaterial i `assets/sources/` (versionshanteras inte); `dotnet run --project src/Taleshaven.Web -- images cut <ark.png> <mapp>` klipper ut ett ark med ett numrerat översiktsark, som underlag för inkorgen. Taggfilerna, importnyckeln och kommandona från B64 tas bort. Bilder som läggs in från manifestet har ingen uppladdare. |

### 1.2 Arbetsförslag (ej slutligt beslutade)

| # | Fråga | Förslag |
|---|-------|---------|
| F1 | Läsbarhet för utomstående | Kampanjinnehåll är läsbart för alla inloggade som standard (enligt skissen). En kampanj kan markeras som privat. |
| F2 | Olästa inlägg | *I fas 6 per tråd, se B27–B28.* Läsposition per kanal för deltagare. Andras inlägg efter läspositionen är olästa (egna räknas inte). Antalet visas på flikarna RPG/OOC och som "N nya" i kampanjlistan (över 99 visas "99+"). Chatten öppnas vid första olästa med en linje "Nya inlägg", och allt i en öppen chatt räknas som läst. |
| F3 | NPC:er | GM kan skapa NPC-karaktärer och skriva som dem i RPG-chatten. |
| F4 | Karaktär och kampanj | En karaktär tillhör en kampanj. En spelare kan ha flera karaktärer i samma kampanj. |
| F5 | Textformat | Text lagras som Markdown, renderas server-side och saneras före visning. |
| F6 | Redigeringshistorik | Tidigare versioner av redigerade inlägg sparas (se B12). Historiken visas inte i gränssnittet än. |
| F7 | Ny ansökan efter avslag | Tillåten. Den avslagna ansökan finns kvar som historik. |
| F8 | Meddelande i ansökan | Valfritt, högst 1 000 tecken. |
| F9 | Full kampanj | Väntande ansökningar ligger kvar men kan inte godkännas förrän det finns plats. |
| F10 | Längd på RPG- och OOC-inlägg | *Ersätts av B23.* Högst 10 000 tecken (Markdown-källtexten). |
| F11 | Ansökningar i spelrummet | Ansökningsformuläret och GM:ns ansökningslista ligger under fliken **Spelare**. |
| F12 | Skrivskydd | Stängda och arkiverade kampanjer är skrivskyddade för alla utom GM. |
| F13 | Tärningskommando | *Ersätts av B31 i fas 6.* I OOC slår man med `/roll 2d6+3` (B24) eller via en tärningsknapp bredvid skrivfältet. |
| F14 | Krönikans numrering | *Ersätts av B27 och B33 i fas 6.* Kapitelnumret följer ordningen i boken. Flyttar GM ett kapitel numreras de övriga om. |
| F15 | Karaktärsbilder | *Ersätts av porträttbiblioteket (B19).* Laddas upp (JPG, PNG eller WebP, högst 5 MB). Bilden beskärs till en kvadrat, skalas till 256×256, metadata tas bort och den sparas som WebP utanför wwwroot. |
| F16 | GM:s karaktärer | Allt GM skapar är NPC:er. Spelare skriver som egna karaktärer eller som sig själva; GM som NPC eller som berättare. |
| F17 | Ta bort karaktär | Går bara om karaktären inte har skrivit några inlägg, så att gamla inlägg behåller sin karaktär. |
| F18 | Ny spelare och olästa | När en spelare godkänns räknas allt som redan skrivits som läst, så att historiken inte blir hundratals olästa. |
| F19 | Radera kampanj | GM skriver kampanjens namn för att bekräfta. Allt innehåll raderas; porträtten ligger kvar i biblioteket (B19). Arkivering rekommenderas för kampanjer som bara är avslutade. |
| F20 | Ta bort spelare | Spelarens inlägg och karaktärer finns kvar. Spelaren kan ansöka igen och börjar då om utan olästa (F18). |

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
- K-4: En användare har en profil med visningsnamn, eventuell profilbild ur porträttbiblioteket (B50) och en
  profilsida med "About me" som andra kan se (B52).
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
- A-3: GM kan öppna, stänga, arkivera och återställa kampanjen genom att byta status.
  Radering kräver att GM skriver kampanjens namn (F19).
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

> Avsnitt 3.5–3.9 beskriver spelrummet som det är byggt i dag (fas 1–5). I fas 6 ersätts de av
> trådarna i avsnitt 3.14 (B25–B34). Tärningarna i 3.11 ändras enligt B31.

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
- C-3: Krönikan visar ett kapitel i taget, äldst först, i en läsyta som fyller
  resten av skärmen (B10, B14).
- C-4: En innehållsförteckning (meny) listar alla kapitel, markerar det aktuella
  och länkar till varje kapitel.
- C-5: "Läs från början" leder till första kapitlet, "Senaste kapitlet" till det
  senaste, som är tydligt markerat.
- C-6: Varje kapitel har en egen adress som kan delas, t.ex. `/chronicle?sida=7`.
- C-6a: Sidnavigering längst ner, alltid synlig, och tangenterna ← → (B14).
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
- H-6: GM kan skapa NPC:er (F3) med namn, porträtt och en anteckning som bara
  GM ser (B15).
- H-7: En NPC kan vara dold för spelarna och visas senare. Dold NPC skriver i
  chatten under ett alias med neutral siluett (B16).
- H-8: GM kan arkivera NPC:er så att de inte syns i "Skriv som" (B17).
- H-9: Porträtt väljs ur porträttbiblioteket, inte laddas upp (B19).

### 3.10a Porträttbibliotek

- PB-1: Administratörer och managers laddar upp porträtt. Bilden bearbetas som
  tidigare: beskärs, skalas om, metadata tas bort och sparas som WebP.
- PB-2: Varje porträtt har taggar och ett valfritt fält för källa och licens.
  Taggar skrivs med små bokstäver, och redan använda taggar föreslås.
- PB-3: Porträtt kan få ändrade taggar och tas bort. Karaktärer som använde ett
  borttaget porträtt får initialer (B20).
- PB-4: Väljaren visar porträtten i ett rutnät och söker på en eller flera
  taggar (porträtt som har alla angivna taggar).

### 3.11 Tärningskast

- T-1: Stöd för d4, d6, d8, d10, d12, d20 och d100.
- T-2: Syntax `NdX`, `NdX+M` och `NdX-M`, t.ex. `1d20`, `2d6+3`, `1d20-1`.
- T-3: Man slår genom att skriva `/roll 2d6+3` i OOC-chatten,
  eller via en tärningsknapp där man väljer tärning, antal och modifierare (F13).
  Text efter notationen blir en valfri beskrivning, t.ex. `/roll 1d20+5 attack`
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

### 3.14 Trådar (fas 6)

> **Ändrat i fas 8 (B37):** trådtyperna, introduktionen och krönikan per tråd är borttagna. En tråd har
> titel och status; en krönika är en vanlig tråd. Kraven nedan gäller med de ändringar som står i
> kursiv stil.

Målbilden för kampanjens innehåll efter fas 6 (före B37):

```text
Campaign
├── Threads
│   ├── Story        Introduction · Posts · Chronicle (när den är Completed)
│   └── Discussion   Introduction · Posts
├── Characters
└── Players
```

En Story-tråd är en del av berättelsen, t.ex. ett kapitel eller en scen. En Discussion-tråd är allt
utanför berättelsen: OOC, regelfrågor, planering, karaktärsskapande osv.

**Trådlistan (fliken Threads)**

- TR-1: Listan visar kampanjens alla trådar som kompakta textrader: titel, typ, status, antal inlägg,
  antal deltagare, senaste inlägg (vem och när) och olästa (B27). Inga bilder eller stora ikoner.
- TR-2: Aktiva trådar ligger före avslutade, och avslutade trådar ska vara lätta att nå. GM ändrar
  ordningen med ↑/↓.
- TR-3: GM skapar trådar med **+ New thread**: titel, typ (Story/Discussion) och introduktion i
  Markdown-editorn (B25). *Efter B37: bara titel.*
- TR-4: GM kan ändra titel, introduktion och status. *Efter B37: titel och status.*

Exempel:

```text
Active
  Chapter 4 – The ruins            Story        14 posts · 3 participants
  ● 3 new posts                                 Latest: Isak · 2 hours ago
  OOC                              Discussion

Completed
  Chapter 3 – Through the forest   Story
  Chapter 2 – The inn              Story
  Chapter 1 – Arrival              Story
```

**Trådsidan**

- TR-5: Överst titel, typ, status, antal inlägg och deltagare, och därunder introduktionen, eller
  krönikan om tråden är avslutad (B28, B33). *Efter B37: titel och status, ingen introduktion eller krönika.*
- TR-6: Inläggen visas 25 per sida med sidnavigering längst ner: Previous (avstängd på första sidan),
  sidnummer med … (t.ex. `1 2 3 … 12 13 14`), Next (avstängd på sista sidan) och Latest.
- TR-7: Tråden öppnas vid första olästa inlägget. Det finns genvägar till första olästa och senaste
  inlägget. Ingen automatisk scroll.
- TR-8: Nya inlägg läggs till direkt för den som står på sista sidan; andra ser "N new posts – go to latest".
- TR-9: Inläggen behåller dagens kompakta stil: avatar, namn, karaktär, GM-märkning och tid, utan stora
  kort per inlägg.
- TR-10: Skrivfältet ligger under sista sidan och knappen heter Post. I Story-trådar väljer man vem man
  skriver som (B29). *Efter B37: "Post as" i alla trådar, med senaste valet förvalt.*
- TR-11: Reply, Quote, Edit och Delete per inlägg, med fast länk till varje inlägg (B30).
- TR-12: Borttagna inlägg visas som "This post was deleted." på sin plats.
- TR-22 (B37): Namn och porträtt på en karaktär eller NPC i ett inlägg länkar till karaktärens sida, utom en
  dold NPC för spelarna.

**Livscykel för en Story-tråd** *(ersatt av B37: GM avslutar med en knapp och en bekräftelse; ingen krönika)*

```text
Create → Active → inlägg skrivs → GM sätter Completed → krönikan skrivs → Completed
```

- TR-13: När GM avslutar en Story-tråd visas ett steg för att skriva krönikan (kan hoppas över) (B32).
- TR-14: En avslutad tråd är skrivskyddad för spelarna. GM kan öppna den igen.
- TR-15: Krönikan är ett enda dokument per Story-tråd, högst 5 000 tecken, och kan redigeras av GM även
  efter att tråden avslutats. Den visar "Last edited by …" (B33).
- TR-16: Originalinläggen finns kvar; krönikan är en sammanfattning, inte en ersättning.

**Tärningar (B31)**

*TR-17–TR-20 ändras av B42: slagen läggs till som rader under skrivfältet och visas som en lista under inlägget; `/roll` är borttaget.*

- TR-17: `[dice]1d20+3[/dice]` i ett inlägg slås på servern när inlägget publiceras och visas på taggens
  plats med enskilda tärningar, modifierare och total. Naturlig 20 och 1 på d20 markeras som i dag.
- TR-18: Förhandsgranskningen visar "rolls when posted".
- TR-19: Slag kan inte ändras eller tas bort genom redigering, och nya taggar i en redigering slås inte.
  Inlägg med slag kan bara tas bort av GM.
- TR-20: Högst 10 slag per inlägg. Knappen 🎲 och tärningspanelen lägger in taggen; `/roll 1d20` är en
  genväg för ett inlägg med bara ett slag.

**Nya spelare** *(efter B37 löses detta med en vanlig Chronicle-tråd)*

- TR-21: En ny spelare ska inte behöva läsa hundratals gamla inlägg. De avslutade Story-trådarna visar sina
  krönikor överst ("Previously on …"), så att man kan läsa dem och sedan gå in i den aktiva tråden.

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
| Se dolda NPC:er och GM-anteckningar | Nej | Nej | Nej | Ja |

¹ Om kampanjen inte är privat (F1).

Roller för hela sajten (B18), oberoende av kampanjroll:

| Funktion | Manager | Administratör |
|---|:-:|:-:|
| Hantera porträttbiblioteket | Ja | Ja |
| Dela ut och ta bort roller | Nej | Ja |

Arkiverade och stängda kampanjer är skrivskyddade för alla utom GM (F12).

Efter fas 6 (B25–B33) ersätts raderna om RPG, OOC och krönika av:

| Funktion | Användare (ej deltagare) | Ansökande | Spelare | GM |
|---|:-:|:-:|:-:|:-:|
| Läsa trådar | Ja¹ | Ja¹ | Ja | Ja |
| Skriva i aktiva trådar och slå tärningar | Nej | Nej | Ja | Ja |
| Skriva i avslutade trådar | Nej | Nej | Nej | Ja |
| Redigera och ta bort egna inlägg | Nej | Nej | Ja² | Ja |
| Redigera och ta bort andras inlägg | Nej | Nej | Nej | Ja |
| Skapa, redigera, ordna och avsluta trådar | Nej | Nej | Nej | Ja |

² Inlägg med tärningsslag kan bara tas bort av GM (B31).

Alla behörighetskontroller görs på servern.

---

## 5. MVP och faser

Status: ✅ klart · ⏳ delvis · (tomt) inte påbörjat.

### Fas 1 – Grund
1. ✅ Konto: registrering, inloggning, profil (e-post och lösenord).
2. ✅ Kampanjer: skapa, lista, status och redigering (se punkt 11).
3. ✅ Ansökningsflöde med meddelande och GM-godkännande.

### Fas 2 – Spelrummet
4. ✅ Spelrum med flikar.
5. ✅ RPG och OOC som chatt enligt B6–B9: en kanal av varje per kampanj,
   gruppering och datumavdelare, 7 dagar/min 20/max 100, automatisk laddning
   uppåt, "Nya inlägg ↓", Enter/Ctrl+Enter och liveuppdatering.
6. ✅ Tärningar i OOC: `/slå` och `/roll` med valfri beskrivning, tärningspanel,
   kast på servern och visning i chatten.
7. ✅ Krönika som bok: innehållsförteckning, ett kapitel per sida (se punkt 12), "Läs från början"
   och "Senaste kapitlet", länkbara kapitel, GM skriver, redigerar, flyttar och
   tar bort kapitel.
8. ✅ Karaktärer med bild och textdokument, NPC:er för GM, val av karaktär
   ("Skriv som") i RPG-chatten och karaktärerna under fliken Spelare.

### Fas 3 – Komplettering
9. ✅ Redigering av egna inlägg (B12): "(redigerad)", liveuppdatering och sparad historik.
10. ✅ Olästmarkeringar: antal på flikarna RPG/OOC och i kampanjlistan, "Nya inlägg" i chatten.
11. ✅ Kampanjadministration för GM (A-2 till A-4): inställningar, status, ta bort spelare och radera kampanjen.

### Fas 4 – Förbättringar efter test
12. ✅ Krönikan (B14): ett kapitel per sida i en läsyta som fyller skärmen, innehåll som meny,
    klassisk sidnavigering längst ner och ← →.
13. ✅ NPC:er med namn, bild och GM-anteckning (B15). Bilden laddas upp tills porträttbiblioteket finns (punkt 18).
14. ✅ Chatten fyller skärmen (B13): inläggen scrollar i en egen yta, rubrik, flikar och skrivfält står kvar
    (även på mobil).
15. ✅ Sökbar NPC-väljare med senast använda och arkiverade NPC:er (B17): de fem senast använda överst,
    sedan alla i bokstavsordning; sökruta när det finns sex eller fler. GM arkiverar och återställer
    på NPC:ns sida; arkiverade ligger i en hopfälld del under Karaktärer.
16. ✅ Dolda NPC:er med alias (B16): kryssruta och alias på NPC:ns redigeringssida. Inläggen läses per
    användare, så riktigt namn och porträtt skickas bara till GM.
17. ✅ Roller för hela sajten: Administratör och Manager (B18). Rollerna ligger i Identitys rolltabeller och
    delas ut på /admin/roles. E-postadresser i `Admin:Emails` räknas alltid som administratörer när adressen
    är bekräftad; de tas bort i konfigurationen, inte på sidan. En administratör kan inte ta bort sin egen
    administratörsroll.
18. ✅ Porträttbibliotek med taggar och väljare (B19, B20): sidan Porträtt för administratörer och managers
    (ladda upp, tagga, ändra, ta bort) och en väljare på karaktärens redigeringssida. Varje sökord matchar
    början av en tagg. Porträtt som tas bort ger initialer; porträtt ligger kvar när en kampanj raderas.

### Fas 5 – Engelska och städning
19. ✅ Chattinlägg högst 5 000 tecken (B23). Databaskolumnen rymmer fortfarande 10 000, så äldre inlägg ligger kvar.
20. ✅ Gränssnittet på engelska, `/roll` som tärningskommando (B24). Datum visas på engelska men i svensk tid.
    Kodkommentarer och interna loggmeddelanden är fortfarande på svenska.
21. ✅ Städad testdata: e2e-testerna städar efter sig (alla konton på `@exempel.se`), och exempelkampanjen
    *The Mists of Harrowmere* (GM Gunnar, spelare Freja och Leif, konton på `@taleshaven.test`) ersätter testkampanjerna.
22. ✅ Dra tillbaka ansökan (B22): "Withdraw application" under Spelare, med ett bekräftelsesteg. Ansökan
    sparas som historik med status *Withdrawn*, försvinner ur GM:s lista, och man kan ansöka igen.
23. ✅ Borttagning av användarkonto med anonymiserade inlägg (B21): Account › Personal data › Delete account,
    med lösenord. Användarraden blir en anonym "gravsten" (namn "Deleted user", ingen e-post, inget lösenord,
    låst), så att inlägg, karaktärer och krönikekapitel behåller sin författare. Inloggningar, passkeys, roller,
    medlemskap, ansökningar och läspositioner raderas. Den som är GM måste först radera sina kampanjer.

### Fas 6 – Trådar (B25–B34)
Förslaget kom från `taleshaven-thread-system-todo.md` (2026-09-27) och besluten togs samma dag.
Ordningen gör att sajten fungerar mellan stegen.

24. ✅ Datamodell: trådar med typ, status, introduktion, krönika och position; inlägg med flera tärningsslag,
    mjuk borttagning och svarsreferens. Typerna heter Story/Discussion (tidigare Rpg/Ooc, samma värden) och
    statusarna Active/Completed (tidigare Open/Locked); trådens beskrivning bytte namn till introduktion.
    Regler för att avsluta och öppna trådar, krönika, borttagning och vem som får ta bort (GM allt, spelare
    egna inlägg utan slag) finns i Core med tester. GM kan nu redigera alla inlägg (B30). Gränssnittet är
    oförändrat tills punkt 25–27; gamla chattar och krönikekapitel tas bort i punkt 32 (B34).
25. ✅ Flikarna Threads · Characters · Players och trådlistan (B26, B27): kompakta rader med typ, utdrag ur
    introduktionen (eller krönikan), antal inlägg och deltagare, senaste inlägg med avatar och relativ tid,
    olästmarkering med antal. Aktiva först, avslutade under Completed (senaste kapitlet överst).
26. ✅ Skapa och redigera trådar (GM): titel, typ och introduktion; ordning med ↑/↓ i trådlistan (B25).
    En ny kampanj har inga trådar.
27. ✅ Trådsidan (B28, B29): introduktion eller krönika överst, 25 inlägg per sida, sidnavigering med
    ← Previous, sidnummer med …, Next →, Latest och tangenterna ← →. Skrivfältet ("Post", Ctrl+Enter) under
    sista sidan, "Post as" bara i Story-trådar. Nya inlägg läggs till direkt på sista sidan; andra ser
    "N new posts – go to latest". Ingen automatisk scroll.
28. ✅ Olästa per tråd: tråden öppnas vid första olästa med en linje "New posts", genvägar till första olästa
    och senaste, antal i trådlistan, på fliken Threads och i kampanjlistan. Borttagna inlägg räknas inte.
29. ✅ Reply ("Replying to …" med länk), Quote (citat med namn), Edit och Delete (mjuk: "This post was
    deleted.") och fasta länkar `?post=N` (B30).
30. ✅ Tärningar i texten med `[dice]…[/dice]`, tärningspanelen och `/roll` som genväg (B31). Slagen visas
    på sin plats; förhandsgranskningen visar "rolls when posted".
31. ✅ Avsluta en tråd, med krönika för Story-trådar; skriva och ändra krönikan i efterhand; öppna tråden
    igen (B32, B33).
32. ✅ Städning: chattarna, krönikefliken, `/ooc`- och `/chronicle`-adresserna, tabellen `ChronicleChapters`
    och kolumnen `Roll` är borttagna. Exempelkampanjen är omskapad i trådformatet: *Chapter 1 – Arrival in
    the fog* (avslutad, med krönika), *Chapter 2 – The empty boats* och *OOC*.

### Fas 7 – Trådsidans utseende
33. ✅ Trådsidan enligt B35: kort med rektangulära porträtt, relativ tid, ikonknappar och "…"-menyer,
    20 inlägg per sida och sidnavigering med Previous till vänster och Latest till höger.
34. ✅ B36: runda porträtt (56 px), maxbredd på inläggstexten, OOC-formatering med `[ooc]…[/ooc]`, knapparna
    Dice och OOC i verktygsraden; tärningspanelen ovanför skrivfältet är borttagen.
35. ✅ Ny exempelkampanj *The Salt Road* (GM Leif, spelare Gunnar och Freja): två kapitel (ett avslutat med krönika), OOC och
    House rules, 125 inlägg över fem veckor med tärningsslag och OOC-text, så att flera trådar har flera sidor.

### Fas 8 – Bara trådar (B37, B38)
36. ✅ Trådtyper, introduktion och krönika per tråd borttagna (B37), med migrering som tar bort kolumnerna. Trådlistan och
    trådsidan visar status; trådformuläret har bara titel. Krönikesidan och `/complete` är borttagna.
37. ✅ Complete thread är en knapp med bekräftelse i tråden; Reopen och Rename thread ligger i "…"-menyn.
38. ✅ "Post as" i alla trådar med det senaste valet i tråden förvalt (B37).
39. ✅ Namn och porträtt länkar till karaktärens sida, utom dolda NPC:er för spelarna (B37).
40. ✅ Alla kampanjer raderade och *The Salt Road* omskapad (B38): Chronicle-tråd, kapitel 1–2 avslutade, kapitel 3
    med en strid i cisternen, OOC och House rules, 162 inlägg.

### Fas 9 – Tärningslista, spoilers och trådfilter (B39–B43)
41. ✅ Trådens åtgärder i "…"-menyn, med bekräftelse för Complete thread under rubriken (B39).
42. ✅ Spoilers: hopfälld ruta och dold text mitt i en mening, med knapp i verktygsraden (B40).
43. ✅ Trådlistan: filtret Active/Completed med antal och olästa, 20 trådar per sida (B41).
44. ✅ Tärningsslag som lista med fördel och nackdel (B42). Slagen sparar läget (`Mode`) i inläggets jsonb; migreringen ger äldre
    slag läget Normal. `[dice]`-taggarna, Dice-knappen och `/roll` är borttagna.
45. ✅ Ny exempelkampanj *Lanterns of Greywater* (B43): 7 trådar (3 avslutade), 135 inlägg.

### Fas 10 – Kampanjlistan, taggar och standardtärning (B44–B49)
46. ✅ Reply som ikon i inläggets huvud; Quote, Edit, Copy link och Delete i "…"-menyn (B44).
47. ✅ Standardtärning per kampanj som fylls i av Add roll (B46). Kolumnen `DefaultRoll`, standard `1d20`.
48. ✅ Taggar på kampanjer (B47), som `text[]`-kolumnen `Tags`. Taggreglerna är gemensamma med porträtten (`TagList`).
49. ✅ Kompakt kampanjlista med My campaigns överst och 25 kampanjer per sida (B48), och filter på namn och taggar (B49).

### Fas 11 – Profilbild och profilsida (B50–B55)
50. ✅ Profilbild ur porträttbiblioteket under Account › Profile, med `#profile` förvalt i sökningen (B50). Kolumnen
    `PortraitId` på användaren (nollställs när porträttet tas bort). Bilden ligger i inloggningskakan, så menyn visar
    den utan databasanrop; kakan förnyas när bilden sparas.
51. ✅ 20 bilder per sida i profilbildsväljaren, med sidnavigering som behåller sökningen (B51).
52. ✅ Profilsida med About me, Member since, antal inlägg och kampanjer, och länkar dit från namn och bilder (B52).
    Kolumnerna `About` och `CreatedAt` på användaren.
53. ✅ 168 taggade profilbilder i biblioteket och 40 bilder per sida i väljaren (B53).
54. ✅ Profilbilden först på profilsidan och sparas med ett klick; egen rubrik och knapp för namn och About me (B54).
55. ✅ "Post as" visas alltid, med länk till att skapa en NPC eller karaktär när man inte har någon (B55).

### Fas 12 – Karaktärens status (B56–B60)
56. ✅ Räknare, tillstånd och sparade slag på karaktären, som jsonb-kolumner på `Characters` (B56).
57. ✅ Den valda karaktärens sparade slag som snabbval i skrivfältet (B57).
58. ✅ Ikoner för räknare, tillstånd och slag ur biblioteket (taggen `icon`), med automatiskt förslag efter namnet som
    sparas och kan bytas (B58). 96 ikoner och 77 monster utklippta och taggade.
59. ✅ Förhandsvisning av https-bildlänkar i inlägg, bara bild, begränsad storlek och inga skript (B59).
60. ✅ Slagens ikoner i postade inlägg (B60). 48 allmänna ikoner (HP, tillstånd m.m.) utklippta och taggade.
    Dessutom 56 dvärgar och 56 alver (taggarna `dwarf`/`elf`, `character`, man/woman, hårfärg m.m.) som
    karaktärsporträtt.

### Fas 13 – Bildbiblioteket (B61–B64)
61. ✅ Typ (Portrait/Icon) och importnyckel på bilderna; taggarna `icon` och `profile` görs om och tas bort (B61).
62. ✅ Samma porträtt i profil- och karaktärsväljaren, med kategoriknappar (B62).
63. ✅ Typ vid uppladdning, typfilter och sidan *Image library* (B63).
64. ✅ Taggstandard, källark i `assets/sources/` med versionshanterade taggfiler och kommandot `images` för
    förhandsvisning och import (B64). De 510 befintliga bilderna fick importnycklar; 501 kommer från tio ark.

### Fas 14 – Manifest och inkorg (B65)
65. ✅ `media/images/`, manifestet `media/images/manifest.csv` som skrivs efter varje ändring och används för att fylla i en
    ny databas, inkorgen `assets/new_images/` (utvecklingsläge), kontrollsumma i stället för importnyckel och `images cut`
    för källark (B65).

### Senare
Social inloggning, privata meddelanden, notiser/e-postnotiser, privata
tärningskast, reaktioner, uppladdade bilder i inlägg, sökning, bokmärken, export, dolda
scener, mer avancerad tärningssyntax (t.ex. `4d6kh3`), PWA.

Möjliga förbättringar att ta ställning till senare:
- ~~"Story so far"~~: löses av en vanlig Chronicle-tråd (B37).
- **Spelare skapar egna trådar** (B25 säger bara GM i första versionen).
- **Egen berättarbild per kampanj:** GM väljer en bild för berättarens inlägg, skild från sin profilbild (B50 använder profilbilden).
- **Förhandsvisning av karaktären** när man håller över namnet eller porträttet i en tråd (B37 börjar med en länk).
- **Privata tärningskast:** GM slår dolt. Kan byggas ovanpå tärningarna i texten (B31).
- **Export av kampanj:** behövs inte nu; säkerhetskopior av databasen räcker.
- ~~Flera RPG-kanaler~~: löses av trådarna i fas 6.

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
Character ── Portrait? (från biblioteket, B19)
User ──< UserRole (Administratör, Manager)
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
| Character | Id, CampaignId, ägare, namn, PortraitId?, dokument, extern länk, regelsystem, IsNpc, skapad, ändrad. NPC:er dessutom: GM-anteckning, dold, alias, arkiverad (B15–B17). |
| Portrait | Id, bildnyckel, taggar (`text[]` med GIN-index), källa och licens, uppladdad av, skapad (B19) |
| ChronicleChapter | Id, CampaignId, nummer, titel, innehåll, författare, skapad, ändrad |
| DiceRoll | Notation, beskrivning, antal, sidor, modifierare, resultat, total. Lagras som jsonb-kolumnen `Roll` på inlägget; användare och tidpunkt kommer från inlägget. |
| ReadMarker | UserId, ThreadId, LastReadPostId |

RPG- och OOC-chatten är var sin kanal i tabellen `Threads` (typ `Rpg` resp.
`Ooc`), en av varje per kampanj. De delar därför logik för inlägg, laddning,
liveuppdatering och olästmarkering. Fler RPG-kanaler kräver ingen ändring av
datamodellen.

**Efter fas 6:**

```text
Campaign ──< Thread (status: Active | Completed) ──< Post
Post ──< DiceRoll (0–10 slag, refereras från texten som [dice:N])
Post ── ReplyTo? (Post)
User ──< ReadMarker >── Thread
```

| Entitet | Viktiga fält |
|---|---|
| Thread | Id, CampaignId, titel, status, position, skapare, skapad, ändrad |
| Post | Id, ThreadId, författare, CharacterId?, innehåll, tärningsslag (jsonb-lista), svar på (PostId?), skapad, redigerad, borttagen (tid och av vem) |

`ChronicleChapter` och kolumnen `Roll` (ett slag per inlägg) är borttagna (B34). Trådarnas typ, introduktion och krönika
är borttagna (B37).

---

## 7. Teknisk arkitektur

| Område | Val |
|---|---|
| Ramverk | .NET 10, Blazor Web App |
| Rendering | Interactive Server för chattarna. Statisk SSR för övriga sidor där det räcker. |
| Autentisering | ASP.NET Core Identity (e-post/lösenord), externa leverantörer senare |
| Databas | PostgreSQL via Entity Framework Core (Npgsql). Lokalt i Docker (`postgres:18`). |
| Formaterad text | Markdown, renderat med Markdig och sanerat med HtmlSanitizer |
| Bilder | SkiaSharp: beskärs, skalas om, EXIF rensas och kodas om till WebP. Lagras på disk (`App_Data/media`) och serveras för inloggade. Porträtten ligger i ett gemensamt bibliotek (B19). Drift på Linux kräver paketet SkiaSharp.NativeAssets.Linux. |
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
- **Bilduppladdning:** bara administratörer och managers laddar upp (B19).
  Kontroll av typ och storlek, omkodning och slumpade filnamn. Bilderna serveras
  aldrig som körbart innehåll.
- **Dolda NPC:er:** riktigt namn, porträtt och GM-anteckning skickas aldrig till
  spelarnas webbläsare, inte ens i chattens data (B16).
- **Tärningskast:** genereras och sparas på servern och kan inte ändras eller
  tas bort. I fas 6 slås de när inlägget publiceras och kan inte ändras eller tas
  bort genom redigering; bara GM kan ta bort ett inlägg med slag (B31).
- **Redigering och borttagning:** inlägg markeras som redigerade och historiken
  sparas. Borttagning av innehåll görs som mjuk radering.

---

## 9. Öppna frågor

Inga just nu. Frågorna om trådsystemet (fas 6) är besvarade i B25–B34.

Tidigare frågor är besvarade (2026-09-27):

- Flera RPG-kanaler och privata tärningskast: väntar, se "Möjliga förbättringar" i avsnitt 5.
- Spelare föreslår krönikekapitel: nej, bara GM skriver krönikan.
- Borttaget konto: inläggen anonymiseras (B21).
- Export av kampanj: behövs inte nu.
- Dra tillbaka ansökan: ja (B22).
