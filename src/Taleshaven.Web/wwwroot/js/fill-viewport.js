// Element med data-fill-viewport får höjden som finns kvar på skärmen, så att innehållet scrollar inuti
// elementet medan rubrik och flikar ovanför står kvar. Element med data-fill-below (syskon efter
// elementet, t.ex. sidnavigering) räknas in så att de också syns utan att sidan scrollar.
// Laddas en gång i App.razor och körs igen efter Blazors förbättrade navigering.

const MIN_HEIGHT = 280;
const BOTTOM_GAP = 16;

function fit() {
    for (const element of document.querySelectorAll('[data-fill-viewport]')) {
        let below = 0;
        for (const sibling of element.parentElement?.querySelectorAll(':scope > [data-fill-below]') ?? []) {
            below += sibling.getBoundingClientRect().height;
        }

        const top = element.getBoundingClientRect().top + window.scrollY;
        const available = window.innerHeight - top - below - BOTTOM_GAP;
        element.style.height = `${Math.max(MIN_HEIGHT, Math.floor(available))}px`;
    }
}

// Vid byte av sida (t.ex. nästa kapitel) ska läsningen börja överst, inte där förra sidan slutade.
function resetScroll() {
    for (const element of document.querySelectorAll('[data-fill-viewport]')) {
        element.scrollTop = 0;
    }
}

let resizeTimer;
window.addEventListener('resize', () => {
    clearTimeout(resizeTimer);
    resizeTimer = setTimeout(fit, 50);
});

// Ihopfällbara delar ovanför (t.ex. "Om kampanjen") ändrar hur mycket plats som finns kvar.
document.addEventListener('toggle', fit, true);

Blazor.addEventListener('enhancedload', () => {
    resetScroll();
    fit();
});

window.addEventListener('load', fit); // Typsnitt och bilder kan ändra höjden ovanför.
fit();
