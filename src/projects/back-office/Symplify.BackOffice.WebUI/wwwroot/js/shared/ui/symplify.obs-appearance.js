/* OBS appearance panel: uses original OBS template CSS, no additional CSS. */
(() => {
  'use strict';
  if (window.__symplifyObsAppearanceCaptureBound) return;
  window.__symplifyObsAppearanceCaptureBound = true;
  const storageGet = key => { try { return localStorage.getItem(key); } catch { return null; } };
  const storageSet = (key, val) => { try { localStorage.setItem(key, val); } catch {} };
  const options = {
    theme: ['light', 'dark', 'system'],
    color: ['blue', 'red', 'green', 'yellow', 'cyan', 'violet'],
    font: ['manrope', 'inter', 'plus-jakarta-sans', 'dm-sans', 'segoe-ui', 'arial', 'tahoma', 'verdana'],
    size: ['compact', 'small', 'standard', 'large', 'xlarge']
  };
  let panel;
  const safe = (type, value, fallback) => options[type].includes(value) ? value : fallback;
  function setOpen(show) {
    panel = document.getElementById('obsAppearancePanel') || document.querySelector('.theme-customization-sidebar');
    if (!panel) { console.error('[Symplify OBS] Ayarlar paneli bulunamadı: #obsAppearancePanel'); return; }
    panel.classList.toggle('active', show);
    panel.classList.toggle('show', show);
    panel.setAttribute('aria-hidden', String(!show));
    document.querySelectorAll('[data-obs-appearance-open]').forEach(b => b.setAttribute('aria-expanded', String(show)));
  }
  function setTheme(value) {
    const theme = safe('theme', value, 'light');
    const dark = window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
    document.documentElement.setAttribute('data-theme', theme === 'system' ? (dark ? 'dark' : 'light') : theme);
    storageSet('theme', theme);
    panel?.querySelectorAll('.theme-btn').forEach(b => b.classList.toggle('active', b.dataset.theme === theme));
  }
  function setColor(value) {
    const color = safe('color', value, 'blue');
    document.documentElement.setAttribute('data-color', color);
    storageSet('theme-color', color);
    storageSet('templateColor', color);
    panel?.querySelectorAll('.color-picker-btn').forEach(b => b.classList.toggle('active', b.dataset.color === color));
  }
  function setFont(value) {
    const font = safe('font', value, 'manrope');
    document.documentElement.setAttribute('data-font', font);
    storageSet('theme-font', font);
    panel?.querySelectorAll('.font-btn').forEach(b => b.classList.toggle('active', b.dataset.font === font));
  }
  function setSize(value) {
    const size = safe('size', value, 'standard');
    document.documentElement.setAttribute('data-ui-size', size);
    storageSet('theme-ui-size', size);
    panel?.querySelectorAll('.ui-size-btn').forEach(b => b.classList.toggle('active', b.dataset.uiSize === size));
  }
  function initialize() {
    panel = document.getElementById('obsAppearancePanel') || document.querySelector('.theme-customization-sidebar');
    if (!panel) { console.error('[Symplify OBS] #obsAppearancePanel bulunamadı. Layout entegrasyonunu kontrol edin.'); return; }
    setOpen(false);
    setTheme(storageGet('theme') || 'light');
    setColor(storageGet('theme-color') || 'blue');
    setFont(storageGet('theme-font') || 'manrope');
    setSize(storageGet('theme-ui-size') || 'standard');
  }
  // Capture phase ensures the settings button works even if other navbar scripts
  // stop bubbling events. Keep other OBS controls on the normal click handler.
  document.addEventListener('click', e => {
    const opener = e.target instanceof Element ? e.target.closest('[data-obs-appearance-open]') : null;
    if (!opener) return;
    e.preventDefault();
    e.stopPropagation();
    setOpen(true);
  }, true);

  document.addEventListener('click', e => {
    if (!(e.target instanceof Element)) return;
    if (e.target.closest('.theme-customization-sidebar__close')) { e.preventDefault(); setOpen(false); return; }
    const button = e.target.closest('.theme-customization-sidebar button');
    if (!button) return;
    if (button.matches('.theme-btn')) setTheme(button.dataset.theme);
    else if (button.matches('.color-picker-btn')) setColor(button.dataset.color);
    else if (button.matches('.font-btn')) setFont(button.dataset.font);
    else if (button.matches('.ui-size-btn')) setSize(button.dataset.uiSize);
  });
  document.addEventListener('keydown', e => { if (e.key === 'Escape') setOpen(false); });
  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', initialize, { once: true });
  else initialize();
  if (window.matchMedia) {
    const media = window.matchMedia('(prefers-color-scheme: dark)');
    if (media.addEventListener) media.addEventListener('change', () => { if (storageGet('theme') === 'system') setTheme('system'); });
  }
})();
