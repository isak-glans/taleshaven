// Läspositionen vid "Ladda äldre inlägg" hålls fast vid ett inlägg (inte sidans höjd),
// så att det fungerar oavsett om webbläsaren själv försöker justera scrollen.
export function anchorTop(elementId) {
    const element = document.getElementById(elementId);
    return element ? element.getBoundingClientRect().top : null;
}

export function restoreAnchor(elementId, previousTop) {
    const element = document.getElementById(elementId);
    if (element && previousTop !== null) {
        window.scrollBy(0, element.getBoundingClientRect().top - previousTop);
    }
}

export function scrollToBottom() {
    window.scrollTo({ top: document.documentElement.scrollHeight });
}
