// Tangenterna ← → följer länkarna märkta data-page-prev och data-page-next (t.ex. bläddring i krönikan).
// Laddas en gång i App.razor; lyssnaren finns kvar vid Blazors förbättrade navigering mellan sidor.

// Medan ett sidbyte pågår ignoreras nya tryck. Annars kan ett snabbt andra tryck klicka på
// den gamla sidans länk och leda till samma sida igen.
let navigating = false;

Blazor.addEventListener('enhancedload', () => { navigating = false; });

document.addEventListener('keydown', event => {
    if (event.key !== 'ArrowLeft' && event.key !== 'ArrowRight') return;
    if (event.defaultPrevented || event.altKey || event.ctrlKey || event.metaKey || event.shiftKey) return;

    // Piltangenterna ska flytta markören när man skriver, inte byta sida.
    const target = event.target;
    if (target instanceof HTMLElement && (target.isContentEditable || target.closest('input, textarea, select'))) return;

    const link = document.querySelector(event.key === 'ArrowLeft' ? 'a[data-page-prev]' : 'a[data-page-next]');
    if (!link) return;

    event.preventDefault();
    if (navigating) return;

    navigating = true;
    setTimeout(() => { navigating = false; }, 3000); // Säkerhetsnät om navigeringen aldrig blir klar.
    link.click(); // Ett klick ger samma förbättrade navigering som när man klickar på pilen.
});
