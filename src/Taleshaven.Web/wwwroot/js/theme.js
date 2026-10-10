// Ljust, mörkt eller som datorn (B75). Laddas i <head> innan sidan ritas, så att den inte blinkar till i fel läge.
// Valet sparas i webbläsaren; "auto" följer operativsystemets inställning.
(function () {
    const key = "taleshaven-theme";
    const media = window.matchMedia("(prefers-color-scheme: dark)");

    function choice() {
        try {
            return localStorage.getItem(key) || "auto";
        } catch {
            return "auto";
        }
    }

    function apply() {
        const selected = choice();
        const dark = selected === "dark" || (selected === "auto" && media.matches);
        const root = document.documentElement;
        root.setAttribute("data-bs-theme", dark ? "dark" : "light");
        root.setAttribute("data-theme-choice", selected);
    }

    apply();
    media.addEventListener("change", apply);

    // Blazors förbättrade navigering skriver över <html>-attributen med serverns; sätt tillbaka läget.
    new MutationObserver(() => {
        const root = document.documentElement;
        if (!root.hasAttribute("data-bs-theme") || !root.hasAttribute("data-theme-choice")) apply();
    }).observe(document.documentElement, { attributes: true, attributeFilter: ["data-bs-theme", "data-theme-choice"] });

    document.addEventListener("click", (event) => {
        const button = event.target.closest("[data-theme-choice-set]");
        if (button) {
            try {
                localStorage.setItem(key, button.getAttribute("data-theme-choice-set"));
            } catch {
                // Utan lagring gäller valet bara den här sidan.
            }
            apply();
        }

        // Toppradens menyer (<details>) stängs när man klickar utanför dem eller väljer något i dem.
        for (const menu of document.querySelectorAll("details.header-menu[open]")) {
            if (!menu.contains(event.target) || event.target.closest("a, button:not([data-theme-choice-set])")) {
                menu.removeAttribute("open");
            }
        }
    });

    document.addEventListener("keydown", (event) => {
        if (event.key !== "Escape") return;
        for (const menu of document.querySelectorAll("details.header-menu[open]")) {
            menu.removeAttribute("open");
            menu.querySelector("summary")?.focus();
        }
    });
})();
