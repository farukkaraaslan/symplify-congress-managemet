(function () {
  "use strict";

  var root = document.documentElement;

  var allowedThemes = ["light", "dark", "system"];
  var allowedDirections = ["ltr", "rtl"];
  var allowedColors = ["blue", "red", "green", "yellow", "orange", "cyan", "violet"];
  var allowedFonts = ["manrope", "inter", "plus-jakarta-sans", "dm-sans", "segoe-ui", "arial", "tahoma", "verdana"];
  var allowedSizes = ["compact", "small", "standard", "large", "xlarge"];

  function read(key) {
    try { return localStorage.getItem(key); } catch (_error) { return null; }
  }

  function normalize(value, allowed, fallback) {
    if (!value) return fallback;
    var key = String(value).toLowerCase().trim();
    return allowed.indexOf(key) !== -1 ? key : fallback;
  }

  function resolveTheme(setting) {
    if (setting !== "system") return setting;
    try {
      return window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";
    } catch (_error) {
      return "light";
    }
  }

  var themeSetting = normalize(read("theme"), allowedThemes, "light");
  var direction = normalize(read("direction"), allowedDirections, "ltr");
  var color = normalize(read("theme-color") || read("templateColor"), allowedColors, "blue");
  var font = normalize(read("theme-font"), allowedFonts, "manrope");
  var size = normalize(read("theme-ui-size"), allowedSizes, "standard");

  if (color === "orange") color = "yellow";

  root.setAttribute("data-theme", resolveTheme(themeSetting));
  root.setAttribute("data-theme-setting", themeSetting);
  root.setAttribute("dir", direction);
  root.setAttribute("data-color", color);
  root.setAttribute("data-font", font);
  root.setAttribute("data-ui-size", size);

  // The late controller (app.js) uses the same state. Keeping it here makes
  // auth, error and standalone pages render correctly even before app.js runs.
  window.SymplifyThemeState = {
    theme: themeSetting,
    direction: direction,
    color: color,
    font: font,
    size: size
  };
})();
