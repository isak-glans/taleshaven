const DRAFT_PREFIX = 'taleshaven:draft:';

export function init(container, draftKey, sendOnEnter) {
    const textarea = container.querySelector('textarea');
    const counter = container.querySelector('[data-md-counter]');
    const updateCounter = () => { if (counter) counter.textContent = textarea.value.length; };

    // På pekskärmar ger Enter alltid ny rad; man skickar med knappen.
    const isTouch = window.matchMedia('(pointer: coarse)').matches;
    const hint = container.querySelector('[data-md-hint]');
    if (isTouch && hint) hint.textContent = 'Tap the button to send';

    // Blazor lyssnar på "change" (@bind), så ändringar gjorda från JS måste meddelas.
    const notifyBlazor = () => {
        textarea.dispatchEvent(new Event('change', { bubbles: true }));
        updateCounter();
    };

    if (!textarea.value) {
        const draft = readDraft(draftKey);
        if (draft) {
            textarea.value = draft;
            notifyBlazor();
        }
    }

    let saveTimer;
    textarea.addEventListener('input', () => {
        updateCounter();
        clearTimeout(saveTimer);
        saveTimer = setTimeout(() => saveDraft(draftKey, textarea.value), 400);
    });

    textarea.addEventListener('keydown', event => {
        if (event.key !== 'Enter' || event.isComposing) return;

        const modifier = event.ctrlKey || event.metaKey;
        const plainEnter = !event.shiftKey && !event.altKey && !modifier;
        const send = modifier || (sendOnEnter && !isTouch && plainEnter);
        if (!send) return;

        event.preventDefault();
        notifyBlazor();
        container.querySelector('[data-md-submit]')?.click();
    });

    for (const button of container.querySelectorAll('[data-md]')) {
        button.addEventListener('click', () => {
            applyFormat(textarea, button.dataset.md);
            notifyBlazor();
            saveDraft(draftKey, textarea.value);
            textarea.focus();
        });
    }

    updateCounter();
}

// Lägger in text vid markören och returnerar hela texten, så att Blazor får samma värde direkt.
export function insert(container, draftKey, insertion) {
    const textarea = container.querySelector('textarea');
    if (!textarea) return insertion;

    const start = textarea.selectionStart ?? textarea.value.length;
    const end = textarea.selectionEnd ?? start;
    textarea.setRangeText(insertion, start, end, 'end');
    textarea.dispatchEvent(new Event('change', { bubbles: true }));
    const counter = container.querySelector('[data-md-counter]');
    if (counter) counter.textContent = textarea.value.length;
    saveDraft(draftKey, textarea.value);
    textarea.focus();
    return textarea.value;
}

export function focus(container) {
    container.querySelector('textarea')?.focus();
}

// En tom nyckel betyder att inga utkast sparas (t.ex. vid redigering av ett befintligt inlägg).
export function clearDraft(draftKey) {
    if (!draftKey) return;
    try { localStorage.removeItem(DRAFT_PREFIX + draftKey); } catch { /* localStorage kan vara blockerat */ }
}

function readDraft(draftKey) {
    if (!draftKey) return null;
    try { return localStorage.getItem(DRAFT_PREFIX + draftKey); } catch { return null; }
}

function saveDraft(draftKey, value) {
    if (!draftKey) return;
    try {
        if (value) localStorage.setItem(DRAFT_PREFIX + draftKey, value);
        else localStorage.removeItem(DRAFT_PREFIX + draftKey);
    } catch { /* localStorage kan vara fullt eller blockerat */ }
}

function applyFormat(textarea, action) {
    const { selectionStart: start, selectionEnd: end, value } = textarea;
    const selected = value.slice(start, end);

    const wrap = (marker, placeholder) => {
        const inner = selected || placeholder;
        replace(textarea, start, end, marker + inner + marker, start + marker.length, start + marker.length + inner.length);
    };

    // Taggar runt markeringen (eller en platshållare som blir markerad), t.ex. [dice]1d20[/dice].
    const wrapTags = (open, close, placeholder) => {
        const inner = selected || placeholder;
        replace(textarea, start, end, open + inner + close, start + open.length, start + open.length + inner.length);
    };

    const prefixLines = prefixFor => {
        const lineStart = value.lastIndexOf('\n', start - 1) + 1;
        const result = value.slice(lineStart, end).split('\n').map((line, i) => prefixFor(i) + line).join('\n');
        replace(textarea, lineStart, end, result, lineStart + result.length, lineStart + result.length);
    };

    switch (action) {
        case 'bold': wrap('**', 'bold text'); break;
        case 'italic': wrap('*', 'italic text'); break;
        case 'dice': wrapTags('[dice]', '[/dice]', '1d20'); break;
        case 'ooc': wrapTags('[ooc]', '[/ooc]', 'out of character'); break;
        case 'heading': prefixLines(() => '### '); break;
        case 'ul': prefixLines(() => '- '); break;
        case 'ol': prefixLines(i => `${i + 1}. `); break;
        case 'quote': prefixLines(() => '> '); break;
        case 'link': {
            const label = selected || 'link text';
            const urlStart = start + label.length + 3;
            replace(textarea, start, end, `[${label}](https://)`, urlStart, urlStart + 'https://'.length);
            break;
        }
    }
}

function replace(textarea, start, end, text, selectionStart, selectionEnd) {
    textarea.setRangeText(text, start, end);
    textarea.setSelectionRange(selectionStart, selectionEnd);
}
