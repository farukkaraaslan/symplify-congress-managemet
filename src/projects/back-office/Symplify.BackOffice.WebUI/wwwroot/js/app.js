window.wowdash = window.wowdash || {};
window.wowdash.getCssVar = function (name, el) {
  var target = el || document.documentElement;
  return getComputedStyle(target).getPropertyValue(name).trim();
};
window.wowdash.sanitizeThemeObject = function (input) {
  var tokens = {
    primary600: window.wowdash.getCssVar("--primary-600") || "#487FFF",
    brandRgb: window.wowdash.getCssVar("--brand-rgb") || "72, 127, 255",
  };

  function rgbaBrand(alpha) {
    return "rgba(" + tokens.brandRgb + ", " + alpha + ")";
  }

  function replaceString(value) {
    if (typeof value !== "string") return value;
    var next = value.trim();

    if (/^#487fff$/i.test(next)) {
      return tokens.primary600;
    }

    var hexAlphaMatch = next.match(/^#487fff([0-9a-f]{2})$/i);
    if (hexAlphaMatch) {
      var alphaInt = parseInt(hexAlphaMatch[1], 16);
      var alpha = Math.max(0, Math.min(1, alphaInt / 255));
      return rgbaBrand(alpha.toFixed(3).replace(/0+$/, "").replace(/\.$/, ""));
    }

    var rgbaMatch = next.match(
      /^rgba\(\s*72\s*,\s*127\s*,\s*255\s*,\s*([0-9.]+)\s*\)$/i
    );
    if (rgbaMatch) {
      return rgbaBrand(rgbaMatch[1]);
    }

    var rgbMatch = next.match(/^rgb\(\s*72\s*,\s*127\s*,\s*255\s*\)$/i);
    if (rgbMatch) {
      return "rgb(" + tokens.brandRgb + ")";
    }

    return next;
  }

  function walk(node) {
    if (!node) return;
    if (typeof node === "string") return replaceString(node);
    if (typeof node === "function") return node;

    if (Array.isArray(node)) {
      for (var i = 0; i < node.length; i++) {
        node[i] = walk(node[i]);
      }
      return node;
    }

    if (typeof node === "object") {
      for (var key in node) {
        if (!Object.prototype.hasOwnProperty.call(node, key)) continue;
        node[key] = walk(node[key]);
      }
      return node;
    }

    return node;
  }

  return walk(input);
};

(function ($) {
  "use strict";

  const DESKTOP_BREAKPOINT = 1200;

  const $window = $(window);
  const $document = $(document);
  const $body = $("body");
  const $html = $("html");
  const getSidebar = () => $(".sidebar");
  const getDashboardMain = () => $(".dashboard-main");
  const getBodyOverlay = () => $(".body-overlay");
  const getThemeCustomizationSidebar = () => $(".theme-customization-sidebar");
  const getSidebarScrollContainer = () => $(".sidebar-menu-area").first();
  const getSidebarMenuRoot = () => $("#sidebar-menu").first();

  (function () {
    const OriginalApexCharts = window.ApexCharts;
    if (typeof OriginalApexCharts !== "function") return;
    if (OriginalApexCharts.__wowdashPatched) return;

    function WrappedApexCharts(el, opts) {
      if (opts) window.wowdash.sanitizeThemeObject(opts);
      return new OriginalApexCharts(el, opts);
    }

    WrappedApexCharts.prototype = OriginalApexCharts.prototype;
    Object.setPrototypeOf(WrappedApexCharts, OriginalApexCharts);
    WrappedApexCharts.__wowdashPatched = true;
    window.ApexCharts = WrappedApexCharts;
  })();

  (function () {
    if (!$.fn || typeof $.fn.vectorMap !== "function") return;
    if ($.fn.vectorMap.__wowdashPatched) return;

    const originalVectorMap = $.fn.vectorMap;
    $.fn.vectorMap = function (options) {
      if (options) window.wowdash.sanitizeThemeObject(options);
      return originalVectorMap.call(this, options);
    };
    $.fn.vectorMap.__wowdashPatched = true;
  })();

  const SIDEBAR_STATE_KEY = "wowdash.sidebar.state.v1";
  let sidebarStateInitialized = false;
  let sidebarScrollSaveTimer = null;

  function isDesktop() {
    return window.innerWidth >= DESKTOP_BREAKPOINT;
  }

  function slugify(text) {
    return String(text || "")
      .toLowerCase()
      .trim()
      .replace(/[\s/]+/g, "-")
      .replace(/[^a-z0-9-]/g, "")
      .replace(/-+/g, "-")
      .replace(/^-|-$/g, "");
  }

  function ensureSidebarDropdownIds() {
    const $root = getSidebarMenuRoot();
    if (!$root.length) return;

    $root.find("li.dropdown").each(function (index) {
      if (this.dataset.sidebarDropdownId) return;

      const labelText = $(this)
        .children("a")
        .text()
        .replace(/\s+/g, " ")
        .trim();
      const slug = slugify(labelText) || "dropdown";
      this.dataset.sidebarDropdownId = `${slug}-${index}`;
    });
  }

  function getSavedSidebarState() {
    try {
      const raw = sessionStorage.getItem(SIDEBAR_STATE_KEY);
      if (!raw) return null;

      const state = JSON.parse(raw);
      if (!state || typeof state !== "object") return null;

      return {
        scrollTop: Number.isFinite(Number(state.scrollTop))
          ? Number(state.scrollTop)
          : null,
        openDropdownIds: Array.isArray(state.openDropdownIds)
          ? state.openDropdownIds.filter((x) => typeof x === "string")
          : [],
      };
    } catch (e) {
      return null;
    }
  }

  function collectSidebarOpenDropdownIds() {
    ensureSidebarDropdownIds();

    const $root = getSidebarMenuRoot();
    if (!$root.length) return [];

    const ids = [];
    $root.find("li.dropdown.open, li.dropdown.dropdown-open").each(function () {
      const id = this.dataset.sidebarDropdownId;
      if (id) ids.push(id);
    });

    return Array.from(new Set(ids));
  }

  function saveSidebarState() {
    const $scroll = getSidebarScrollContainer();
    if (!$scroll.length) return;

    const state = {
      scrollTop: $scroll.scrollTop(),
      openDropdownIds: collectSidebarOpenDropdownIds(),
      ts: Date.now(),
    };

    try {
      sessionStorage.setItem(SIDEBAR_STATE_KEY, JSON.stringify(state));
    } catch (e) {}
  }

  function applySidebarOpenState(openDropdownIds) {
    if (!Array.isArray(openDropdownIds) || openDropdownIds.length === 0) return;

    ensureSidebarDropdownIds();

    const $root = getSidebarMenuRoot();
    if (!$root.length) return;

    openDropdownIds.forEach((id) => {
      const $item = $root.find(
        `li.dropdown[data-sidebar-dropdown-id="${id}"]`
      );
      if (!$item.length) return;

      $item.addClass("dropdown-open open");
      $item.children(".sidebar-submenu").show();
    });
  }

  function restoreSidebarScroll(scrollTop) {
    const $scroll = getSidebarScrollContainer();
    if (!$scroll.length) return;
    if (!Number.isFinite(scrollTop) || scrollTop < 0) return;

    requestAnimationFrame(function () {
      $scroll.scrollTop(scrollTop);
    });
  }

  function ensureSidebarActiveLinkVisible() {
    const $scroll = getSidebarScrollContainer();
    const $root = getSidebarMenuRoot();
    if (!$scroll.length || !$root.length) return;

    const $active = $root.find("a.active-page").first();
    if (!$active.length) return;

    const scrollEl = $scroll.get(0);
    const activeEl = $active.get(0);
    if (!scrollEl || !activeEl) return;

    const scrollRect = scrollEl.getBoundingClientRect();
    const activeRect = activeEl.getBoundingClientRect();

    if (activeRect.top < scrollRect.top || activeRect.bottom > scrollRect.bottom) {
      activeEl.scrollIntoView({ block: "nearest" });
    }
  }

  function initSidebarStatePersistence() {
    if (sidebarStateInitialized) return;
    sidebarStateInitialized = true;

    $document.on("scroll", ".sidebar-menu-area", function () {
      if (sidebarScrollSaveTimer) clearTimeout(sidebarScrollSaveTimer);
      sidebarScrollSaveTimer = setTimeout(function () {
        saveSidebarState();
      }, 120);
    });

    $document.on("click", ".sidebar-menu a", function () {
      saveSidebarState();
    });

    window.addEventListener("beforeunload", function () {
      saveSidebarState();
    });
  }

  function runSidebarRestoreFlow() {
    initSidebarStatePersistence();

    const savedState = getSavedSidebarState();
    if (savedState) {
      applySidebarOpenState(savedState.openDropdownIds);
    }

    activateSidebarMenu();

    if (savedState) {
      restoreSidebarScroll(savedState.scrollTop);
    }

    requestAnimationFrame(function () {
      ensureSidebarActiveLinkVisible();
    });
  }

  function isMobileSidebarOpen() {
    return getSidebar().hasClass("sidebar-open");
  }

  function isThemeCustomizationOpen() {
    return getThemeCustomizationSidebar().hasClass("active");
  }

  function syncOverlayState() {
    const shouldShowOverlay = isMobileSidebarOpen() || isThemeCustomizationOpen();

    getBodyOverlay().toggleClass("show active", shouldShowOverlay);
    $body.toggleClass("overlay-active", shouldShowOverlay);
  }

  function openMobileSidebar() {
    if (isDesktop()) return;

    getSidebar().addClass("sidebar-open");
    $body.addClass("sidebar-overlay-open");
    syncOverlayState();
  }

  function closeMobileSidebar() {
    getSidebar().removeClass("sidebar-open");
    $body.removeClass("sidebar-overlay-open");
    syncOverlayState();
  }

  function toggleDesktopSidebar() {
    $(".sidebar-toggle").toggleClass("active");
    getSidebar().toggleClass("active");
    getDashboardMain().toggleClass("active");
  }

  function openThemeCustomization() {
    getThemeCustomizationSidebar().addClass("active");
    syncOverlayState();
  }

  function closeThemeCustomization() {
    getThemeCustomizationSidebar().removeClass("active");
    syncOverlayState();
  }

  function closeAllFloatingPanels() {
    closeMobileSidebar();
    closeThemeCustomization();
  }

  function handleResponsiveState() {
    if (isDesktop()) {
      closeMobileSidebar();
    } else {
      syncOverlayState();
    }
  }

  $document.on("click", ".sidebar-menu .dropdown > a", function (e) {
    const $item = $(this).parent(".dropdown");
    const $submenu = $item.children(".sidebar-submenu");

    if (!$submenu.length) return;

    e.preventDefault();

    $item
      .siblings(".dropdown")
      .removeClass("dropdown-open open")
      .children(".sidebar-submenu")
      .stop(true, true)
      .slideUp(200);

    $submenu.stop(true, true).slideToggle(200);
    $item.toggleClass("dropdown-open open");
    saveSidebarState();
  });

  $document.on("click", ".sidebar-menu a", function () {
    if (isDesktop()) return;
    if ($(this).siblings(".sidebar-submenu").length) return;

    closeMobileSidebar();
  });

  $document.on("click", ".sidebar-toggle", function () {
    toggleDesktopSidebar();
  });

  $document.on("click", ".sidebar-mobile-toggle", function () {
    openMobileSidebar();
  });

  $document.on("click", ".sidebar-close-btn", function () {
    closeMobileSidebar();
  });

  $document.on("click", ".theme-customization__button", function () {
    if (getThemeCustomizationSidebar().hasClass("active")) {
      closeThemeCustomization();
    } else {
      openThemeCustomization();
    }
  });

  $document.on("click", ".theme-customization-sidebar__close", function () {
    closeThemeCustomization();
  });

  $document.on("click", ".body-overlay", function () {
    closeAllFloatingPanels();
  });

  $document.on("pointerdown", function (e) {
    if (!isThemeCustomizationOpen()) return;

    const $target = $(e.target);
    if ($target.closest(".theme-customization-sidebar").length) return;
    if ($target.closest(".theme-customization__button").length) return;
    if ($target.closest(".sidebar").length) return;

    closeThemeCustomization();
  });

  $document.on("keydown", function (e) {
    if (e.key === "Escape") {
      closeAllFloatingPanels();
    }
  });

  $window.on("resize", function () {
    handleResponsiveState();
  });

  function activateSidebarMenu() {
    const currentUrl = window.location.href.split(/[?#]/)[0];
    let $activeItem = $("ul#sidebar-menu a")
      .filter(function () {
        return this.href.split(/[?#]/)[0] === currentUrl;
      })
      .first();

    if ($activeItem.length) {
      $activeItem.addClass("active-page").parent().addClass("active-page");

      let $parent = $activeItem.parent();
      while ($parent.length) {
        if ($parent.hasClass("sidebar-submenu")) {
          $parent.show().parent(".dropdown").addClass("dropdown-open open");
        }
        $parent = $parent.parent();
      }
    }
  }

  $(function () {
    runSidebarRestoreFlow();
  });

  document.addEventListener("layout:loaded", function () {
    runSidebarRestoreFlow();
    handleResponsiveState();
    syncOverlayState();
    applyTheme(currentThemeSetting);
  });

  function calculateSettingAsThemeString(localStorageTheme) {
    if (localStorageTheme !== null) {
      return localStorageTheme;
    }
    return "light";
  }

  function updateThemeToggleButton(isDark) {
    const button = document.querySelector("[data-theme-toggle]");
    if (!button) return;

    const icon = isDark ? "ri:moon-line" : "ri:sun-line";
    const label = isDark ? "dark" : "light";

    button.setAttribute("aria-label", label);
    button.setAttribute("title", label);
    button.innerHTML = `<iconify-icon icon="${icon}" class="text-primary-light text-xl"></iconify-icon>`;
  }

  function resolveTheme(theme) {
    if (theme === "system") {
      return window.matchMedia("(prefers-color-scheme: dark)").matches
        ? "dark"
        : "light";
    }

    return theme;
  }

  function applyTheme(theme) {
    const resolvedTheme = resolveTheme(theme);
    $html.attr("data-theme", resolvedTheme);
    updateThemeToggleButton(resolvedTheme === "dark");

    document.querySelectorAll(".dark-light-mode .theme-btn").forEach((btn) => {
      btn.classList.toggle("active", btn.getAttribute("data-theme") === theme);
    });
  }

  let currentThemeSetting = calculateSettingAsThemeString(
    localStorage.getItem("theme")
  );

  applyTheme(currentThemeSetting);

  $document.on("click", "[data-theme-toggle]", function () {
    const currentAppliedTheme =
      document.documentElement.getAttribute("data-theme") || "light";
    const nextTheme = currentAppliedTheme === "dark" ? "light" : "dark";

    localStorage.setItem("theme", nextTheme);
    currentThemeSetting = nextTheme;
    applyTheme(nextTheme);
  });

  $document.on("click", ".dark-light-mode .theme-btn", function () {
    const theme = this.getAttribute("data-theme");
    if (!theme) return;

    localStorage.setItem("theme", theme);
    currentThemeSetting = theme;
    applyTheme(theme);
  });

  window
    .matchMedia("(prefers-color-scheme: dark)")
    .addEventListener("change", function () {
      if (localStorage.getItem("theme") === "system") {
        applyTheme("system");
      }
    });

  $(".ltr-mode-btn").on("click", function () {
    $html.attr("dir", "ltr");
    localStorage.setItem("direction", "ltr");
    $(".ltr-mode-btn, .rtl-mode-btn").removeClass("active");
    $(this).addClass("active");
  });

  $(".rtl-mode-btn").on("click", function () {
    $html.attr("dir", "rtl");
    localStorage.setItem("direction", "rtl");
    $(".ltr-mode-btn, .rtl-mode-btn").removeClass("active");
    $(this).addClass("active");
  });

  $(document).ready(function () {
    const savedDir = localStorage.getItem("direction");

    if (savedDir) {
      $html.attr("dir", savedDir);

      if (savedDir === "rtl") {
        $(".rtl-mode-btn").addClass("active");
      } else {
        $(".ltr-mode-btn").addClass("active");
      }
    }
  });

  const allowedFonts = new Set(["manrope", "inter", "plus-jakarta-sans", "dm-sans"]);
  const defaultFont = "manrope";

  function normalizeFont(font) {
    if (!font) return defaultFont;
    const key = String(font).toLowerCase().trim();
    return allowedFonts.has(key) ? key : defaultFont;
  }

  function applyFont(font) {
    const nextFont = normalizeFont(font);
    document.documentElement.setAttribute("data-font", nextFont);
    localStorage.setItem("theme-font", nextFont);

    document.querySelectorAll(".font-btn").forEach((btn) => {
      btn.classList.toggle("active", btn.getAttribute("data-font") === nextFont);
    });
  }

  applyFont(localStorage.getItem("theme-font") || defaultFont);

  $document.on("click", ".font-btn", function () {
    const font = this.getAttribute("data-font");
    applyFont(font);
  });

  const colorPickerButtons = document.querySelectorAll(".color-picker-btn");
  const allowedColors = new Set([
    "blue",
    "red",
    "green",
    "yellow",
    "orange",
    "cyan",
    "violet",
  ]);
  const defaultColor = "blue";

  function normalizeColor(color) {
    if (!color) return defaultColor;
    const key = String(color).toLowerCase().trim();
    if (!allowedColors.has(key)) return defaultColor;
    if (key === "orange") return "yellow";
    return key;
  }

  function applyColor(color) {
    const nextColor = normalizeColor(color);
    document.documentElement.setAttribute("data-color", nextColor);
    localStorage.setItem("theme-color", nextColor);
    localStorage.setItem("templateColor", nextColor);

    colorPickerButtons.forEach((btn) => {
      btn.classList.toggle("active", btn.getAttribute("data-color") === nextColor);
    });
  }

  colorPickerButtons.forEach((btn) => {
    btn.addEventListener("click", function () {
      const color = btn.getAttribute("data-color");
      if (!color) return;

      applyColor(color);
    });
  });

  applyColor(
    localStorage.getItem("theme-color") || localStorage.getItem("templateColor") || defaultColor
  );

  $("#selectAll").on("change", function () {
    $(".form-check .form-check-input").prop("checked", $(this).prop("checked"));
  });

  $(".remove-btn").on("click", function () {
    $(this).closest("tr").remove();

    if ($(".table tbody tr").length === 0) {
      $(".table").addClass("bg-danger");
      $(".no-items-found").show();
    }
  });

  handleResponsiveState();
})(jQuery);
