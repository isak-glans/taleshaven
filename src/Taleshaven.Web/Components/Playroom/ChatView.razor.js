// Chatten scrollar i sin egen inläggsyta (container), som fyller resten av skärmen (fill-viewport.js, B13).
// Rubrik och flikar ovanför och skrivfältet under står kvar. All scrollning sätts direkt (scrollTop), så att
// ingen animerad scroll hinner trigga inladdningen av äldre inlägg innan ytan når botten.

const TOP_MARGIN_PX = 400;

// Anropar LoadOlderAsync när toppen av inläggsytan närmar sig. Returnerar ett objekt med disconnect().
export function observeTop(sentinel, dotNetRef, container) {
    const observer = new IntersectionObserver(entries => {
        if (entries.some(entry => entry.isIntersecting)) {
            dotNetRef.invokeMethodAsync('LoadOlderAsync');
        }
    }, { root: container, rootMargin: `${TOP_MARGIN_PX}px 0px 0px 0px` });

    observer.observe(sentinel);
    return { disconnect: () => observer.disconnect() };
}

export function isNearTop(sentinel, container) {
    return !!sentinel && sentinel.getBoundingClientRect().bottom > container.getBoundingClientRect().top - TOP_MARGIN_PX;
}

export function isNearBottom(container) {
    return container.scrollTop + container.clientHeight >= container.scrollHeight - 150;
}

// Läspositionen vid inladdning av äldre inlägg hålls fast vid ett inlägg (inte ytans höjd),
// så att det fungerar oavsett om webbläsaren själv försöker justera scrollen.
export function anchorTop(elementId) {
    const element = document.getElementById(elementId);
    return element ? element.getBoundingClientRect().top : null;
}

export function restoreAnchor(container, elementId, previousTop) {
    const element = document.getElementById(elementId);
    if (element && previousTop !== null) {
        container.scrollTop += element.getBoundingClientRect().top - previousTop;
    }
}

// Scrollar så att elementet (t.ex. "Nya inlägg") hamnar en bit under inläggsytans överkant.
export function scrollToElement(container, elementId) {
    const element = document.getElementById(elementId);
    if (element) {
        container.scrollTop += element.getBoundingClientRect().top - container.getBoundingClientRect().top - 16;
    }
}

export function scrollToBottom(container) {
    container.scrollTop = container.scrollHeight;
}
