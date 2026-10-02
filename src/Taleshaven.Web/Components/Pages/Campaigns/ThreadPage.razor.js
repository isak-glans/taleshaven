// Trådsidan scrollar bara när läsaren bett om det: en fast länk till ett inlägg, första olästa, skrivfältet
// vid svar och citat, eller början av sidan vid sidbyte (B28). Ingen automatisk scroll när nya inlägg kommer in.

export function scrollToElement(id) {
    const element = document.getElementById(id);
    if (!element) return;
    const top = element.getBoundingClientRect().top + window.scrollY - 16;
    window.scrollTo({ top, behavior: 'instant' });
}

export function scrollToTop() {
    window.scrollTo({ top: 0, behavior: 'instant' });
}

// Kopierar en fast l?nk till ett inl?gg. Returnerar false om webbl?saren inte till?ter det.
export async function copyText(text) {
    try {
        await navigator.clipboard.writeText(text);
        return true;
    } catch {
        return false;
    }
}
