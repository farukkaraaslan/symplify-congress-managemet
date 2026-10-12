const __layoutScriptUrl = new URL(
  document.currentScript?.src || window.location.href
);
const __partialsBaseUrl = new URL("../../partials/", __layoutScriptUrl);

async function loadPartial(selector, fileUrl) {
  const target = document.querySelector(selector);
  if (!target) return;

  try {
    const response = await fetch(fileUrl);
    if (!response.ok) {
      throw new Error(`${fileUrl} yüklenemedi`);
    }

    const html = await response.text();
    target.innerHTML = html;
  } catch (error) {
    console.error(error);
  }
}

async function loadLayout() {
  await loadPartial(
    "#sidebar-container",
    new URL("sidebar.html", __partialsBaseUrl)
  );
  await loadPartial(
    "#navbar-container",
    new URL("navbar.html", __partialsBaseUrl)
  );
  await loadPartial(
    "#footer-container",
    new URL("footer.html", __partialsBaseUrl)
  );

  document.dispatchEvent(new CustomEvent("layout:loaded"));
}

document.addEventListener("DOMContentLoaded", loadLayout);
