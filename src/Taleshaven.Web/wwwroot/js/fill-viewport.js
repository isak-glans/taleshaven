// Element med data-fill-viewport får höjden som finns kvar på skärmen, så att innehållet scrollar inuti
// elementet medan rubrik och flikar ovanför står kvar (krönikan B14, chatten B13). Element med data-fill-below
// (syskon efter elementet, t.ex. sidnavigering eller skrivfält) räknas in så att de också syns utan att sidan scrollar.
// Laddas en gång i App.razor och körs igen efter Blazors förbättrade navigering och när något ändrar storlek.

const MIN_HEIGHT = 200; // På mobil tar rubrik, flikar och skrivfält mycket plats; några inlägg ska ändå synas.
const BOTTOM_GAP = 16;

// På mobil krymper tangentbordet den synliga ytan; visualViewport visar hur mycket som faktiskt syns.
const viewportHeight = () => window.visualViewport?.height ?? window.innerHeight;

function fit() {
    for (const element of document.querySelectorAll('[data-fill-viewport]')) {
        let below = 0;
        for (const sibling of element.parentElement?.querySelectorAll(':scope > [data-fill-below]') ?? []) {
            below += sibling.getBoundingClientRect().height;
            watch(sibling);
        }

        // Den som läser längst ner (t.ex. senaste chattinlägget) ska stå kvar längst ner när höjden ändras.
        const wasAtBottom = element.scrollHeight > element.clientHeight
            && element.scrollTop + element.clientHeight >= element.scrollHeight - 4;

        const top = element.getBoundingClientRect().top + window.scrollY;
        const available = viewportHeight() - top - below - BOTTOM_GAP;
        element.style.height = `${Math.max(MIN_HEIGHT, Math.floor(available))}px`;

        if (wasAtBottom) {
            element.scrollTop = element.scrollHeight;
        }
    }
}

let scheduled = false;
function scheduleFit() {
    if (scheduled) return;
    scheduled = true;
    requestAnimationFrame(() => {
        scheduled = false;
        fit();
    });
}

// Skrivfält och sidnavigering ändrar höjd (t.ex. tärningspanelen öppnas eller ett felmeddelande visas).
const resizeObserver = new ResizeObserver(scheduleFit);
const watched = new WeakSet();
function watch(element) {
    if (watched.has(element)) return;
    watched.add(element);
    resizeObserver.observe(element);
}

// Vid byte av sida (t.ex. nästa kapitel) ska läsningen börja överst, inte där förra sidan slutade.
function resetScroll() {
    for (const element of document.querySelectorAll('[data-fill-viewport]')) {
        element.scrollTop = 0;
    }
}

// När en interaktiv komponent (chatten) startar byts den förrenderade HTML:en ut och höjden försvinner.
// Nya element passas in direkt, innan komponenten hinner scrolla till senaste inlägget.
new MutationObserver(() => {
    if (document.querySelector('[data-fill-viewport]:not([style*="height"])')) fit();
}).observe(document.body, { childList: true, subtree: true });

window.addEventListener('resize', scheduleFit);
window.visualViewport?.addEventListener('resize', scheduleFit);

// Ihopfällbara delar ovanför (t.ex. "Om kampanjen") ändrar hur mycket plats som finns kvar.
document.addEventListener('toggle', scheduleFit, true);

Blazor.addEventListener('enhancedload', () => {
    resetScroll();
    fit();
});

window.addEventListener('load', fit); // Typsnitt och bilder kan ändra höjden ovanför.
fit();
