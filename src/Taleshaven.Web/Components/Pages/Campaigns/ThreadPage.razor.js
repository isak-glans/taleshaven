// Trådsidan scrollar bara när läsaren bett om det: en fast länk till ett inlägg, första olästa, skrivfältet
// vid svar och citat, eller början av sidan vid sidbyte (B28). Ingen automatisk scroll när nya inlägg kommer in.

const ROLLS_PREFIX = 'taleshaven:rolls:';

export function scrollToElement(id) {
    const element = document.getElementById(id);
    if (!element) return;
    const top = element.getBoundingClientRect().top + window.scrollY - 16;
    window.scrollTo({ top, behavior: 'instant' });
}

export function scrollToTop() {
    window.scrollTo({ top: 0, behavior: 'instant' });
}

// Kopierar en fast länk till ett inlägg. Returnerar false om webbläsaren inte tillåter det.
export async function copyText(text) {
    try {
        await navigator.clipboard.writeText(text);
        return true;
    } catch {
        return false;
    }
}

// Tärningsslag som lagts till men inte postats sparas som utkast, som texten (B42).
export function loadRolls(key) {
    try { return localStorage.getItem(ROLLS_PREFIX + key); } catch { return null; }
}

export function saveRolls(key, json) {
    try {
        if (json) localStorage.setItem(ROLLS_PREFIX + key, json);
        else localStorage.removeItem(ROLLS_PREFIX + key);
    } catch { /* localStorage kan vara fullt eller blockerat */ }
}

export function focusLastRoll() {
    const inputs = document.querySelectorAll('.roll-editor .roll-notation');
    inputs[inputs.length - 1]?.focus();
}
