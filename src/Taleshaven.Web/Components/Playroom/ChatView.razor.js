// Chatten scrollar med sidan (fönstret); skrivfältet ligger fast längst ner med position: sticky.
// All scrollning är 'instant': Bootstrap slår på mjuk scroll, och en animerad scroll hinner annars
// trigga inladdningen av äldre inlägg innan sidan når botten.

const TOP_MARGIN_PX = 400;

// Anropar LoadOlderAsync när toppen av chatten närmar sig skärmen. Returnerar ett objekt med disconnect().
export function observeTop(sentinel, dotNetRef) {
    const observer = new IntersectionObserver(entries => {
        if (entries.some(entry => entry.isIntersecting)) {
            dotNetRef.invokeMethodAsync('LoadOlderAsync');
        }
    }, { rootMargin: `${TOP_MARGIN_PX}px 0px 0px 0px` });

    observer.observe(sentinel);
    return { disconnect: () => observer.disconnect() };
}

export function isNearTop(sentinel) {
    return !!sentinel && sentinel.getBoundingClientRect().bottom > -TOP_MARGIN_PX;
}

export function isNearBottom() {
    return window.innerHeight + window.scrollY >= document.documentElement.scrollHeight - 150;
}

// Läspositionen vid inladdning av äldre inlägg hålls fast vid ett inlägg (inte sidans höjd),
// så att det fungerar oavsett om webbläsaren själv försöker justera scrollen.
export function anchorTop(elementId) {
    const element = document.getElementById(elementId);
    return element ? element.getBoundingClientRect().top : null;
}

export function restoreAnchor(elementId, previousTop) {
    const element = document.getElementById(elementId);
    if (element && previousTop !== null) {
        window.scrollBy({ top: element.getBoundingClientRect().top - previousTop, behavior: 'instant' });
    }
}

export function scrollToBottom() {
    window.scrollTo({ top: document.documentElement.scrollHeight, behavior: 'instant' });
}
