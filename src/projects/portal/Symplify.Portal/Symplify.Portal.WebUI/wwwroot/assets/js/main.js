(function() {
  "use strict";

  const themeKey = 'theme';
  const themeRoot = document.documentElement;
  const themeMedia = window.matchMedia('(prefers-color-scheme: dark)');
  const sidebarMq = window.matchMedia('(max-width: 1199.98px)');

  function getThemeMode() {
    try {
      return localStorage.getItem(themeKey) || 'auto';
    } catch (e) {
      return 'auto';
    }
  }

  function setThemeMode(mode) {
    const m = mode === 'light' || mode === 'dark' ? mode : 'auto';
    try {
      localStorage.setItem(themeKey, m);
    } catch (e) {}
    if (m === 'auto') {
      themeRoot.removeAttribute('data-theme');
    } else {
      themeRoot.setAttribute('data-theme', m);
    }
    return m;
  }

  function resolveTheme(mode) {
    const m = mode === 'light' || mode === 'dark' ? mode : 'auto';
    if (m === 'dark') return 'dark';
    if (m === 'light') return 'light';
    return themeMedia && themeMedia.matches ? 'dark' : 'light';
  }

  function applyThemeLogos(mode) {
    const resolved = resolveTheme(mode);
    const isDark = resolved === 'dark';

    const targets = document.querySelectorAll('#header .logo img, .site-sidebar-brand img, img.hero-logo--brand, img.event-summary-card__logo, img[data-logo-light][data-logo-dark]');

    targets.forEach(function (img) {
      if (!img || !img.getAttribute) return;
      if (img.hasAttribute('data-logo-light') && img.hasAttribute('data-logo-dark')) {
        const next = isDark ? img.getAttribute('data-logo-dark') : img.getAttribute('data-logo-light');
        if (!next) return;
        if ((img.getAttribute('src') || '') !== next) img.setAttribute('src', next);
        return;
      }
    });
  }

  function applyThemeButton(btn, mode) {
    const icon = btn.querySelector('i');
    const m = mode === 'light' || mode === 'dark' ? mode : 'auto';
    if (icon) {
      icon.className = m === 'light' ? 'bi bi-sun' : m === 'dark' ? 'bi bi-moon' : 'bi bi-circle-half';
    }
    btn.title = m === 'light' ? 'Tema: Açık' : m === 'dark' ? 'Tema: Koyu' : 'Tema: Otomatik';
    btn.setAttribute('aria-label', btn.title);
  }

  function ensureHeaderControlsHost() {
    let host = document.querySelector('#header .header-controls');
    if (host) return host;

    const headerRow = document.querySelector('#header .header-mainbar .container-fluid');
    if (!headerRow) return null;

    const sidebarToggle = headerRow.querySelector('.sidebar-toggle');
    host = document.createElement('div');
    host.className = 'header-controls';

    if (sidebarToggle) {
      headerRow.insertBefore(host, sidebarToggle);
    } else {
      headerRow.appendChild(host);
    }

    return host;
  }

  function wireSidebar(sidebar) {
    if (!sidebar || sidebar.getAttribute('data-sidebar-wired') === 'true') return sidebar;

    sidebar.querySelectorAll('.site-sidebar-menu a').forEach(function (a) {
      a.addEventListener('click', function () {
        if (a.closest('.dropdown') && a.querySelector('.toggle-dropdown')) return;
        if (!window.bootstrap || !bootstrap.Offcanvas) return;
        const instance = bootstrap.Offcanvas.getInstance(sidebar) || new bootstrap.Offcanvas(sidebar);
        instance.hide();
      });
    });

    sidebar.querySelectorAll('.site-sidebar-menu .dropdown > a').forEach(function (a) {
      a.addEventListener('click', function (e) {
        if (!this.querySelector('.toggle-dropdown')) return;
        e.preventDefault();
        this.parentNode.classList.toggle('active');
        if (this.nextElementSibling) this.nextElementSibling.classList.toggle('dropdown-active');
        e.stopImmediatePropagation();
      });
    });

    sidebar.setAttribute('data-sidebar-wired', 'true');
    document.body.classList.add('sidebar-enabled');
    applyThemeLogos(getThemeMode());
    return sidebar;
  }

  function createFallbackSidebar() {
    const headerLogo = document.querySelector('#header .logo');
    const headerLogoImg = headerLogo ? headerLogo.querySelector('img') : null;
    const srcMenu = document.querySelector('#navmenu > ul');
    const brandHref = headerLogo && headerLogo.getAttribute('href') ? headerLogo.getAttribute('href') : '/';
    const siteTitle = headerLogoImg && headerLogoImg.getAttribute('alt')
      ? headerLogoImg.getAttribute('alt')
      : (document.title || 'Symplify').split('|')[0].trim();

    const div = document.createElement('div');
    div.className = 'offcanvas offcanvas-end site-sidebar';
    div.id = 'siteSidebar';
    div.tabIndex = -1;
    div.setAttribute('aria-label', 'Menü');
    div.innerHTML = '<div class="offcanvas-header"><button type="button" class="site-sidebar-close" data-bs-dismiss="offcanvas" aria-label="Kapat"><i class="bi bi-x"></i></button></div><div class="offcanvas-body"><a class="site-sidebar-brand"></a><div class="site-sidebar-slogan"></div><div class="site-sidebar-controls"></div><nav class="site-sidebar-nav"></nav></div>';
    document.body.appendChild(div);

    const brand = div.querySelector('.site-sidebar-brand');
    if (brand) {
      brand.setAttribute('href', brandHref || '/');
      brand.setAttribute('aria-label', siteTitle || 'Menü');
      if (headerLogoImg) {
        const clonedLogo = headerLogoImg.cloneNode(true);
        clonedLogo.removeAttribute('id');
        brand.appendChild(clonedLogo);
      } else {
        brand.textContent = siteTitle || 'Menü';
      }
    }

    const slogan = div.querySelector('.site-sidebar-slogan');
    if (slogan) slogan.textContent = siteTitle || '';

    const nav = div.querySelector('.site-sidebar-nav');
    if (nav && srcMenu) {
      const menu = srcMenu.cloneNode(true);
      menu.classList.add('site-sidebar-menu');
      nav.appendChild(menu);
    }

    return div;
  }

  function ensureSidebar() {
    let sidebar = document.querySelector('#siteSidebar');
    if (!sidebar) sidebar = createFallbackSidebar();
    return wireSidebar(sidebar);
  }

  function ensureSidebarToggle() {
    if (document.querySelector('#header .sidebar-toggle')) return;

    const headerRow = document.querySelector('#header .header-mainbar .container-fluid');
    const navMenu = document.querySelector('#header #navmenu');
    if (!headerRow || !navMenu) return;

    const btn = document.createElement('button');
    btn.type = 'button';
    btn.className = 'sidebar-toggle';
    btn.setAttribute('aria-label', 'Menü');
    btn.innerHTML = '<i class="bi bi-list"></i>';
    headerRow.insertBefore(btn, navMenu.nextSibling);

    const sidebar = ensureSidebar();
    if (sidebar && window.bootstrap && bootstrap.Offcanvas) {
      btn.addEventListener('click', function () {
        const instance = bootstrap.Offcanvas.getInstance(sidebar) || new bootstrap.Offcanvas(sidebar);
        instance.show();
      });
    }

    const mobileToggle = document.querySelector('.mobile-nav-toggle');
    if (mobileToggle) mobileToggle.classList.add('d-none');
  }

  function createThemeToggle() {
    let btn = document.querySelector('.theme-toggle');
    if (btn) return btn;

    btn = document.createElement('button');
    btn.type = 'button';
    btn.className = 'theme-toggle';
    btn.innerHTML = '<i class="bi bi-circle-half"></i>';

    let mode = setThemeMode(getThemeMode());
    applyThemeButton(btn, mode);
    applyThemeLogos(mode);

    btn.addEventListener('click', function () {
      mode = getThemeMode();
      const next = mode === 'auto' ? 'light' : mode === 'light' ? 'dark' : 'auto';
      mode = setThemeMode(next);
      applyThemeButton(btn, mode);
      applyThemeLogos(mode);
    });

    if (themeMedia && typeof themeMedia.addEventListener === 'function') {
      themeMedia.addEventListener('change', function () {
        if (getThemeMode() === 'auto') {
          setThemeMode('auto');
          applyThemeButton(btn, 'auto');
          applyThemeLogos('auto');
        }
      });
    }

    return btn;
  }

  function placeHeaderControls() {
    ensureSidebarToggle();

    const topbarHost = document.querySelector('#header .header-topbar .topbar-controls');
    const headerHost = ensureHeaderControlsHost();
    const sidebar = ensureSidebar();
    const sidebarHost = sidebar ? sidebar.querySelector('.site-sidebar-controls') : null;

    const theme = createThemeToggle();

    const toSidebar = !!(sidebarMq && sidebarMq.matches && sidebarHost);
    if (topbarHost) {
      topbarHost.appendChild(theme);
    } else if (toSidebar) {
      sidebarHost.appendChild(theme);
    } else if (headerHost) {
      headerHost.appendChild(theme);
    }

    const mobileToggle = document.querySelector('.mobile-nav-toggle');
    if (mobileToggle) mobileToggle.classList.add('d-none');
  }

  ensureSidebarToggle();
  ensureSidebar();

  window.addEventListener('load', placeHeaderControls);
  if (sidebarMq && typeof sidebarMq.addEventListener === 'function') {
    sidebarMq.addEventListener('change', placeHeaderControls);
  }

  /**
   * Apply .scrolled class to the body as the page is scrolled down
   */
  function toggleScrolled() {
    const selectBody = document.querySelector('body');
    const selectHeader = document.querySelector('#header');
    if (!selectHeader.classList.contains('scroll-up-sticky') && !selectHeader.classList.contains('sticky-top') && !selectHeader.classList.contains('fixed-top')) return;
    window.scrollY > 100 ? selectBody.classList.add('scrolled') : selectBody.classList.remove('scrolled');
  }

  document.addEventListener('scroll', toggleScrolled);
  window.addEventListener('load', toggleScrolled);

  /**
   * Mobile nav toggle
   */
  const mobileNavToggleBtn = document.querySelector('.mobile-nav-toggle');

  function mobileNavToogle() {
    document.querySelector('body').classList.toggle('mobile-nav-active');
    mobileNavToggleBtn.classList.toggle('bi-list');
    mobileNavToggleBtn.classList.toggle('bi-x');
  }
  if (mobileNavToggleBtn) {
    if (!document.body.classList.contains('sidebar-enabled')) {
      mobileNavToggleBtn.addEventListener('click', mobileNavToogle);
    }
  }

  /**
   * Hide mobile nav on same-page/hash links
   */
  document.querySelectorAll('#navmenu a').forEach(navmenu => {
    navmenu.addEventListener('click', () => {
      if (navmenu.closest('.dropdown') && navmenu.querySelector('.toggle-dropdown')) return;
      if (document.querySelector('.mobile-nav-active')) {
        mobileNavToogle();
      }
    });

  });

  /**
   * Toggle mobile nav dropdowns
   */
  document.querySelectorAll('.navmenu .dropdown > a').forEach(navmenu => {
    navmenu.addEventListener('click', function(e) {
      if (!document.querySelector('.mobile-nav-active')) return;
      if (!this.querySelector('.toggle-dropdown')) return;
      e.preventDefault();
      this.parentNode.classList.toggle('active');
      if (this.nextElementSibling) this.nextElementSibling.classList.toggle('dropdown-active');
      e.stopImmediatePropagation();
    });
  });

  /**
   * Preloader
   */
  const preloader = document.querySelector('#preloader');
  if (preloader) {
    window.addEventListener('load', () => {
      preloader.remove();
    });
  }

  function initMobileTabScroll() {
    const mq = window.matchMedia('(max-width: 991.98px)');

    function scrollToEl(el, offset) {
      if (!el) return;
      const top = el.getBoundingClientRect().top + window.pageYOffset - offset;
      window.scrollTo({ top, behavior: 'smooth' });
    }

    function getContentRoot(trigger) {
      const row = trigger.closest('.row');
      if (!row) return null;
      return row.querySelector('.tabs__content');
    }

    function handle(trigger) {
      if (!mq.matches) return;
      const root = getContentRoot(trigger);
      if (!root) return;
      const header = document.querySelector('#header');
      const headerOffset = header ? Math.ceil(header.getBoundingClientRect().height) + 12 : 92;
      scrollToEl(root, headerOffset);
    }

    document.querySelectorAll('.tabs__nav [data-bs-toggle="pill"]').forEach(function (trigger) {
      trigger.addEventListener('click', function () {
        setTimeout(function () { handle(trigger); }, 50);
      });
    });
  }

  window.addEventListener('load', initMobileTabScroll);

  /**
   * Scroll top button
   */
  let scrollTop = document.querySelector('.scroll-top');

  function toggleScrollTop() {
    if (scrollTop) {
      window.scrollY > 100 ? scrollTop.classList.add('active') : scrollTop.classList.remove('active');
    }
  }
  scrollTop.addEventListener('click', (e) => {
    e.preventDefault();
    window.scrollTo({
      top: 0,
      behavior: 'smooth'
    });
  });

  window.addEventListener('load', toggleScrollTop);
  document.addEventListener('scroll', toggleScrollTop);

  /**
   * Animation on scroll function and init
   */
  function aosInit() {
    AOS.init({
      duration: 600,
      easing: 'ease-in-out',
      once: true,
      mirror: false
    });
  }
  window.addEventListener('load', aosInit);

  /**
   * Initiate Pure Counter
   */
  new PureCounter();

  /**
   * Init swiper sliders
   */
  function initSwiper() {
    document.querySelectorAll(".init-swiper").forEach(function(swiperElement) {
      let config = JSON.parse(
        swiperElement.querySelector(".swiper-config").innerHTML.trim()
      );

      if (swiperElement.classList.contains("swiper-tab")) {
        initSwiperWithCustomPagination(swiperElement, config);
      } else {
        new Swiper(swiperElement, config);
      }
    });
  }

  window.addEventListener("load", initSwiper);

  /*
   * Pricing Toggle
   */

  const pricingContainers = document.querySelectorAll('.pricing-toggle-container');

  pricingContainers.forEach(function(container) {
    const pricingSwitch = container.querySelector('.pricing-toggle input[type="checkbox"]');
    const monthlyText = container.querySelector('.monthly');
    const yearlyText = container.querySelector('.yearly');

    pricingSwitch.addEventListener('change', function() {
      const pricingItems = container.querySelectorAll('.pricing-item');

      if (this.checked) {
        monthlyText.classList.remove('active');
        yearlyText.classList.add('active');
        pricingItems.forEach(item => {
          item.classList.add('yearly-active');
        });
      } else {
        monthlyText.classList.add('active');
        yearlyText.classList.remove('active');
        pricingItems.forEach(item => {
          item.classList.remove('yearly-active');
        });
      }
    });
  });

})();
(function () {
  "use strict";

  const root = document.querySelector('[data-documents]');
  if (!root) return;

  const list = root.querySelector('[data-documents-list]');
  const cards = Array.from(root.querySelectorAll('.document-card'));
  const search = root.querySelector('[data-documents-search]');
  const filterControls = Array.from(root.querySelectorAll('[data-documents-filter]'));
  const sortControl = root.querySelector('[data-documents-sort]');
  const viewButtons = Array.from(root.querySelectorAll('[data-documents-view]'));
  const count = root.querySelector('[data-documents-count]');
  const empty = root.querySelector('[data-documents-empty]');
  const storageKey = 'symplify.portal.documents.view';

  function normalize(value) {
    return (value || '')
      .toString()
      .trim()
      .toLocaleLowerCase(document.documentElement.lang || 'tr-TR');
  }

  function splitAliases(value) {
    return (value || '')
      .toString()
      .split('||')
      .map(normalize)
      .filter(Boolean);
  }

  function setView(view) {
    const next = view === 'list' ? 'list' : 'grid';
    if (list) list.setAttribute('data-view', next);

    viewButtons.forEach(function (btn) {
      const isActive = btn.getAttribute('data-documents-view') === next;
      btn.classList.toggle('active', isActive);
      btn.setAttribute('aria-pressed', isActive ? 'true' : 'false');
    });

    try {
      localStorage.setItem(storageKey, next);
    } catch (e) {}
  }

  function getFilters() {
    const filters = {
      q: normalize(search ? search.value : ''),
      type: 'all',
      year: 'all',
      status: 'all'
    };

    filterControls.forEach(function (control) {
      const key = control.getAttribute('data-documents-filter');
      if (key) filters[key] = control.value || 'all';
    });

    return filters;
  }

  function matches(card, filters) {
    const haystack = normalize([
      card.getAttribute('data-title'),
      card.getAttribute('data-type'),
      card.querySelector('.document-card__congress')?.textContent
    ].join(' '));

    if (filters.q && !haystack.includes(filters.q)) return false;

    if (filters.type !== 'all') {
      const selectedType = normalize(filters.type);
      const typeAliases = splitAliases(card.getAttribute('data-type-aliases') || card.getAttribute('data-type'));
      if (!typeAliases.includes(selectedType)) return false;
    }

    if (filters.year !== 'all' && card.getAttribute('data-year') !== filters.year) return false;
    if (filters.status !== 'all' && card.getAttribute('data-status') !== filters.status) return false;

    return true;
  }

  function sortCards() {
    if (!list) return;
    const mode = sortControl ? sortControl.value : 'newest';
    const sorted = cards.slice().sort(function (a, b) {
      if (mode === 'title') {
        return normalize(a.getAttribute('data-title')).localeCompare(normalize(b.getAttribute('data-title')), document.documentElement.lang || 'tr');
      }

      const da = new Date(a.getAttribute('data-date') || '1900-01-01').getTime();
      const db = new Date(b.getAttribute('data-date') || '1900-01-01').getTime();
      return mode === 'oldest' ? da - db : db - da;
    });

    sorted.forEach(function (card) {
      list.appendChild(card);
    });
  }

  function applyFilters() {
    const filters = getFilters();
    let visible = 0;

    cards.forEach(function (card) {
      const show = matches(card, filters);

      if (show) {
        card.hidden = false;
        card.removeAttribute('hidden');
        card.style.removeProperty('display');
        card.classList.add('aos-animate');
        visible += 1;
      } else {
        card.hidden = true;
        card.setAttribute('hidden', 'hidden');
        card.style.display = 'none';
      }
    });

    if (count) count.textContent = visible.toString();
    if (empty) empty.hidden = visible !== 0;

    if (window.AOS && typeof window.AOS.refreshHard === 'function') {
      window.requestAnimationFrame(function () {
        window.AOS.refreshHard();
      });
    } else if (window.AOS && typeof window.AOS.refresh === 'function') {
      window.requestAnimationFrame(function () {
        window.AOS.refresh();
      });
    }
  }

  viewButtons.forEach(function (btn) {
    btn.addEventListener('click', function () {
      setView(btn.getAttribute('data-documents-view'));
    });
  });

  if (search) search.addEventListener('input', applyFilters);
  filterControls.forEach(function (control) {
    control.addEventListener('change', applyFilters);
  });

  if (sortControl) {
    sortControl.addEventListener('change', function () {
      sortCards();
      applyFilters();
    });
  }

  let initialView = 'grid';
  try {
    initialView = localStorage.getItem(storageKey) || 'grid';
  } catch (e) {}

  setView(initialView);
  sortCards();
  applyFilters();
})();
